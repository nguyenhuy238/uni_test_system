using UniTestSystem.AdminApp.Models;
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
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);
                if (loginResponse != null)
                {
                    CurrentUser = loginResponse.user;
                    SetAuthToken(loginResponse.token);
                }
                return loginResponse;
            }
            
            return null;
        }

        public async Task<List<Test>?> GetTestsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/tests");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Test>>(responseContent);
            }
            
            return null;
        }

        public async Task<Test?> GetTestAsync(string id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/tests/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Test>(responseContent);
            }
            
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
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/users");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<User>>(responseContent);
            }
            
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
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/faculties");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Faculty>>(content);
            }
            return null;
        }

        public async Task<List<StudentClass>?> GetClassesAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/classes");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<StudentClass>>(content);
            }
            return null;
        }

        public async Task<Faculty?> CreateFacultyAsync(Faculty faculty)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/faculties", new StringContent(JsonSerializer.Serialize(faculty), Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode) return JsonSerializer.Deserialize<Faculty>(await response.Content.ReadAsStringAsync());
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
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/admin/classes", new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode) return JsonSerializer.Deserialize<StudentClass>(await response.Content.ReadAsStringAsync());
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

        // --- Question Bank ---

        public async Task<List<Question>?> GetQuestionsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/questions");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Question>>(content);
            }
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

        // --- Sessions ---

        public async Task<List<Session>?> GetSessionsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/sessions");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Session>>(content);
            }
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
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/admin/dashboard/summary");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DashboardStats>(content);
            }
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
    }
}
