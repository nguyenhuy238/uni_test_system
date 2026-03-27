using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;
using UniTestSystem.AdminApp.Models;

namespace UniTestSystem.AdminApp.Services
{
    public class ApiService
    {
        private const string AuthStateFileName = "auth_state.bin";
        private static readonly byte[] AuthStateEntropy = Encoding.UTF8.GetBytes("UniTestSystem.AdminApp.Auth.v1");

        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        private string? _jwtToken;
        private string? _refreshToken;
        private DateTimeOffset? _accessTokenExpiresAtUtc;

        public User? CurrentUser { get; private set; }

        public ApiService(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            TryLoadAuthStateFromDisk();
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            if (CurrentUser == null || string.IsNullOrWhiteSpace(_jwtToken))
            {
                return false;
            }

            var ok = await EnsureValidTokenAsync();
            if (!ok)
            {
                ClearAuthState();
            }

            return ok;
        }

        public async Task<LoginResponse?> LoginAsync(string email, string password)
        {
            var payload = new { email, password };
            using var response = await SendJsonRawAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/auth/login", payload, requiresAuth: false);

            if (response == null)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new LoginResponse { message = "Forbidden" };
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var login = await ReadJsonAsync<LoginResponse>(response);
            if (login?.user == null || string.IsNullOrWhiteSpace(login.token))
            {
                return null;
            }

            SetAuthState(login.token, login.refreshToken, login.user);
            return login;
        }

        public async Task LogoutAsync()
        {
            if (!string.IsNullOrWhiteSpace(_refreshToken))
            {
                var payload = new { refreshToken = _refreshToken };
                using var _ = await SendJsonRawAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/auth/logout", payload, requiresAuth: true);
            }

            ClearAuthState();
        }

        public async Task<List<Test>?> GetTestsAsync() => await GetJsonAsync<List<Test>>($"{_baseUrl}/api/admin/tests");
        public async Task<List<User>?> GetUsersAsync() => await GetJsonAsync<List<User>>($"{_baseUrl}/api/admin/users");
        public async Task<List<Question>?> GetQuestionsAsync() => await GetJsonAsync<List<Question>>($"{_baseUrl}/api/admin/questions");
        public async Task<List<Session>?> GetSessionsAsync() => await GetJsonAsync<List<Session>>($"{_baseUrl}/api/admin/sessions");
        public async Task<DashboardStats?> GetDashboardStatsAsync() => await GetJsonAsync<DashboardStats>($"{_baseUrl}/api/admin/dashboard/summary");
        public async Task<List<Faculty>?> GetFacultiesAsync() => await GetJsonAsync<List<Faculty>>($"{_baseUrl}/api/admin/faculties");
        public async Task<List<StudentClass>?> GetClassesAsync() => await GetJsonAsync<List<StudentClass>>($"{_baseUrl}/api/admin/classes");
        public async Task<List<Course>?> GetCoursesAsync() => await GetJsonAsync<List<Course>>($"{_baseUrl}/api/admin/courses");
        public async Task<List<Enrollment>?> GetEnrollmentsAsync(string courseId) => await GetJsonAsync<List<Enrollment>>($"{_baseUrl}/api/admin/enrollments/{courseId}");
        public async Task<List<AuditLogEntry>?> GetAuditLogsAsync(string? from = null, string? to = null, string? keyword = null, string? actor = null, int take = 300)
            => await GetJsonAsync<List<AuditLogEntry>>($"{_baseUrl}/api/admin/audit?from={Uri.EscapeDataString(from ?? "")}&to={Uri.EscapeDataString(to ?? "")}&keyword={Uri.EscapeDataString(keyword ?? "")}&actor={Uri.EscapeDataString(actor ?? "")}&take={take}");
        public async Task<SystemSettingsModel?> GetSystemSettingsAsync() => await GetJsonAsync<SystemSettingsModel>($"{_baseUrl}/api/admin/system-settings");
        public async Task<List<BackupFileInfo>?> GetBackupsAsync() => await GetJsonAsync<List<BackupFileInfo>>($"{_baseUrl}/api/admin/maintenance/backups");
        public async Task<List<ExamSchedule>?> GetExamSchedulesAsync() => await GetJsonAsync<List<ExamSchedule>>($"{_baseUrl}/api/admin/exam-schedules");
        public async Task<YearEndPreviewModel?> GetYearEndPreviewAsync(string academicYear, string? facultyId = null)
            => await GetJsonAsync<YearEndPreviewModel>(
                $"{_baseUrl}/api/admin/transcripts/year-end/preview?academicYear={Uri.EscapeDataString(academicYear)}&facultyId={Uri.EscapeDataString(facultyId ?? "")}");

        public async Task<bool> CreateTestAsync(Test test) => await SendJsonAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/tests", test);
        public async Task<bool> UpdateTestAsync(string id, Test test) => await SendJsonAsync(HttpMethod.Put, $"{_baseUrl}/api/admin/tests/{id}", test);
        public async Task<bool> DeleteTestAsync(string id) => await SendWithoutBodyAsync(HttpMethod.Delete, $"{_baseUrl}/api/admin/tests/{id}");

        public async Task<bool> CreateUserAsync(User user, string password)
        {
            var payload = new
            {
                user.Name,
                user.Email,
                user.Role,
                user.Department,
                user.Level,
                user.Skill,
                user.TeamId,
                Password = password
            };
            return await SendJsonAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/users", payload);
        }

        public async Task<bool> UpdateUserAsync(string id, User user, string? password = null)
        {
            var payload = new
            {
                user.Name,
                user.Email,
                user.Role,
                user.Department,
                user.Level,
                user.Skill,
                user.TeamId,
                Password = password
            };
            return await SendJsonAsync(HttpMethod.Put, $"{_baseUrl}/api/admin/users/{id}", payload);
        }

        public async Task<bool> DeleteUserAsync(string id) => await SendWithoutBodyAsync(HttpMethod.Delete, $"{_baseUrl}/api/admin/users/{id}");

        public async Task<bool> CreateQuestionAsync(Question question) => await SendJsonAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/questions", question);
        public async Task<bool> UpdateQuestionAsync(string id, Question question) => await SendJsonAsync(HttpMethod.Put, $"{_baseUrl}/api/admin/questions/{id}", question);
        public async Task<bool> DeleteQuestionAsync(string id) => await SendWithoutBodyAsync(HttpMethod.Delete, $"{_baseUrl}/api/admin/questions/{id}");
        public async Task<bool> SubmitQuestionAsync(string id) => await SendWithoutBodyAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/questions/submit/{id}");
        public async Task<bool> ApproveQuestionAsync(string id) => await SendWithoutBodyAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/questions/approve/{id}");
        public async Task<bool> RejectQuestionAsync(string id, string reason) => await SendWithoutBodyAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/questions/reject/{id}?reason={Uri.EscapeDataString(reason)}");

        public async Task<bool> DeleteSessionAsync(string id) => await SendWithoutBodyAsync(HttpMethod.Delete, $"{_baseUrl}/api/admin/sessions/{id}");

        public async Task<Faculty?> CreateFacultyAsync(Faculty faculty) => await SendJsonReadAsync<Faculty>(HttpMethod.Post, $"{_baseUrl}/api/admin/faculties", faculty);
        public async Task<bool> UpdateFacultyAsync(string id, Faculty faculty) => await SendJsonAsync(HttpMethod.Put, $"{_baseUrl}/api/admin/faculties/{id}", faculty);
        public async Task<bool> DeleteFacultyAsync(string id) => await SendWithoutBodyAsync(HttpMethod.Delete, $"{_baseUrl}/api/admin/faculties/{id}");

        public async Task<StudentClass?> CreateClassAsync(StudentClass model) => await SendJsonReadAsync<StudentClass>(HttpMethod.Post, $"{_baseUrl}/api/admin/classes", model);
        public async Task<bool> UpdateClassAsync(string id, StudentClass model) => await SendJsonAsync(HttpMethod.Put, $"{_baseUrl}/api/admin/classes/{id}", model);
        public async Task<bool> DeleteClassAsync(string id) => await SendWithoutBodyAsync(HttpMethod.Delete, $"{_baseUrl}/api/admin/classes/{id}");

        public async Task<bool> CreateCourseAsync(Course course) => await SendJsonAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/courses", course);
        public async Task<bool> UpdateCourseAsync(string id, Course course) => await SendJsonAsync(HttpMethod.Put, $"{_baseUrl}/api/admin/courses/{id}", course);
        public async Task<bool> DeleteCourseAsync(string id) => await SendWithoutBodyAsync(HttpMethod.Delete, $"{_baseUrl}/api/admin/courses/{id}");

        public async Task<bool> EnrollStudentAsync(string studentId, string courseId, string semester)
        {
            var payload = new { studentId, courseId, semester };
            return await SendJsonAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/enrollments", payload);
        }

        public async Task<bool> UnenrollStudentAsync(string studentId, string courseId)
            => await SendWithoutBodyAsync(HttpMethod.Delete, $"{_baseUrl}/api/admin/enrollments/{studentId}/{courseId}");

        public async Task<bool> ImportStudentsAsync(string filePath, string? classId = null)
        {
            var url = $"{_baseUrl}/api/admin/import/students";
            if (!string.IsNullOrWhiteSpace(classId))
            {
                url += $"?classId={Uri.EscapeDataString(classId)}";
            }

            return await UploadFileAsync(url, filePath, "file");
        }

        public async Task<bool> ImportCoursesAsync(string filePath)
            => await UploadFileAsync($"{_baseUrl}/api/admin/import/courses", filePath, "file");

        public async Task<byte[]?> DownloadReportXlsxAsync(string? from = null, string? to = null)
            => await DownloadBytesAsync($"{_baseUrl}/reports/export/xlsx?from={Uri.EscapeDataString(from ?? "")}&to={Uri.EscapeDataString(to ?? "")}");

        public async Task<byte[]?> DownloadReportPdfAsync(string? from = null, string? to = null)
            => await DownloadBytesAsync($"{_baseUrl}/reports/export/pdf?from={Uri.EscapeDataString(from ?? "")}&to={Uri.EscapeDataString(to ?? "")}");

        public async Task<bool> SaveSystemSettingsAsync(SystemSettingsModel settings)
        {
            var payload = new
            {
                settings.SystemName,
                settings.CurrentSemester,
                settings.CurrentAcademicYear,
                settings.WarningGpaThreshold,
                settings.FailGpaThreshold,
                settings.TreatOutstandingDebtAsFail,
                settings.LogoUrl
            };
            return await SendJsonAsync(HttpMethod.Put, $"{_baseUrl}/api/admin/system-settings", payload);
        }

        public async Task<BackupResult?> CreateBackupAsync(string? fileName = null)
        {
            var payload = new { fileName };
            return await SendJsonReadAsync<BackupResult>(HttpMethod.Post, $"{_baseUrl}/api/admin/maintenance/backup", payload);
        }

        public async Task<BackupResult?> RestoreBackupAsync(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
            {
                return null;
            }

            return await UploadFileReadAsync<BackupResult>($"{_baseUrl}/api/admin/maintenance/restore", backupFilePath, "file");
        }

        public async Task<bool> CreateExamScheduleAsync(ExamScheduleDraft draft)
        {
            var payload = new
            {
                draft.TestId,
                draft.CourseId,
                draft.Room,
                draft.StartTime,
                draft.EndTime,
                draft.ExamType
            };
            return await SendJsonAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/exam-schedules", payload);
        }

        public async Task<bool> BulkCreateExamSchedulesAsync(IEnumerable<ExamScheduleDraft> drafts)
        {
            var payload = new
            {
                items = drafts.Select(d => new
                {
                    d.TestId,
                    d.CourseId,
                    d.Room,
                    d.StartTime,
                    d.EndTime,
                    d.ExamType
                }).ToList()
            };

            return await SendJsonAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/exam-schedules/bulk", payload);
        }

        public async Task<bool> DeleteExamScheduleAsync(string id)
            => await SendWithoutBodyAsync(HttpMethod.Delete, $"{_baseUrl}/api/admin/exam-schedules/{id}");

        public async Task<byte[]?> DownloadExamScheduleCsvAsync()
            => await DownloadBytesAsync($"{_baseUrl}/api/admin/exam-schedules/export/csv");

        public async Task<YearEndFinalizeResultModel?> FinalizeYearEndAsync(string academicYear, string? facultyId = null)
        {
            var payload = new { academicYear, facultyId };
            using var response = await SendJsonRawAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/transcripts/year-end/finalize", payload, requiresAuth: true);
            if (response == null)
            {
                return null;
            }

            return await ReadJsonAsync<YearEndFinalizeResultModel>(response);
        }

        private void SetAuthState(string accessToken, string? refreshToken, User user)
        {
            _jwtToken = accessToken;
            _refreshToken = refreshToken;
            _accessTokenExpiresAtUtc = TryGetTokenValidTo(accessToken);
            CurrentUser = user;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            PersistAuthStateToDisk();
        }

        private void ClearAuthState()
        {
            _jwtToken = null;
            _refreshToken = null;
            _accessTokenExpiresAtUtc = null;
            CurrentUser = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;

            try
            {
                var path = GetAuthStateFilePath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private async Task<bool> EnsureValidTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(_jwtToken))
            {
                return false;
            }

            if (!_accessTokenExpiresAtUtc.HasValue || _accessTokenExpiresAtUtc.Value > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return true;
            }

            return await RefreshAccessTokenAsync();
        }

        private async Task<bool> RefreshAccessTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(_refreshToken))
            {
                return false;
            }

            var payload = new { refreshToken = _refreshToken };
            using var response = await SendJsonRawAsync(HttpMethod.Post, $"{_baseUrl}/api/admin/auth/refresh", payload, requiresAuth: false);
            if (response == null || !response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await ReadJsonAsync<RefreshLoginResponse>(response);
            if (result == null || string.IsNullOrWhiteSpace(result.token))
            {
                return false;
            }

            SetAuthState(result.token, result.refreshToken ?? _refreshToken, CurrentUser ?? new User());
            return true;
        }

        private async Task<HttpResponseMessage?> SendJsonRawAsync(HttpMethod method, string url, object payload, bool requiresAuth)
        {
            return await SendWithRetryAsync(
                () =>
                {
                    var json = JsonSerializer.Serialize(payload);
                    return new HttpRequestMessage(method, url)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                },
                requiresAuth);
        }

        private async Task<bool> SendJsonAsync(HttpMethod method, string url, object payload)
        {
            using var response = await SendJsonRawAsync(method, url, payload, requiresAuth: true);
            return response?.IsSuccessStatusCode == true;
        }

        private async Task<T?> SendJsonReadAsync<T>(HttpMethod method, string url, object payload)
        {
            using var response = await SendJsonRawAsync(method, url, payload, requiresAuth: true);
            if (response == null || !response.IsSuccessStatusCode)
            {
                return default;
            }

            return await ReadJsonAsync<T>(response);
        }

        private async Task<bool> SendWithoutBodyAsync(HttpMethod method, string url)
        {
            using var response = await SendWithRetryAsync(() => new HttpRequestMessage(method, url), requiresAuth: true);
            return response?.IsSuccessStatusCode == true;
        }

        private async Task<T?> GetJsonAsync<T>(string url)
        {
            using var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, url), requiresAuth: true);
            if (response == null || !response.IsSuccessStatusCode)
            {
                return default;
            }

            return await ReadJsonAsync<T>(response);
        }

        private async Task<byte[]?> DownloadBytesAsync(string url)
        {
            using var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, url), requiresAuth: true);
            if (response == null || !response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync();
        }

        private async Task<bool> UploadFileAsync(string url, string filePath, string fieldName)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            using var response = await SendWithRetryAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    var form = new MultipartFormDataContent();
                    var fileBytes = File.ReadAllBytes(filePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    form.Add(fileContent, fieldName, Path.GetFileName(filePath));
                    request.Content = form;
                    return request;
                },
                requiresAuth: true);

            return response?.IsSuccessStatusCode == true;
        }

        private async Task<T?> UploadFileReadAsync<T>(string url, string filePath, string fieldName)
        {
            if (!File.Exists(filePath))
            {
                return default;
            }

            using var response = await SendWithRetryAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    var form = new MultipartFormDataContent();
                    var fileBytes = File.ReadAllBytes(filePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    form.Add(fileContent, fieldName, Path.GetFileName(filePath));
                    request.Content = form;
                    return request;
                },
                requiresAuth: true);

            if (response == null || !response.IsSuccessStatusCode)
            {
                return default;
            }

            return await ReadJsonAsync<T>(response);
        }

        private async Task<HttpResponseMessage?> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory, bool requiresAuth)
        {
            if (requiresAuth && !await EnsureValidTokenAsync())
            {
                return null;
            }

            async Task<HttpResponseMessage?> SendOnceAsync()
            {
                using var request = requestFactory();
                return await _httpClient.SendAsync(request);
            }

            var response = await SendOnceAsync();
            if (!requiresAuth || response == null || response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            var refreshed = await RefreshAccessTokenAsync();
            if (!refreshed)
            {
                return response;
            }

            response.Dispose();
            return await SendOnceAsync();
        }

        private async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        private void PersistAuthStateToDisk()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_jwtToken) || CurrentUser == null)
                {
                    return;
                }

                var state = new StoredAuthState
                {
                    AccessToken = _jwtToken,
                    RefreshToken = _refreshToken,
                    AccessTokenExpiresAtUtc = _accessTokenExpiresAtUtc,
                    User = CurrentUser
                };

                var json = JsonSerializer.Serialize(state, _jsonOptions);
                var protectedBytes = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(json),
                    AuthStateEntropy,
                    DataProtectionScope.CurrentUser);

                var path = GetAuthStateFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, protectedBytes);
            }
            catch
            {
            }
        }

        private void TryLoadAuthStateFromDisk()
        {
            try
            {
                var path = GetAuthStateFilePath();
                if (!File.Exists(path))
                {
                    return;
                }

                var protectedBytes = File.ReadAllBytes(path);
                var rawBytes = ProtectedData.Unprotect(protectedBytes, AuthStateEntropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(rawBytes);
                var state = JsonSerializer.Deserialize<StoredAuthState>(json, _jsonOptions);
                if (state?.User == null || string.IsNullOrWhiteSpace(state.AccessToken))
                {
                    return;
                }

                _jwtToken = state.AccessToken;
                _refreshToken = state.RefreshToken;
                _accessTokenExpiresAtUtc = state.AccessTokenExpiresAtUtc ?? TryGetTokenValidTo(state.AccessToken);
                CurrentUser = state.User;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
            }
            catch
            {
                ClearAuthState();
            }
        }

        private static DateTimeOffset? TryGetTokenValidTo(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2)
                {
                    return null;
                }

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var bytes = Convert.FromBase64String(payload);
                using var doc = JsonDocument.Parse(bytes);
                if (!doc.RootElement.TryGetProperty("exp", out var expElement))
                {
                    return null;
                }

                long expUnix;
                if (expElement.ValueKind == JsonValueKind.Number)
                {
                    expUnix = expElement.GetInt64();
                }
                else if (expElement.ValueKind == JsonValueKind.String &&
                         long.TryParse(expElement.GetString(), out var parsed))
                {
                    expUnix = parsed;
                }
                else
                {
                    return null;
                }

                return DateTimeOffset.FromUnixTimeSeconds(expUnix);
            }
            catch
            {
                return null;
            }
        }

        private static string GetAuthStateFilePath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "UniTestSystem", "AdminApp", AuthStateFileName);
        }
    }

    public class LoginResponse
    {
        public string token { get; set; } = "";
        public string? refreshToken { get; set; }
        public User user { get; set; } = new User();
        public string? message { get; set; }
    }

    internal class RefreshLoginResponse
    {
        public string token { get; set; } = "";
        public string? refreshToken { get; set; }
    }

    internal class StoredAuthState
    {
        public string AccessToken { get; set; } = "";
        public string? RefreshToken { get; set; }
        public DateTimeOffset? AccessTokenExpiresAtUtc { get; set; }
        public User User { get; set; } = new User();
    }
}
