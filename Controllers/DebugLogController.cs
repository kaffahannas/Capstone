#if DEBUG
using Microsoft.AspNetCore.Mvc;

namespace LightenUp.Web.Controllers;

[Route("debug/log")]
[ApiController]
[IgnoreAntiforgeryToken]
public class DebugLogController : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] System.Text.Json.JsonElement payload)
    {
        var line = payload.GetRawText() + Environment.NewLine;
        var cwd = Directory.GetCurrentDirectory();
        var paths = new[]
        {
            Path.Combine(cwd, ".cursor", "debug-9d27c6.log"),
            Path.Combine(cwd, "debug-9d27c6.log")
        };

        foreach (var logPath in paths)
        {
            try
            {
                var dir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(logPath, line);
            }
            catch
            {
                // try next path
            }
        }

        return Ok();
    }
}
#endif
