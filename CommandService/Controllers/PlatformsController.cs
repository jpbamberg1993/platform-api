using Microsoft.AspNetCore.Mvc;

namespace CommandService.Controllers;

[Route("api/command/[controller]")]
[ApiController]
public class PlatformsController : ControllerBase
{
    [HttpPost]
    public ActionResult TestInboundConnection()
    {
        Console.WriteLine("--> Inbound POST # Command Service");
        return Ok("Inbound test of PlatformsController");
    }
}