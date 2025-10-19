using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OrderManagementSystem.Models.ViewModels;
using OrderManagementSystem.Services.Interfaces;

namespace OrderManagementSystem.Services.Implementations
{
    public class SteadfastService : ISteadfastService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _secretKey;

        public SteadfastService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _baseUrl = configuration["Steadfast:BaseUrl"] ?? throw new ArgumentNullException("Steadfast BaseUrl not configured");
            _apiKey = configuration["Steadfast:ApiKey"] ?? throw new ArgumentNullException("Steadfast ApiKey not configured");
            _secretKey = configuration["Steadfast:SecretKey"] ?? throw new ArgumentNullException("Steadfast SecretKey not configured");

            // Configure HTTP client headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Api-Key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("Secret-Key", _secretKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<SteadfastOrderResponse> CreateOrder(SteadfastOrderRequest request)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/create_order", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<SteadfastOrderResponse>(responseContent);
                    return result ?? throw new Exception("Failed to deserialize Steadfast response");
                }
                else
                {
                    throw new Exception($"Steadfast API Error: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Steadfast API Error: {ex.Message}");
                throw;
            }
        }

        public async Task<SteadfastStatusResponse> CheckDeliveryStatus(string trackingCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/status_by_trackingcode/{trackingCode}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<SteadfastStatusResponse>(responseContent);
                    return result ?? throw new Exception("Failed to deserialize status response");
                }
                else
                {
                    throw new Exception($"Steadfast Status Check Error: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Steadfast Status Check Error: {ex.Message}");
                throw;
            }
        }

        public async Task<SteadfastBalanceResponse> GetCurrentBalance()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/get_balance");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<SteadfastBalanceResponse>(responseContent);
                    return result ?? throw new Exception("Failed to deserialize balance response");
                }
                else
                {
                    throw new Exception($"Steadfast Balance Check Error: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Steadfast Balance Check Error: {ex.Message}");
                throw;
            }
        }
    }
}