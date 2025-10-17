using System.Security.Claims;
using Congreso.Api.DTOs;
using Congreso.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Congreso.Api.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "admin,organizer")]
    public class AdminPodiumsController : ControllerBase
    {
        private readonly IPodiumService _podiumService;
        private readonly Serilog.ILogger _logger;

        public AdminPodiumsController(IPodiumService podiumService)
        {
            _podiumService = podiumService;
            _logger = Log.ForContext<AdminPodiumsController>();
        }

        /// <summary>
        /// Crea un nuevo registro de podio
        /// </summary>
        /// <param name="request">Datos del podio a crear</param>
        /// <returns>Podio creado</returns>
        [HttpPost]
        [ProducesResponseType(typeof(PodiumResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PodiumResponse>> CreatePodium([FromBody] CreatePodiumRequest request)
        {
            try
            {
                var userGuid = GetCurrentUserId();
                var actorId = GetActorAuditId(userGuid);

                _logger.Information("Usuario {UserGuid} creando podio para año {Year}, lugar {Place}", 
                    userGuid, request.Year, request.Place);

                var podiumId = await _podiumService.CreatePodiumAsync(request, actorId);

                _logger.Information("Podio creado exitosamente por usuario {UserGuid}: ID {PodiumId}", 
                    userGuid, podiumId);

                // Get the created podium to return in the response
                var podium = await _podiumService.GetPodiumByIdAsync(podiumId);

                return CreatedAtAction(nameof(PublicPodiumsController.GetPodiumById), 
                    new { controller = "PublicPodiums", id = podiumId }, podium);
            }
            catch (ArgumentException ex)
            {
                _logger.Warning(ex, "Datos inválidos al crear podio: {Message}", ex.Message);
                return BadRequest(new ProblemDetails
                {
                    Title = "Datos inválidos",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("duplicado"))
            {
                _logger.Warning(ex, "Intento de crear podio duplicado: {Message}", ex.Message);
                return Conflict(new ProblemDetails
                {
                    Title = "Conflicto",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict,
                    Instance = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error inesperado creando podio por usuario {UserId}", GetCurrentUserId());
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
        /// Actualiza un registro de podio existente
        /// </summary>
        /// <param name="id">ID del podio a actualizar</param>
        /// <param name="request">Datos actualizados del podio</param>
        /// <returns>Podio actualizado</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(PodiumResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PodiumResponse>> UpdatePodium(
            int id, [FromBody] UpdatePodiumRequest request)
        {
            try
            {
                var userGuid = GetCurrentUserId();
                var actorId = GetActorAuditId(userGuid);
                
                _logger.Information("Usuario {UserGuid} actualizando podio ID {PodiumId} para año {Year}, lugar {Place}", 
                    userGuid, id, request.Year, request.Place);

                await _podiumService.UpdatePodiumAsync(id, request, actorId);

                _logger.Information("Podio actualizado exitosamente por usuario {UserGuid}: ID {PodiumId}", 
                    userGuid, id);

                // Get the updated podium to return in the response
                var podium = await _podiumService.GetPodiumByIdAsync(id);
                return Ok(podium);
            }
            catch (ArgumentException ex)
            {
                _logger.Warning(ex, "Datos inválidos al actualizar podio ID {PodiumId}: {Message}", id, ex.Message);
                return BadRequest(new ProblemDetails
                {
                    Title = "Datos inválidos",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.Warning(ex, "Podio no encontrado ID {PodiumId}: {Message}", id, ex.Message);
                return NotFound(new ProblemDetails
                {
                    Title = "No encontrado",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error inesperado actualizando podio ID {PodiumId} por usuario {UserId}", 
                    id, GetCurrentUserId());
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
        /// Elimina un registro de podio
        /// </summary>
        /// <param name="id">ID del podio a eliminar</param>
        /// <returns>Resultado de la eliminación</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeletePodium(int id)
        {
            try
            {
                var userGuid = GetCurrentUserId();
                var actorId = GetActorAuditId(userGuid);
                
                _logger.Information("Usuario {UserGuid} eliminando podio ID {PodiumId}", userGuid, id);

                await _podiumService.DeletePodiumAsync(id, actorId);

                _logger.Information("Podio eliminado exitosamente por usuario {UserGuid}: ID {PodiumId}", userGuid, id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.Warning(ex, "Podio no encontrado para eliminar: ID {PodiumId}", id);
                return NotFound(new ProblemDetails
                {
                    Title = "No encontrado",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error inesperado eliminando podio ID {PodiumId} por usuario {UserId}", id, GetCurrentUserId());
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
        /// Obtiene todos los registros de podio con paginación
        /// </summary>
        /// <param name="page">Número de página</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <returns>Lista de podios</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<PodiumResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PodiumResponse>>> GetAllPodiums(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userGuid = GetCurrentUserId();
                var actorId = GetActorAuditId(userGuid);
                
                _logger.Information("Usuario {UserGuid} consultando todos los podios: página {Page}, tamaño {PageSize}", 
                    userGuid, page, pageSize);

                var podiums = await _podiumService.GetAllPodiumsAsync(page, pageSize);

                _logger.Information("Podios consultados exitosamente por usuario {UserGuid}: {Count} resultados", 
                    userGuid, podiums.Count);

                return Ok(podiums);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error inesperado consultando podios por usuario {UserId}", GetCurrentUserId());
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Error interno",
                    Detail = "Ocurrió un error al procesar la solicitud",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = HttpContext.Request.Path
                });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.Warning("No se pudo obtener el ID de usuario del token");
                throw new InvalidOperationException("No se pudo identificar al usuario");
            }
            return userId;
        }

        private int GetActorAuditId(Guid userId)
        {
            var bytes = userId.ToByteArray();
            var value = BitConverter.ToInt32(bytes, 0);
            if (value == int.MinValue) return 0;
            return Math.Abs(value);
        }

        private async Task<List<PodiumResponse>> GetAllPodiumsFromDatabaseAsync()
        {
            // Este método debería estar en el servicio, pero lo ponemos aquí por simplicidad
            // En producción, mover a IPodiumService
            return new List<PodiumResponse>(); // Implementación placeholder
        }
    }
}