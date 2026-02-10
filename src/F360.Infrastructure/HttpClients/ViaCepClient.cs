using F360.Domain.Dtos.Responses.External;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace F360.Infrastructure.HttpClients;

public class ViaCepClient(HttpClient httpClient, ILogger<ViaCepClient> logger)
{
    public async Task<ViaCepResponse?> GetAddressAsync(string cep)
    {
        var cleanCep = cep.Replace("-", "");

        var response = await httpClient.GetAsync($"https://viacep.com.br/ws/{cleanCep}/json/");

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to fetch address for CEP {Cep}. Response: {Response}", cep, response);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ViaCepResponse>();
    }
}
