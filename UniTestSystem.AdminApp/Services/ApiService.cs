using UniTestSystem.AdminApp.Models;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UniTestSystem.AdminApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string? _jwtToken;
        public User? CurrentUser { get; private set; }

        public ApiService(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetAuthToken(string token)
        {
            _jwtToken = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<LoginResponse?> LoginAsync(string email, string password)
        {
            var loginRequest = new { email, password };
            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/auth/login", content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                // Access denied for this role
                return new LoginResponse { message = "Forbidden" };
            }

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseContent) || !responseContent.TrimStart().StartsWith("{"))
                    return null;

                try
                {
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);
                    if (loginResponse != null)
                    {
                        CurrentUser = loginResponse.user;
                        SetAuthToken(loginResponse.token);
                    }
                    return loginResponse;
                }
                catch { }
            }
            
            return null;
        }

        public async Task<List<Test>?> GetTestsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/tests");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(responseContent) || !responseContent.TrimStart().StartsWith("["))
                        return null;
                    return JsonSerializer.Deserialize<List<Test>>(responseContent);
                }
            }
            catch { }
            return null;
        }

        public async Task<Test?> GetTestAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/tests/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(responseContent) || !responseContent.TrimStart().StartsWith("{"))
                        return null;
                    return JsonSerializer.Deserialize<Test>(responseContent);
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> CreateTestAsync(Test test)
        {
            var json = JsonSerializer.Serialize(test);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/tests", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTestAsync(string id, Test test)
        {
            var json = JsonSerializer.Serialize(test);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/api/admin/tests/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteTestAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/admin/tests/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<User>?> GetUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/users");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(responseContent) || !responseContent.TrimStart().StartsWith("["))
                        return null;
                    return JsonSerializer.Deserialize<List<User>>(responseContent);
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> CreateUserAsync(User user, string password)
        {
            var createRequest = new
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

            var json = JsonSerializer.Serialize(createRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/users", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateUserAsync(string id, User user, string? password = null)
        {
            var updateRequest = new
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

            var json = JsonSerializer.Serialize(updateRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/api/admin/users/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/admin/users/{id}");
            return response.IsSuccessStatusCode;
        }

        // --- Academic ---

        public async Task<List<Faculty>?> GetFacultiesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/faculties");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content) || !content.TrimStart().StartsWith("["))
                        return null;
                    return JsonSerializer.Deserialize<List<Faculty>>(content);
                }
            }
            catch { }
            return null;
        }

        public async Task<List<StudentClass>?> GetClassesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/classes");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content) || !content.TrimStart().StartsWith("["))
                        return null;
                    return JsonSerializer.Deserialize<List<StudentClass>>(content);
                }
            }
            catch { }
            return null;
        }

        public async Task<Faculty?> CreateFacultyAsync(Faculty faculty)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/faculties", new StringContent(JsonSerializer.Serialize(faculty), Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(content) && content.TrimStart().StartsWith("{"))
                        return JsonSerializer.Deserialize<Faculty>(content);
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> UpdateFacultyAsync(string id, Faculty faculty)
        {
            var response = await _httpClient.PutAsync($"{_baseUrl}/api/admin/faculties/{id}", new StringContent(JsonSerializer.Serialize(faculty), Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteFacultyAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/admin/faculties/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<StudentClass?> CreateClassAsync(StudentClass model)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/classes", new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(content) && content.TrimStart().StartsWith("{"))
                        return JsonSerializer.Deserialize<StudentClass>(content);
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> UpdateClassAsync(string id, StudentClass model)
        {
            var response = await _httpClient.PutAsync($"{_baseUrl}/api/admin/classes/{id}", new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteClassAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/admin/classes/{id}");
            return response.IsSuccessStatusCode;
        }

        // --- Courses ---

        public async Task<List<Course>?> GetCoursesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/courses");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content) || !content.TrimStart().StartsWith("["))
                        return null;
                    return JsonSerializer.Deserialize<List<Course>>(content);
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> CreateCourseAsync(Course course)
        {
            var json = JsonSerializer.Serialize(course);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/courses", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateCourseAsync(string id, Course course)
        {
            var json = JsonSerializer.Serialize(course);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/api/admin/courses/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteCourseAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/admin/courses/{id}");
            return response.IsSuccessStatusCode;
        }

        // --- Enrollments ---

        public async Task<List<Enrollment>?> GetEnrollmentsAsync(string courseId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/enrollments/{courseId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content) || !content.TrimStart().StartsWith("["))
                        return null;
                    return JsonSerializer.Deserialize<List<Enrollment>>(content);
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> EnrollStudentAsync(string studentId, string courseId, string semester)
        {
            var request = new { studentId, courseId, semester };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/enrollments", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UnenrollStudentAsync(string studentId, string courseId)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/admin/enrollments/{studentId}/{courseId}");
            return response.IsSuccessStatusCode;
        }

        // --- Import ---

        public async Task<bool> ImportStudentsAsync(string filePath, string? classId = null)
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            var url = $"{_baseUrl}/api/admin/import/students";
            if (!string.IsNullOrEmpty(classId)) url += $"?classId={classId}";

            var response = await _httpClient.PostAsync(url, form);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ImportCoursesAsync(string filePath)
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/import/courses", form);
            return response.IsSuccessStatusCode;
        }

        // --- Question Bank ---

        public async Task<List<Question>?> GetQuestionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/questions");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content) || !content.TrimStart().StartsWith("["))
                        return null;
                    return JsonSerializer.Deserialize<List<Question>>(content);
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> CreateQuestionAsync(Question question)
        {
            var json = JsonSerializer.Serialize(question);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/questions", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateQuestionAsync(string id, Question question)
        {
            var json = JsonSerializer.Serialize(question);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/api/admin/questions/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteQuestionAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/admin/questions/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SubmitQuestionAsync(string id)
        {
            // Note: Controller action is [HttpPost] Questions/Submit/{id}
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/questions/submit/{id}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ApproveQuestionAsync(string id)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/questions/approve/{id}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RejectQuestionAsync(string id, string reason)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/questions/reject/{id}?reason={Uri.EscapeDataString(reason)}", null);
            return response.IsSuccessStatusCode;
        }

        // --- Sessions ---

        public async Task<List<Session>?> GetSessionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/sessions");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content) || !content.TrimStart().StartsWith("["))
                        return null;
                    return JsonSerializer.Deserialize<List<Session>>(content);
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> DeleteSessionAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/admin/sessions/{id}");
            return response.IsSuccessStatusCode;
        }

        // --- Dashboard ---

        public async Task<DashboardStats?> GetDashboardStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/dashboard/summary");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content) || !content.TrimStart().StartsWith("{"))
                        return null;
                    return JsonSerializer.Deserialize<DashboardStats>(content);
                }
            }
            catch { }
            return null;
        }

        // --- Exports ---

        public async Task<byte[]?> DownloadReportXlsxAsync(string? from = null, string? to = null)
        {
            var url = $"{_baseUrl}/reports/export/xlsx?from={from}&to={to}";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }

        public async Task<byte[]?> DownloadReportPdfAsync(string? from = null, string? to = null)
        {
            var url = $"{_baseUrl}/reports/export/pdf?from={from}&to={to}";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
    }

    public class LoginResponse
    {
        public string token { get; set; } = "";
        public User user { get; set; } = new User();
        public string? message { get; set; }
    }
}
