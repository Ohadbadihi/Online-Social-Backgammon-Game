using FinalProjApi.Dto;
using FinalProjApi.Models;
using FinalProjApi.Service.TokenJwt;
using FinalProjApi.Service.UserLogic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinalProjApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;
        private readonly ITokenService _tokenService;

        public AuthController(IUserService userService, IConfiguration configuration, ILogger<AuthController> logger, ITokenService tokenService)
        {
            _userService = userService;
            _logger = logger;
            _configuration = configuration;
            _tokenService = tokenService;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDtoLogin dtoLogin)
        {
            try
            {


                var user = await _userService.Login(dtoLogin.Username, dtoLogin.Password);
                if (user != null)
                {
                    if (await _userService.CheckIfUserOnline(dtoLogin.Username))
                    {
                        await _userService.SetUserOnline(dtoLogin.Username);
                    }

                    string jwt = GenerateToken(user, dtoLogin.RememberMe);
                    var sessionId = Guid.NewGuid().ToString();
                    await _tokenService.StoreTokenAsync(jwt, user.Username, sessionId);
                    CreateCookie(jwt, dtoLogin.RememberMe);
                    _logger.LogInformation("User {Username} logged in successfully.", dtoLogin.Username);
                    return Ok(new { message = "Login successful", username = dtoLogin.Username, SessionId = sessionId });

                }
                _logger.LogWarning("Invalid login attempt for user {Username}.", dtoLogin.Username);
                return BadRequest(new { errorCode = "INVALID_CREDENTIALS", message = "Username or Password is incorrect" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login for user {Username}.", dtoLogin.Username);
                return StatusCode(500, "An error occurred while processing your request.");
            }

        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDtoLogin registerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration attempt. Model state is invalid.");
                return BadRequest(ModelState);
            }

            try
            {
                var isUserExist = await _userService.Register(registerDto.Username, registerDto.Password); 
                if (isUserExist)
                {
                    _logger.LogWarning("Registration attempt failed. Username {Username} is already taken.", registerDto.Username);
                    return Conflict("Username is already taken.");
                }
                else
                {
                    _logger.LogInformation("User {Username} registered successfully.", registerDto.Username);
                    return Ok(new { message = "Registration successful!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during registration for user {Username}.", registerDto.Username);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] UserDtoLogout userLogout)
        {
            try
            {
                var token = Request.Cookies["JWT"];
                if (!string.IsNullOrEmpty(token))
                {
                    await _tokenService.InvalidateTokenAsync(token, userLogout.SessionId);
                    _logger.LogInformation("Token invalidated for user {Username}.", userLogout.UserName);
                }
                Response.Cookies.Delete("JWT", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddDays(-1)
                });
                _logger.LogInformation("User {Username} logged out successfully.", userLogout.UserName);
                return Ok(new { message = "Logout successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during logout for user {Username}.", userLogout.UserName);
                return StatusCode(500, "An error occurred while processing your request.");
            }

        }



        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                var token = Request.Cookies["JWT"];
                if (token == null)
                {
                    _logger.LogWarning("Token validation failed: JWT token not found.");
                    return Unauthorized("JWT token not found.");
                }

                var isTokenValid = await _tokenService.IsTokenValidAsync(token);
                if (!isTokenValid)
                {
                    _logger.LogWarning("Token validation failed: Token has been invalidated.");
                    return Unauthorized("Token has been invalidated.");
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["JWT:Key"]!);


                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration["JWT:Issuer"],
                    ValidAudience = _configuration["JWT:Audience"],
                    ClockSkew = TimeSpan.Zero  // Token expiration time tolerance
                }, out SecurityToken validatedToken);

                return Ok("Token is valid.");
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Invalid token during validation.");
                return Unauthorized("Invalid token.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during token validation.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        private string GenerateToken(User user, bool rememberMe)
        {

            List<Claim> claims = new List<Claim>{
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]!));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var tokenExpiration = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(2);

            var tokenJWT = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: tokenExpiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenJWT);
        }

        private void CreateCookie(string jwt, bool rememberMe)
        {
            Response.Cookies.Append("JWT", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(2)
            });
        }

    }

}

