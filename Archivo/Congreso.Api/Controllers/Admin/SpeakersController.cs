using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Models.Admin.Speakers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Congreso.Api.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/speakers")]
    public class SpeakersController : ControllerBase
    {
        private readonly CongresoDbContext _context;

        public SpeakersController(CongresoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Speaker>>> GetSpeakers()
        {
            var speakers = await _context.Speakers
                .OrderBy(s => s.FullName)
                .ToListAsync();
            return Ok(speakers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Speaker>> GetSpeaker(Guid id)
        {
            var speaker = await _context.Speakers.FindAsync(id);

            if (speaker == null)
                return NotFound();

            return Ok(new { data = speaker });
        }

        [HttpPost]
        public async Task<ActionResult<Speaker>> CreateSpeaker([FromBody] Speaker speaker)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _context.Speakers.Add(speaker);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSpeaker), new { id = speaker.Id }, speaker);
            }
            catch (DbUpdateException)
            {
                return Conflict("Error al crear el speaker. Posible conflicto de datos.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] PutSpeakerDto dto)
        {
            var speaker = await _context.Speakers.FindAsync(id);
            if (speaker == null) return NotFound();

            speaker.FullName = dto.FullName;
            speaker.OrgName = dto.Title;
            speaker.Bio = dto.Bio;
            speaker.PhotoUrl = dto.AvatarUrl;
            speaker.Social = dto.Links ?? "{}";
            
            await _context.SaveChangesAsync();
            return Ok(new { data = speaker });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(Guid id, [FromBody] UpdateSpeakerDto dto)
        {
            var speaker = await _context.Speakers.FindAsync(id);
            if (speaker == null) return NotFound();
            
            if (dto.FullName != null) speaker.FullName = dto.FullName;
            if (dto.Title != null) speaker.OrgName = dto.Title;
            if (dto.Bio != null) speaker.Bio = dto.Bio;
            if (dto.AvatarUrl != null) speaker.PhotoUrl = dto.AvatarUrl;
            if (dto.Links != null) speaker.Social = dto.Links;
            
            await _context.SaveChangesAsync();
            return Ok(new { data = speaker });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var speaker = await _context.Speakers.FindAsync(id);
            if (speaker == null) return NotFound();
            
            try
            {
                _context.Speakers.Remove(speaker);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = "Speaker is in use.", detail = ex.Message });
            }
        }

        private async Task<bool> SpeakerExists(Guid id)
        {
            return await _context.Speakers.AnyAsync(e => e.Id == id);
        }
    }
}