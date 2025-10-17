using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Models.Admin.Faq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Congreso.Api.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/faqitems")]
    public class FaqItemsController : ControllerBase
    {
        private readonly CongresoDbContext _context;

        public FaqItemsController(CongresoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FaqItem>>> GetFaqItems()
        {
            var faqItems = await _context.FaqItems
                .OrderBy(f => f.Position)
                .ToListAsync();
            return Ok(faqItems);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FaqItem>> GetFaqItem(int id)
        {
            var faqItem = await _context.FaqItems.FindAsync(id);

            if (faqItem == null)
                return NotFound();

            return Ok(faqItem);
        }

        [HttpPost]
        public async Task<ActionResult<FaqItem>> CreateFaqItem([FromBody] FaqItem faqItem)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _context.FaqItems.Add(faqItem);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetFaqItem), new { id = faqItem.Id }, faqItem);
            }
            catch (DbUpdateException)
            {
                return Conflict("Error al crear el FAQ item. Posible conflicto de datos.");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] PutFaqItemDto dto)
        {
            var f = await _context.FaqItems.FindAsync(id);
            if (f is null) return NotFound();
            f.Question = dto.Question;
            f.Answer = dto.Answer;
            f.Position = dto.SortOrder;
            f.Published = dto.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { data = f });
        }

        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Patch(int id, [FromBody] UpdateFaqItemDto dto)
        {
            var f = await _context.FaqItems.FindAsync(id);
            if (f is null) return NotFound();
            if (dto.Question != null) f.Question = dto.Question;
            if (dto.Answer != null) f.Answer = dto.Answer;
            if (dto.SortOrder.HasValue) f.Position = dto.SortOrder.Value;
            if (dto.IsActive.HasValue) f.Published = dto.IsActive.Value;
            await _context.SaveChangesAsync();
            return Ok(new { data = f });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var f = await _context.FaqItems.FindAsync(id);
            if (f is null) return NotFound();
            _context.FaqItems.Remove(f);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private async Task<bool> FaqItemExists(int id)
        {
            return await _context.FaqItems.AnyAsync(e => e.Id == id);
        }
    }
}