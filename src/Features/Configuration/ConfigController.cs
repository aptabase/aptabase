using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Features.Configuration;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ConfigController : Controller
{
    private readonly ILogger _logger;
    private readonly EnvSettings _env;

    public ConfigController(
        ILogger<ConfigController> logger,
        EnvSettings env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("/api/_config")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        return Ok(new {
            disableSignup = _env.DisableSignup
        });
    }
}
