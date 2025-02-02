namespace FinalProjApi.Service.TokenJwt
{
    public interface ITokenService
    {
        Task StoreTokenAsync(string token, string username, string sessionId);
        Task InvalidateTokenAsync(string token, string sessionId);
        Task<bool> IsTokenValidAsync(string token);
    }
}
