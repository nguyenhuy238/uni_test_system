using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace EmployeeSurvey.AdminApp.Services;

public class ApiService
{
    private readonly HttpClient _client;
    private string? _token;

    public ApiService()
    {
        _client = new HttpClient { BaseAddress = new Uri("https://localhost:7158/") };
    }

    public void SetToken(string token)
    {
        _token = token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task<(bool ok, string? token, dynamic? user)> LoginAsync(string email, string password)
    {
        var json = JsonConvert.SerializeObject(new { email, password });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("api/auth/login", content);
        if (response.IsSuccessStatusCode)
        {
            var resJson = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(resJson);
            if (data != null)
            {
                _token = data.token;
                SetToken(_token!);
                return (true, _token, data.user);
            }
        }
        return (false, null, null);
    }

    public async Task<List<T>> GetAsync<T>(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
    }

    public async Task<T?> GetByIdAsync<T>(string endpoint, string id)
    {
        var response = await _client.GetAsync($"{endpoint}/{id}");
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(json);
    }

    public async Task PostAsync<T>(string endpoint, T data)
    {
        var json = JsonConvert.SerializeObject(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
    }

    public async Task PutAsync<T>(string endpoint, string id, T data)
    {
        var json = JsonConvert.SerializeObject(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"{endpoint}/{id}", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string endpoint, string id)
    {
        var response = await _client.DeleteAsync($"{endpoint}/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> DownloadFileAsync(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
