using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Congreso.Api.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Congreso.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendancesController : ControllerBase
{
    private readonly ICheckInTokenService _checkInTokenService;
    private readonly ISecureQrService _secureQrService;
    private readonly ILogger<AttendancesController> _logger;
    private readonly IMetricsCollector _metrics;

    public AttendancesController(ICheckInTokenService checkInTokenService, ISecureQrService secureQrService, ILogger<AttendancesController> logger, IMetricsCollector metrics)
    {
        _checkInTokenService = checkInTokenService;
        _secureQrService = secureQrService;
        _logger = logger;
        _metrics = metrics;
    }

    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
    {
        try
        {
            // Obtener el ID del usuario asistente que está realizando el check-in
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var assistantUserId))
            {
                return Unauthorized("Usuario no autenticado");
            }

            // Primero intentar procesar como QR seguro (HMAC firmado)
            var secureResult = await _secureQrService.ProcessSecureQrAsync(request.Token, assistantUserId);
            if (secureResult.Success)
            {
                _metrics.TrackQrScan(true, true);
                return Ok(new
                {
                    message = secureResult.Message,
                    wasAlreadyCheckedIn = secureResult.WasAlreadyProcessed,
                    checkInTime = secureResult.ProcessedAt
                });
            }

            // Fallback: procesar token de check-in legado (persistente en BD)
            var legacyResult = await _checkInTokenService.ProcessCheckInAsync(request.Token, assistantUserId);

            if (!legacyResult.Success)
            {
                _metrics.TrackQrScan(false, false);
                return BadRequest(new { message = legacyResult.Message });
            }

            _metrics.TrackQrScan(true, false);
            return Ok(new
            {
                message = legacyResult.Message,
                wasAlreadyCheckedIn = legacyResult.WasAlreadyCheckedIn,
                checkInTime = legacyResult.CheckInTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando check-in con token {TokenPrefix}", request.Token[..8] + "...");
            _metrics.TrackQrScan(false, true);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}

public class CheckInRequest
{
    public string Token { get; set; } = string.Empty;
}