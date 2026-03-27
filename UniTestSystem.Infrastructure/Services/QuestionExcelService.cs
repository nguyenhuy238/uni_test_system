using ClosedXML.Excel;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using System.IO;

namespace UniTestSystem.Infrastructure.Services
{
    public class QuestionExcelService : IQuestionExcelService
    {
        private readonly IQuestionService _svc;
        private readonly IRepository<Question> _qRepo;
        private readonly IRepository<Subject> _subjectRepo;
        private readonly IRepository<DifficultyLevel> _difficultyRepo;

        public QuestionExcelService(
            IQuestionService svc,
            IRepository<Question> qRepo,
            IRepository<Subject> subjectRepo,
            IRepository<DifficultyLevel> difficultyRepo)
        {
            _svc = svc;
            _qRepo = qRepo;
            _subjectRepo = subjectRepo;
            _difficultyRepo = difficultyRepo;
        }

        public Task<byte[]> ExportAsync(IEnumerable<Question> data)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Questions");

            var headers = new[]
            {
                "Content", "Type", "Status", "Subject", "DifficultyLevel", "Tags (comma)", "Options (|)", 
                "CorrectKeys (|)", "EssayMinWords", "MatchingPairs (L=R || L=R)", "DragTokens (|)", 
                "DragSlots (Name=Answer || Name=Answer)", "Media (FileName#Url#Caption || ...)"
            };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            int r = 2;
            foreach (var q in data)
            {
                ws.Cell(r, 1).Value = q.Content;
                ws.Cell(r, 2).Value = q.Type.ToString();
                ws.Cell(r, 3).Value = q.Status.ToString();
                ws.Cell(r, 4).Value = q.SubjectId;
                ws.Cell(r, 5).Value = q.DifficultyLevelId;
                ws.Cell(r, 6).Value = string.Join(',', q.Tags ?? new());
                ws.Cell(r, 7).Value = string.Join('|', q.Options.Select(o => o.Content));
                ws.Cell(r, 8).Value = string.Join('|', q.Options.Where(o => o.IsCorrect).Select(o => o.Content));
                ws.Cell(r, 9).SetValue(q.EssayMinWords ?? 0);

                var matching = q.MatchingPairs is { Count: > 0 }
                    ? string.Join(" || ", q.MatchingPairs!.Select(p => $"{p.L}={p.R}"))
                    : "";
                ws.Cell(r, 10).Value = matching;

                var tokens = q.DragDrop?.Tokens ?? new List<string>();
                var slots = q.DragDrop?.Slots ?? new List<DragSlot>();
                ws.Cell(r, 11).Value = string.Join('|', tokens);
                ws.Cell(r, 12).Value = slots.Count > 0 ? string.Join(" || ", slots.Select(s => $"{s.Name}={s.Answer}")) : "";

                var media = q.Media is { Count: > 0 }
                    ? string.Join(" || ", q.Media.Select(m => $"{m.FileName}#{m.Url}#{m.Caption ?? ""}"))
                    : "";
                ws.Cell(r, 13).Value = media;

                r++;
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return Task.FromResult(ms.ToArray());
        }

        public async Task<ImportResult> ImportAsync(Stream fileStream, string actor)
        {
            var res = new ImportResult();
            using var wb = new XLWorkbook(fileStream);
            if (!wb.Worksheets.TryGetWorksheet("Questions", out var ws))
                ws = wb.Worksheets.Worksheet(1);

            var range = ws.RangeUsed();
            if (range == null) return res;

            static string Norm(string? s) => (s ?? "").Trim().ToLowerInvariant();
            static string MakeKey(string content, QType type, string subject)
                => $"{Norm(content)}|{type}|{Norm(subject)}";

            var existing = await _qRepo.GetAllAsync();
            var existingKeys = existing.Select(q => MakeKey(q.Content, q.Type, q.SubjectId)).ToHashSet(StringComparer.Ordinal);
            var seenInThisFile = new HashSet<string>(StringComparer.Ordinal);
            var subjects = await _subjectRepo.GetAllAsync();
            var difficulties = await _difficultyRepo.GetAllAsync();
            var subjectLookup = BuildLookup(subjects.Select(x => (x.Id, x.Name)));
            var difficultyLookup = BuildLookup(difficulties.Select(x => (x.Id, x.Name)));
            var defaultDifficultyId = ResolveReferenceId("Easy", difficultyLookup)
                ?? difficulties.FirstOrDefault()?.Id;

            var headerRow = range.FirstRow();
            var colIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= headerRow.CellCount(); c++)
            {
                var name = headerRow.Cell(c).GetString().Trim();
                if (!string.IsNullOrEmpty(name)) colIndex[name] = c;
            }

            int GetCol(string name, bool required = false)
            {
                if (colIndex.TryGetValue(name, out var c)) return c;
                if (required) throw new Exception($"Missing required column: {name}");
                return -1;
            }

            int cContent = GetCol("Content", true), cType = GetCol("Type", true), cStatus = GetCol("Status"), cSubject = GetCol("Subject", true);
            int cDifficultyLevel = GetCol("DifficultyLevel"), cTags = GetCol("Tags (comma)"), cOptions = GetCol("Options (|)"), cCorrect = GetCol("CorrectKeys (|)"), cEssayMin = GetCol("EssayMinWords"), cPairs = GetCol("MatchingPairs (L=R || L=R)"), cDragTokens = GetCol("DragTokens (|)"), cDragSlots = GetCol("DragSlots (Name=Answer || Name=Answer)"), cMedia = GetCol("Media (FileName#Url#Caption || ...)");

            foreach (var row in range.RowsUsed().Skip(1))
            {
                res.Total++;
                try
                {
                    var content = GetCell(row, cContent);
                    var typeRaw = GetCell(row, cType);
                    var rawSubject = GetCell(row, cSubject);

                    if (!Enum.TryParse<QType>(typeRaw, true, out var type)) throw new Exception($"Invalid Type: {typeRaw}");

                    var isLegacyShifted = LooksLikeLegacyShiftedRow(
                        type,
                        GetCell(row, cDifficultyLevel),
                        GetCell(row, cTags),
                        GetCell(row, cOptions),
                        GetCell(row, cCorrect),
                        GetCell(row, cMedia));

                    var rawDifficulty = isLegacyShifted ? string.Empty : GetCell(row, cDifficultyLevel);
                    var rawTags = isLegacyShifted ? GetCell(row, cDifficultyLevel) : GetCell(row, cTags);
                    var rawOptions = isLegacyShifted ? GetCell(row, cTags) : GetCell(row, cOptions);
                    var rawCorrect = isLegacyShifted ? GetCell(row, cOptions) : GetCell(row, cCorrect);
                    var rawEssayMin = isLegacyShifted ? GetCell(row, cCorrect) : GetCell(row, cEssayMin);
                    var rawPairs = isLegacyShifted ? GetCell(row, cEssayMin) : GetCell(row, cPairs);
                    var rawDragTokens = isLegacyShifted ? GetCell(row, cPairs) : GetCell(row, cDragTokens);
                    var rawDragSlots = isLegacyShifted ? GetCell(row, cDragTokens) : GetCell(row, cDragSlots);
                    var rawMedia = isLegacyShifted ? GetCell(row, cDragSlots) : GetCell(row, cMedia);

                    var subjectId = await EnsureSubjectIdAsync(rawSubject, subjectLookup);
                    var difficultyId = await EnsureDifficultyIdAsync(rawDifficulty, difficultyLookup, defaultDifficultyId, isLegacyShifted);

                    var key = MakeKey(content, type, subjectId);

                    if (existingKeys.Contains(key) || seenInThisFile.Contains(key))
                    {
                        res.Skipped++;
                        res.SkippedReasons.Add($"Row {row.RowNumber()}: skipped duplicate.");
                        continue;
                    }

                    var q = new Question
                    {
                        Content = content,
                        Type = type,
                        Status = ParseStatus(GetCell(row, cStatus)),
                        SubjectId = subjectId,
                        DifficultyLevelId = difficultyId,
                        Tags = SplitCsv(rawTags, ','),
                        EssayMinWords = ParseNullableInt(rawEssayMin)
                    };

                    var optTexts = SplitCsv(rawOptions, '|');
                    var correctTexts = SplitCsv(rawCorrect, '|').ToHashSet(StringComparer.OrdinalIgnoreCase);
                    q.Options = optTexts.Select(t => new Option { Content = t, IsCorrect = correctTexts.Contains(t) }).ToList();

                    if (!string.IsNullOrWhiteSpace(rawPairs))
                    {
                        var pairs = new List<MatchPair>();
                        foreach (var token in SplitRaw(rawPairs, "||"))
                        {
                            var eqIdx = token.IndexOf('=');
                            if (eqIdx > 0) pairs.Add(new MatchPair(token[..eqIdx].Trim(), token[(eqIdx + 1)..].Trim()));
                        }
                        if (pairs.Count > 0) q.MatchingPairs = pairs;
                    }

                    var dragTokens = SplitCsv(rawDragTokens, '|');
                    var dragSlots = new List<DragSlot>();
                    foreach (var token in SplitRaw(rawDragSlots, "||"))
                    {
                        var eqIdx = token.IndexOf('=');
                        if (eqIdx > 0) dragSlots.Add(new DragSlot(token[..eqIdx].Trim(), token[(eqIdx + 1)..].Trim()));
                    }
                    if (dragTokens.Count > 0 || dragSlots.Count > 0)
                    {
                        q.DragDrop = new DragDropConfig
                        {
                            Tokens = dragTokens,
                            Slots = dragSlots
                        };
                    }

                    var media = new List<MediaFile>();
                    foreach (var token in SplitRaw(rawMedia, "||"))
                    {
                        var parts = token.Split('#');
                        if (parts.Length >= 2)
                        {
                            media.Add(new MediaFile
                            {
                                FileName = parts[0].Trim(),
                                Url = parts[1].Trim(),
                                Caption = parts.Length >= 3 ? parts[2].Trim() : null
                            });
                        }
                    }
                    if (media.Count > 0) q.Media = media;

                    if (type == QType.TrueFalse)
                    {
                        var isTrue = correctTexts.Contains("True");
                        q.Options = new List<Option> { new Option { Content = "True", IsCorrect = isTrue }, new Option { Content = "False", IsCorrect = !isTrue } };
                    }

                    await _svc.CreateAsync(q, actor);
                    existingKeys.Add(key); seenInThisFile.Add(key); res.Success++;
                }
                catch (Exception ex) { res.Errors.Add($"Row {row.RowNumber()}: {GetRootErrorMessage(ex)}"); }
            }
            return res;
        }

        private static string GetCell(IXLRangeRow row, int col) => col > 0 ? row.Cell(col).GetString().Trim() : string.Empty;
        private static int? ParseNullableInt(string? s) => int.TryParse(s?.Trim(), out var v) ? v : null;
        private static List<string> SplitCsv(string? s, char sep) => string.IsNullOrWhiteSpace(s) ? new() : s.Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        private static List<string> SplitRaw(string? s, string delim) => string.IsNullOrWhiteSpace(s) ? new() : s.Split(delim, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

        private async Task<string> EnsureSubjectIdAsync(string rawSubject, Dictionary<string, string> subjectLookup)
        {
            if (string.IsNullOrWhiteSpace(rawSubject))
                throw new Exception("Subject is required");

            var resolved = ResolveReferenceId(rawSubject, subjectLookup);
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;

            var subjectName = rawSubject.Trim();
            var subject = new Subject
            {
                Id = BuildNewReferenceId(subjectName, subjectLookup),
                Name = subjectName,
                IsDeleted = false
            };

            await _subjectRepo.InsertAsync(subject);
            AddLookup(subjectLookup, subject.Id, subject.Name);
            return subject.Id;
        }

        private async Task<string> EnsureDifficultyIdAsync(
            string rawDifficulty,
            Dictionary<string, string> difficultyLookup,
            string? defaultDifficultyId,
            bool isLegacyShifted)
        {
            var candidate = rawDifficulty;
            if (string.IsNullOrWhiteSpace(candidate) || isLegacyShifted)
                candidate = defaultDifficultyId ?? "Easy";

            var resolved = ResolveReferenceId(candidate, difficultyLookup);
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;

            var difficultyName = string.IsNullOrWhiteSpace(rawDifficulty) || isLegacyShifted
                ? "Easy"
                : rawDifficulty.Trim();

            var difficulty = new DifficultyLevel
            {
                Id = BuildNewReferenceId(difficultyName, difficultyLookup),
                Name = difficultyName,
                Weight = ParseDifficultyWeight(difficultyName),
                IsDeleted = false
            };

            await _difficultyRepo.InsertAsync(difficulty);
            AddLookup(difficultyLookup, difficulty.Id, difficulty.Name);
            return difficulty.Id;
        }

        private static QuestionStatus ParseStatus(string rawStatus)
        {
            if (Enum.TryParse<QuestionStatus>(rawStatus, true, out var status))
                return status;

            return (rawStatus ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "active" => QuestionStatus.Approved,
                "inactive" => QuestionStatus.Draft,
                _ => QuestionStatus.Draft
            };
        }

        private static Dictionary<string, string> BuildLookup(IEnumerable<(string Id, string Name)> values)
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (id, name) in values)
            {
                AddLookup(lookup, id, name);
            }
            return lookup;
        }

        private static void AddLookup(Dictionary<string, string> lookup, string id, string? name)
        {
            if (!string.IsNullOrWhiteSpace(id) && !lookup.ContainsKey(id))
                lookup[id] = id;

            if (!string.IsNullOrWhiteSpace(name) && !lookup.ContainsKey(name))
                lookup[name] = id;
        }

        private static string? ResolveReferenceId(string? raw, Dictionary<string, string> lookup)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return lookup.TryGetValue(raw.Trim(), out var id) ? id : null;
        }

        private static string BuildNewReferenceId(string rawValue, Dictionary<string, string> lookup)
        {
            var candidate = rawValue.Trim();
            if (!lookup.Values.Any(v => string.Equals(v, candidate, StringComparison.OrdinalIgnoreCase)))
                return candidate;

            return $"{candidate}-{Guid.NewGuid():N}";
        }

        private static int ParseDifficultyWeight(string name)
        {
            return name.Trim().ToLowerInvariant() switch
            {
                "easy" => 1,
                "medium" => 2,
                "hard" => 3,
                _ => 1
            };
        }

        private static bool LooksLikeLegacyShiftedRow(
            QType type,
            string rawDifficulty,
            string rawTags,
            string rawOptions,
            string rawCorrect,
            string rawMedia)
        {
            if (type != QType.MCQ && type != QType.TrueFalse && type != QType.Essay && type != QType.Matching && type != QType.DragDrop)
                return false;

            var tagsLooksLikeOptions = rawTags.Contains('|', StringComparison.Ordinal);
            var difficultyLooksLikeTags = rawDifficulty.Contains(',', StringComparison.Ordinal) || rawDifficulty.Contains('|', StringComparison.Ordinal);
            var mediaAppearsShifted = string.IsNullOrWhiteSpace(rawMedia) && rawCorrect.Contains('|', StringComparison.Ordinal);

            return tagsLooksLikeOptions && (difficultyLooksLikeTags || mediaAppearsShifted);
        }

        private static string GetRootErrorMessage(Exception ex)
        {
            var current = ex;
            while (current.InnerException != null)
                current = current.InnerException;

            return current.Message;
        }
    }
}
