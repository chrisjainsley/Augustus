using System.Text.Json;

namespace Augustus.Sample.StripeApi.Services;

public class StripeClient
{
    private readonly HttpClient _httpClient;

    public StripeClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JsonElement> CreateChargeAsync(int amount, string currency, string source)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["amount"] = amount.ToString(),
            ["currency"] = currency,
            ["source"] = source
        });

        var response = await _httpClient.PostAsync("/v1/charges", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    public async Task<JsonElement> GetCustomerAsync(string id)
    {
        var response = await _httpClient.GetAsync($"/v1/customers/{id}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    public async Task<JsonElement> CreateCustomerAsync(string name, string email)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = name,
            ["email"] = email
        });

        var response = await _httpClient.PostAsync("/v1/customers", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
