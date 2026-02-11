using F360.Domain.Dtos.Responses.External;

namespace F360.Domain.Interfaces.HttpClients;

public interface IViaCepClient
{
    Task<ViaCepResponse?> GetAddressAsync(string cep, CancellationToken cancellationToken);
}
