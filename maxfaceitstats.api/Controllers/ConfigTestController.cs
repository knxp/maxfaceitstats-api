using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace maxfaceitstats.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConfigTestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConfigTestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var testSecret = _configuration["TEST_SECRET"];

            return Ok(new
            {
                TestSecretExists = !string.IsNullOrEmpty(testSecret),
                TestSecretValue = testSecret,
                Environment = _configuration["ASPNETCORE_ENVIRONMENT"]
            });
        }
    }
}
