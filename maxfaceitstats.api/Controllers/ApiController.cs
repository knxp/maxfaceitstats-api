using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using maxfaceitstats.api.Models;
using System.Net.Http;
using System.Text.Json;

namespace maxfaceitstats.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ApiController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("token")]
        public IActionResult GetToken([FromBody] AuthRequest request)
        {
            // Validate both API keys exist
            var faceitApiKey = _configuration["FACEIT_API"];
            var steamApiKey = _configuration["STEAM_API"];

            if (string.IsNullOrEmpty(faceitApiKey) || string.IsNullOrEmpty(steamApiKey))
            {
                return StatusCode(500, "API configuration is missing");
            }

            var jwtKey = _configuration["JWT:SigningKey"] ?? throw new Exception("JWT:SigningKey is not configured");
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("FaceitApiKey", faceitApiKey),
                    new Claim("SteamApiKey", steamApiKey)
                }),
                Expires = DateTime.UtcNow.AddHours(1), // Short expiration for security
                Issuer = "https://maxstats-api-c4b8dudcgsdxeraf.centralus-01.azurewebsites.net",
                Audience = "https://maxstats.dev",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new
            {
                Token = tokenHandler.WriteToken(token)
            });
        }

        [HttpGet("steam/resolve/{vanityUrl}")]
        public async Task<IActionResult> ResolveSteamVanityUrl(string vanityUrl)
        {
            try
            {
                var steamApiKey = _configuration["STEAM_API"];
                if (string.IsNullOrEmpty(steamApiKey))
                {
                    return StatusCode(500, "Steam API key not configured");
                }

                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(
                    $"https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1/?key={steamApiKey}&vanityurl={vanityUrl}");

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonDocument>();
                var steamId = jsonResponse?.RootElement
                    .GetProperty("response")
                    .GetProperty("steamid")
                    .GetString();

                if (string.IsNullOrEmpty(steamId))
                {
                    return NotFound("Steam ID not found");
                }

                return Ok(steamId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error resolving Steam vanity URL");
            }
        }
    }
}
