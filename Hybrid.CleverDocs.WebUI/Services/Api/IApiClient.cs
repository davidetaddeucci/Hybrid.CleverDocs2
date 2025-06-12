using Hybrid.CleverDocs.WebUI.Models;

namespace Hybrid.CleverDocs.WebUI.Services.Api;

public interface IApiClient
{
    Task<ApiResponse<T>> GetAsync<T>(string endpoint);
    Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? data = null);
    Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data);
    Task<ApiResponse<T>> DeleteAsync<T>(string endpoint);
    Task<ApiResponse<PaginatedResponse<T>>> GetPaginatedAsync<T>(string endpoint, int page = 1, int pageSize = 25);
    Task<ApiResponse<FileUploadResult>> UploadFileAsync(string endpoint, Stream fileStream, string fileName, string contentType);
}