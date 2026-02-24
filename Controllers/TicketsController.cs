using BackendApiExam.Data;
using BackendApiExam.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BackendApiExam.Controllers
{
    [ApiController]
    [Route("tickets")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetUserRole() =>
            User.FindFirstValue(ClaimTypes.Role)!;

        private object FormatTicket(Ticket t) => new
        {
            t.Id,
            t.Title,
            t.Description,
            status = t.Status.ToString(),
            priority = t.Priority.ToString(),
            created_by = t.CreatedByUser == null ? null : new
            {
                id = t.CreatedByUser.Id,
                name = t.CreatedByUser.Name,
                email = t.CreatedByUser.Email,
                role = t.CreatedByUser.Role == null ? null : new
                {
                    id = t.CreatedByUser.Role.Id,
                    name = t.CreatedByUser.Role.Name
                },
                created_at = t.CreatedByUser.CreatedAt
            },
            assigned_to = t.AssignedToUser == null ? null : new
            {
                id = t.AssignedToUser.Id,
                name = t.AssignedToUser.Name,
                email = t.AssignedToUser.Email,
                role = t.AssignedToUser.Role == null ? null : new
                {
                    id = t.AssignedToUser.Role.Id,
                    name = t.AssignedToUser.Role.Name
                },
                created_at = t.AssignedToUser.CreatedAt
            },
            created_at = t.CreatedAt
        };

        [HttpPost]
        [Authorize(Roles = "USER,MANAGER")]
        public async Task<IActionResult> Create([FromBody] CreateTicketDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = new Ticket
            {
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                Status = TicketStatus.OPEN,
                CreatedBy = GetUserId(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var created = await _context.Tickets
                .Include(t => t.CreatedByUser).ThenInclude(u => u.Role)
                .Include(t => t.AssignedToUser).ThenInclude(u => u!.Role)
                .FirstAsync(t => t.Id == ticket.Id);

            return Created("", FormatTicket(created));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var role = GetUserRole();

            IQueryable<Ticket> query = _context.Tickets
                .Include(t => t.CreatedByUser).ThenInclude(u => u.Role)
                .Include(t => t.AssignedToUser).ThenInclude(u => u!.Role);

            if (role == "MANAGER")
            {
            }
            else if (role == "SUPPORT")
            {
                query = query.Where(t => t.AssignedTo == userId);
            }
            else
            {
                query = query.Where(t => t.CreatedBy == userId);
            }

            var tickets = await query.ToListAsync();
            return Ok(tickets.Select(FormatTicket));
        }

        [HttpPatch("{id}/assign")]
        [Authorize(Roles = "MANAGER,SUPPORT")]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _context.Tickets
                .Include(t => t.CreatedByUser).ThenInclude(u => u.Role)
                .Include(t => t.AssignedToUser).ThenInclude(u => u!.Role)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            var assignee = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == dto.UserId);

            if (assignee == null)
                return NotFound(new { message = "User not found" });

            if (assignee.Role.Name == "USER")
                return BadRequest(new { message = "Tickets cannot be assigned to users with role USER" });

            ticket.AssignedTo = dto.UserId;
            await _context.SaveChangesAsync();

            await _context.Entry(ticket).Reference(t => t.AssignedToUser).LoadAsync();
            if (ticket.AssignedToUser != null)
                await _context.Entry(ticket.AssignedToUser).Reference(u => u.Role).LoadAsync();

            return Ok(FormatTicket(ticket));
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "MANAGER,SUPPORT")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _context.Tickets
                .Include(t => t.CreatedByUser).ThenInclude(u => u.Role)
                .Include(t => t.AssignedToUser).ThenInclude(u => u!.Role)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            if (!IsValidTransition(ticket.Status, dto.Status))
                return BadRequest(new { message = $"Invalid status transition" });

            var oldStatus = ticket.Status;
            ticket.Status = dto.Status;

            var log = new TicketStatusLog
            {
                TicketId = ticket.Id,
                OldStatus = oldStatus,
                NewStatus = dto.Status,
                ChangedBy = GetUserId(),
                ChangedAt = DateTime.Now
            };
            _context.TicketStatusLogs.Add(log);

            await _context.SaveChangesAsync();
            return Ok(FormatTicket(ticket));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "MANAGER")]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static bool IsValidTransition(TicketStatus current, TicketStatus next)
        {
            return (current, next) switch
            {
                (TicketStatus.OPEN, TicketStatus.IN_PROGRESS) => true,
                (TicketStatus.IN_PROGRESS, TicketStatus.RESOLVED) => true,
                (TicketStatus.RESOLVED, TicketStatus.CLOSED) => true,
                _ => false
            };
        }
    }
}
