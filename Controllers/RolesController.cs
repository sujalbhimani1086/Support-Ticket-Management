using BackendApiExam.Data;
using BackendApiExam.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApiExam.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "MANAGER")]
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RolesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _db.Roles.ToListAsync();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role == null)
                return NotFound(new { message = "Role not found" });

            return Ok(role);
        }

        [HttpPost]
        public async Task<IActionResult> Create(RoleDTO role)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool exists = await _db.Roles.AnyAsync(r => r.Name == role.Name);
            if (exists)
                return BadRequest(new { message = "Role already exists" });

            var roleToadd = new Role { Name = role.Name };

            _db.Roles.Add(roleToadd);
            await _db.SaveChangesAsync();

            return Created("", role);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, RoleDTO role)
        {
            var existing = await _db.Roles.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Role not found" });

            existing.Name = role.Name;
            await _db.SaveChangesAsync();

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role == null)
                return NotFound(new { message = "Role not found" });

            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Role deleted successfully" });
        }
    }
}