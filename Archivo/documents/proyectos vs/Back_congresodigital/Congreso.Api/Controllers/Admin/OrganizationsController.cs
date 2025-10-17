using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Models.Admin.Organizations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Congreso.Api.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("api/admin/organizations")]
    public class OrganizationsController : ControllerBase
    {
        private readonly CongresoDbContext _context;

        public OrganizationsController(CongresoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Organization>>> GetOrganizations()
        {
            var organizations = await _context.Organizations
                .OrderBy(o => o.Name)
                .ToListAsync();
            return Ok(organizations);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Organization>> GetOrganization(Guid id)
        {
            var organization = await _context.Organizations.FindAsync(id);

            if (organization == null)
                return NotFound();

            return Ok(organization);
        }

        [HttpPost]
        public async Task<ActionResult<Organization>> CreateOrganization([FromBody] Organization organization)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organization);
            }
            catch (DbUpdateException)
            {
                return Conflict("Error al crear la organización. Posible conflicto de datos.");
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] PutOrganizationDto dto)
        {
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id.ToString() == id.ToString());
            if (org == null) return NotFound();
            
            // Opcional: unicidad por Name si aplica
            if (await _context.Organizations.AnyAsync(o => o.Id != org.Id && o.Name == dto.Name))
                return Conflict(new { message = "Organization name already exists." });

            org.Name = dto.Name;
            org.Type = dto.Type;
            org.Domain = dto.Domain;
            
            await _context.SaveChangesAsync();
            return Ok(new { data = org });
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Patch(Guid id, [FromBody] UpdateOrganizationDto dto)
        {
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id.ToString() == id.ToString());
            if (org == null) return NotFound();
            
            if (dto.Name != null) org.Name = dto.Name;
            if (dto.Type != null) org.Type = dto.Type;
            if (dto.Domain != null) org.Domain = dto.Domain;
            
            await _context.SaveChangesAsync();
            return Ok(new { data = org });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id.ToString() == id.ToString());
            if (org == null) return NotFound();
            
            try
            {
                _context.Organizations.Remove(org);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = "Organization is in use.", detail = ex.Message });
            }
        }

        private async Task<bool> OrganizationExists(Guid id)
        {
            return await _context.Organizations.AnyAsync(e => e.Id.ToString() == id.ToString());
        }
    }
}