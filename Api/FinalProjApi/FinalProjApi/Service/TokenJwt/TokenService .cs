using FinalProjApi.Data;
using FinalProjApi.Models;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace FinalProjApi.Service.TokenJwt
{
    public class TokenService : ITokenService
    {
        private readonly DataBaseContext _context;

        public TokenService(DataBaseContext context)
        {
            _context = context;
        }


        public async Task StoreTokenAsync(string token, string username, string sessionId)
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var expiryDate = jwtToken.ValidTo;

            var newToken = new UserToken
            {
                Token = token,
                Username = username,
                ExpiryDate = expiryDate,
                SessionId = sessionId
            };

            _context.UserTokens.Add(newToken);
            await _context.SaveChangesAsync();
        }

        public async Task InvalidateTokenAsync(string token, string sessionId)
        {
            var tokenToInvalidate = await _context.UserTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.SessionId == sessionId);

            if (tokenToInvalidate != null)
            {
                var invalidatedToken = new InvalidatedToken
                {
                    Token = token,
                    InvalidatedAt = DateTime.UtcNow
                };

                _context.InvalidatedTokens.Add(invalidatedToken);
                _context.UserTokens.Remove(tokenToInvalidate);
                await _context.SaveChangesAsync();
            }



        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            var invalidToken = await _context.InvalidatedTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            return invalidToken == null; // Token is valid if not in the invalidated tokens list
        }

    }
}
