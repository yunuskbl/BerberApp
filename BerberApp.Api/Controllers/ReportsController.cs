using BerberApp.Api.Authorization;
using BerberApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequirePlan(PlanType.Full)]  // Sadece Full plan
public class ReportsController : ControllerBase
{
    [HttpGet("advanced")]
    public IActionResult GetAdvancedReport()
    {
        return Ok(new { message = "Advanced reports - Full plan only" });
    }

    [HttpGet("basic")]
    public IActionResult GetBasicReport()
    {
        return Ok(new { message = "Basic reports" });
    }
}