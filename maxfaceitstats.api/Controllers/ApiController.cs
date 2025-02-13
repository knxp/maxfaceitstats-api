using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using maxfaceitstats.api.Models;

namespace maxfaceitstats.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
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

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(faceitApiKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("FaceitApiKey", faceitApiKey),
                    new Claim("SteamApiKey", steamApiKey)
                }),
                Expires = DateTime.UtcNow.AddHours(1), // Short expiration for security
                Issuer = "https://maxfaceitstats-api.azurewebsites.net",
                Audience = "https://www.maxfaceitstats.com",
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
    }
}
