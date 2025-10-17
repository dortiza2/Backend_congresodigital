using Congreso.Api.Data;
using Congreso.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Congreso.Api.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/winners")]
    public class WinnersController : ControllerBase
    {
        private readonly CongresoDbContext _context;

        public WinnersController(CongresoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Winner>>> GetWinners()
        {
            try
            {
                var winners = await _context.Winners
                    .OrderBy(w => w.EditionYear)
                    .ThenBy(w => w.ActivityId)
                    .ThenBy(w => w.Place)
                    .ToListAsync();
                return Ok(winners);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno del servidor", message = ex.Message, innerException = ex.InnerException?.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Winner>> GetWinner(long id)
        {
            var winner = await _context.Winners
                .Include(w => w.User)
                .Include(w => w.Activity)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (winner == null)
                return NotFound();

            return Ok(winner);
        }

        [HttpPost]
        public async Task<ActionResult<Winner>> CreateWinner([FromBody] Winner winner)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar que no exista ya un ganador para la misma combinación
            var existingWinner = await _context.Winners
                .FirstOrDefaultAsync(w => w.EditionYear == winner.EditionYear && 
                                         w.ActivityId == winner.ActivityId && 
                                         w.Place == winner.Place);

            if (existingWinner != null)
            {
                return Conflict($"Ya existe un ganador para el lugar {winner.Place} en la actividad {winner.ActivityId} del año {winner.EditionYear}.");
            }

            try
            {
                _context.Winners.Add(winner);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetWinner), new { id = winner.Id }, winner);
            }
            catch (DbUpdateException)
            {
                return Conflict("Error al crear el ganador. Posible violación de restricción única.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWinner(long id, Winner winner)
        {
            if (id != winner.Id)
                return BadRequest("El ID del ganador no coincide.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar que no exista ya un ganador para la misma combinación (excluyendo el actual)
            var existingWinner = await _context.Winners
                .FirstOrDefaultAsync(w => w.Id != id &&
                                         w.EditionYear == winner.EditionYear && 
                                         w.ActivityId == winner.ActivityId && 
                                         w.Place == winner.Place);

            if (existingWinner != null)
            {
                return Conflict($"Ya existe un ganador para el lugar {winner.Place} en la actividad {winner.ActivityId} del año {winner.EditionYear}.");
            }

            _context.Entry(winner).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WinnerExists(id))
                    return NotFound();
                throw;
            }
            catch (DbUpdateException)
            {
                return Conflict("Error al actualizar el ganador. Posible violación de restricción única.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWinner(long id)
        {
            var winner = await _context.Winners.FindAsync(id);
            if (winner == null)
                return NotFound();

            try
            {
                _context.Winners.Remove(winner);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede eliminar el ganador.");
            }
        }

        private bool WinnerExists(long id)
        {
            return _context.Winners.Any(e => e.Id == id);
        }
    }
}