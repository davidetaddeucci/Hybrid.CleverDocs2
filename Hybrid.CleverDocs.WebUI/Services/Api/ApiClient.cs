using Blazored.LocalStorage;
using Hybrid.CleverDocs.WebUI.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Hybrid.CleverDocs.WebUI.Services.Api;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _configuration = configuration;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Configure base address
        var baseUrl = _configuration["ApiSettings:BaseUrl"];
        if (!string.IsNullOrEmpty(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        // Configure timeout
        var timeout = _configuration.GetValue<int>("ApiSettings:Timeout", 30);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        return await SendRequestAsync<T>(HttpMethod.Get, endpoint);
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? data = null)
    {
        return await SendRequestAsync<T>(HttpMethod.Post, endpoint, data);
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
    {
        return await SendRequestAsync<T>(HttpMethod.Put, endpoint, data);
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
    {
        return await SendRequestAsync<T>(HttpMethod.Delete, endpoint);
    }

    public async Task<ApiResponse<PaginatedResponse<T>>> GetPaginatedAsync<T>(string endpoint, int page = 1, int pageSize = 25)
    {
        var url = $"{endpoint}?page={page}&pageSize={pageSize}";
        return await SendRequestAsync<PaginatedResponse<T>>(HttpMethod.Get, url);
    }

    public async Task<ApiResponse<FileUploadResult>> UploadFileAsync(string endpoint, Stream fileStream, string fileName, string contentType)
    {
        try
        {
            await AddAuthorizationHeaderAsync();

            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<FileUploadResult>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<FileUploadResult>(false, Message: "Risposta non valida dal server");
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<ApiResponse<FileUploadResult>>(responseContent, _jsonOptions);
                return errorResponse ?? new ApiResponse<FileUploadResult>(false, Message: $"Errore HTTP: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<FileUploadResult>(false, Message: $"Errore durante l'upload: {ex.Message}");
        }
    }

    private async Task<ApiResponse<T>> SendRequestAsync<T>(HttpMethod method, string endpoint, object? data = null)
    {
        try
        {
            await AddAuthorizationHeaderAsync();

            using var request = new HttpRequestMessage(method, endpoint);

            if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrEmpty(responseContent))
                {
                    return new ApiResponse<T>(true);
                }

                var result = JsonSerializer.Deserialize<ApiResponse<T>>(responseContent, _jsonOptions);
                return result ?? new ApiResponse<T>(false, Message: "Risposta non valida dal server");
            }
            else
            {
                // Handle specific HTTP status codes
                return response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => new ApiResponse<T>(false, Message: "Non autorizzato. Effettua nuovamente il login."),
                    System.Net.HttpStatusCode.Forbidden => new ApiResponse<T>(false, Message: "Accesso negato. Non hai i permessi necessari."),
                    System.Net.HttpStatusCode.NotFound => new ApiResponse<T>(false, Message: "Risorsa non trovata."),
                    System.Net.HttpStatusCode.BadRequest => await ParseErrorResponseAsync<T>(responseContent),
                    _ => new ApiResponse<T>(false, Message: $"Errore del server: {response.StatusCode}")
                };
            }
        }
        catch (HttpRequestException ex)
        {
            return new ApiResponse<T>(false, Message: $"Errore di connessione: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return new ApiResponse<T>(false, Message: "Timeout della richiesta. Riprova più tardi.");
        }
        catch (Exception ex)
        {
            return new ApiResponse<T>(false, Message: $"Errore imprevisto: {ex.Message}");
        }
    }

    private async Task<ApiResponse<T>> ParseErrorResponseAsync<T>(string responseContent)
    {
        try
        {
            var errorResponse = JsonSerializer.Deserialize<ApiResponse<T>>(responseContent, _jsonOptions);
            return errorResponse ?? new ApiResponse<T>(false, Message: "Errore sconosciuto");
        }
        catch
        {
            return new ApiResponse<T>(false, Message: "Errore nella richiesta");
        }
    }

    private async Task AddAuthorizationHeaderAsync()
    {
        var tokenKey = _configuration["Authentication:TokenStorageKey"] ?? "auth_token";
        var token = await _localStorage.GetItemAsync<string>(tokenKey);

        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}