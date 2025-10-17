using System.ComponentModel.DataAnnotations;
using Congreso.Api.DTOs;
using Congreso.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Congreso.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicPodiumsController : ControllerBase
    {
        private readonly IPodiumService _podiumService;
        private readonly Serilog.ILogger _logger;

        public PublicPodiumsController(IPodiumService podiumService)
        {
            _podiumService = podiumService;
            _logger = Log.ForContext<PublicPodiumsController>();
        }

        /// <summary>
        /// Obtiene el podio de ganadores para un año específico
        /// </summary>
        /// <param name="year">Año del podio (2020-2030)</param>
        /// <returns>Lista de ganadores del podio para el año especificado</returns>
        [HttpGet]
        [HttpGet("/api/podium")]
        [ProducesResponseType(typeof(List<PodiumResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<PodiumResponse>>> GetPodiumByYear([FromQuery] int year)
        {
            try
            {
                // Validar rango de año
                if (year < 2020 || year > 2030)
                {
                    _logger.Warning("Año inválido solicitado: {Year}", year);
                    return BadRequest(new { error = "El año debe estar entre 2020 y 2030" });
                }

                _logger.Information("Obteniendo podio para año {Year}", year);
                
                // Obtener información de auditoría
                var userAgent = Request.Headers["User-Agent"].ToString();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                var podium = await _podiumService.GetPodiumByYearAsync(year, userAgent, ipAddress);
                
                _logger.Information("Podio obtenido exitosamente para año {Year}: {Count} registros", 
                    year, podium.Count);
                
                return Ok(podium);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error al obtener podio para año {Year}", year);
                return StatusCode(500, new { error = "Error interno al obtener el podio" });
            }
        }

        /// <summary>
        /// Obtiene un registro específico de podio por ID
        /// </summary>
        /// <param name="id">ID del registro de podio</param>
        /// <returns>Registro de podio o 404 si no existe</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PodiumResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PodiumResponse>> GetPodiumById(int id)
        {
            try
            {
                _logger.Information("Solicitando podio por ID: {Id}", id);
                
                var podium = await _podiumService.GetPodiumByIdAsync(id);
                
                if (podium == null)
                {
                    _logger.Warning("Podio no encontrado por ID: {Id}", id);
                    return NotFound(new ProblemDetails
                    {
                        Title = "No encontrado",
                        Detail = $"No se encontró un registro de podio con ID {id}",
                        Status = StatusCodes.Status404NotFound,
                        Instance = HttpContext.Request.Path
                    });
                }

                _logger.Information("Podio obtenido exitosamente por ID: {Id}", id);
                return Ok(podium);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error obteniendo podio por ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Error interno",
                    Detail = "Ocurrió un error al procesar la solicitud",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = HttpContext.Request.Path
                });
            }
        }

        /// <summary>
        /// Health check básico para el servicio de podios
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public ActionResult<object> HealthCheck()
        {
            _logger.Debug("Health check solicitado para podios");
            
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "PublicPodiumsController"
            });
        }
    }
}