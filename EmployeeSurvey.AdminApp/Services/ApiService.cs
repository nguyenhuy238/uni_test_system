using EmployeeSurvey.AdminApp.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EmployeeSurvey.AdminApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string? _jwtToken;

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
                return JsonSerializer.Deserialize<LoginResponse>(responseContent);
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
    }

    public class LoginResponse
    {
        public string token { get; set; } = "";
        public User user { get; set; } = new User();
    }
}
