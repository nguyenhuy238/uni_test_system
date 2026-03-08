using ClosedXML.Excel;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using System.Text;
using System.IO;

namespace UniTestSystem.Infrastructure.Services
{
    public class QuestionExcelService : IQuestionExcelService
    {
        private readonly IQuestionService _svc;
        private readonly IRepository<Question> _qRepo;

        public QuestionExcelService(IQuestionService svc, IRepository<Question> qRepo)
        {
            _svc = svc;
            _qRepo = qRepo;
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
                ws.Cell(r, 5).Value = string.Join(',', q.Tags ?? new());
                ws.Cell(r, 6).Value = string.Join('|', q.Options.Select(o => o.Content));
                ws.Cell(r, 7).Value = string.Join('|', q.Options.Where(o => o.IsCorrect).Select(o => o.Content));
                ws.Cell(r, 8).SetValue(q.EssayMinWords ?? 0);

                var matching = q.MatchingPairs is { Count: > 0 }
                    ? string.Join(" || ", q.MatchingPairs!.Select(p => $"{p.L}={p.R}"))
                    : "";
                ws.Cell(r, 9).Value = matching;

                var tokens = q.DragDrop?.Tokens ?? new List<string>();
                var slots = q.DragDrop?.Slots ?? new List<DragSlot>();
                ws.Cell(r, 10).Value = string.Join('|', tokens);
                ws.Cell(r, 11).Value = slots.Count > 0 ? string.Join(" || ", slots.Select(s => $"{s.Name}={s.Answer}")) : "";

                var media = q.Media is { Count: > 0 }
                    ? string.Join(" || ", q.Media.Select(m => $"{m.FileName}#{m.Url}#{m.Caption ?? ""}"))
                    : "";
                ws.Cell(r, 12).Value = media;

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
                    var content = row.Cell(cContent).GetString().Trim();
                    var typeRaw = row.Cell(cType).GetString().Trim();
                    var subject = row.Cell(cSubject).GetString().Trim();

                    if (!Enum.TryParse<QType>(typeRaw, true, out var type)) throw new Exception($"Invalid Type: {typeRaw}");
                    var key = MakeKey(content, type, subject);

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
                        Status = cStatus > 0 && Enum.TryParse<QuestionStatus>(row.Cell(cStatus).GetString(), true, out var st) ? st : QuestionStatus.Draft,
                        SubjectId = subject,
                        DifficultyLevelId = cDifficultyLevel > 0 ? row.Cell(cDifficultyLevel).GetString().Trim() : "Easy",
                        Tags = SplitCsv(cTags > 0 ? row.Cell(cTags).GetString() : "", ','),
                        EssayMinWords = ParseNullableInt(cEssayMin > 0 ? row.Cell(cEssayMin).GetString() : "")
                    };

                    var optTexts = SplitCsv(cOptions > 0 ? row.Cell(cOptions).GetString() : "", '|');
                    var correctTexts = SplitCsv(cCorrect > 0 ? row.Cell(cCorrect).GetString() : "", '|').ToHashSet(StringComparer.OrdinalIgnoreCase);
                    q.Options = optTexts.Select(t => new Option { Content = t, IsCorrect = correctTexts.Contains(t) }).ToList();

                    if (cPairs > 0)
                    {
                        var pairs = new List<MatchPair>();
                        foreach (var token in SplitRaw(row.Cell(cPairs).GetString(), "||"))
                        {
                            var eqIdx = token.IndexOf('=');
                            if (eqIdx > 0) pairs.Add(new MatchPair(token[..eqIdx].Trim(), token[(eqIdx + 1)..].Trim()));
                        }
                        if (pairs.Count > 0) q.MatchingPairs = pairs;
                    }

                    if (type == QType.TrueFalse)
                    {
                        var isTrue = correctTexts.Contains("True");
                        q.Options = new List<Option> { new Option { Content = "True", IsCorrect = isTrue }, new Option { Content = "False", IsCorrect = !isTrue } };
                    }

                    await _svc.CreateAsync(q, actor);
                    existingKeys.Add(key); seenInThisFile.Add(key); res.Success++;
                }
                catch (Exception ex) { res.Errors.Add($"Row {row.RowNumber()}: {ex.Message}"); }
            }
            return res;
        }

        private static int? ParseNullableInt(string? s) => int.TryParse(s?.Trim(), out var v) ? v : null;
        private static List<string> SplitCsv(string? s, char sep) => string.IsNullOrWhiteSpace(s) ? new() : s.Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        private static List<string> SplitRaw(string? s, string delim) => string.IsNullOrWhiteSpace(s) ? new() : s.Split(delim, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
    }
}
