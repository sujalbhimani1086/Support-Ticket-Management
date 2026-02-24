using BackendApiExam.Data;
using BackendApiExam.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BackendApiExam.Controllers
{
    [ApiController]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetUserRole() =>
            User.FindFirstValue(ClaimTypes.Role)!;

        private object FormatComment(TicketComment c) => new
        {
            c.Id,
            comment = c.Comment,
            user = c.User == null ? null : new
            {
                id = c.User.Id,
                name = c.User.Name,
                email = c.User.Email,
                role = c.User.Role == null ? null : new
                {
                    id = c.User.Role.Id,
                    name = c.User.Role.Name
                },
                created_at = c.User.CreatedAt
            },
            created_at = c.CreatedAt
        };

        private bool CanAccessTicket(Ticket ticket)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            if (role == "MANAGER") return true;
            if (role == "SUPPORT" && ticket.AssignedTo == userId) return true;
            if (role == "USER" && ticket.CreatedBy == userId) return true;
            return false;
        }

        [HttpPost("tickets/{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] CommentDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            if (!CanAccessTicket(ticket))
                return StatusCode(403, new { message = "Forbidden" });

            var comment = new TicketComment
            {
                TicketId = id,
                UserId = GetUserId(),
                Comment = dto.Comment,
                CreatedAt = DateTime.Now
            };

            _context.TicketComments.Add(comment);
            await _context.SaveChangesAsync();

            var created = await _context.TicketComments
                .Include(c => c.User).ThenInclude(u => u.Role)
                .FirstAsync(c => c.Id == comment.Id);

            return Created("", FormatComment(created));
        }

        [HttpGet("tickets/{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            if (!CanAccessTicket(ticket))
                return StatusCode(403, new { message = "Forbidden" });

            var comments = await _context.TicketComments
                .Where(c => c.TicketId == id)
                .Include(c => c.User).ThenInclude(u => u.Role)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return Ok(comments.Select(FormatComment));
        }

        [HttpPatch("comments/{id}")]
        public async Task<IActionResult> EditComment(int id, [FromBody] CommentDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var comment = await _context.TicketComments
                .Include(c => c.User).ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
                return NotFound(new { message = "Comment not found" });

            var userId = GetUserId();
            var role = GetUserRole();

            if (role != "MANAGER" && comment.UserId != userId)
                return StatusCode(403, new { message = "Forbidden" });

            comment.Comment = dto.Comment;
            await _context.SaveChangesAsync();

            return Ok(FormatComment(comment));
        }

        [HttpDelete("comments/{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.TicketComments.FindAsync(id);
            if (comment == null)
                return NotFound(new { message = "Comment not found" });

            var userId = GetUserId();
            var role = GetUserRole();

            if (role != "MANAGER" && comment.UserId != userId)
                return StatusCode(403, new { message = "Forbidden" });

            _context.TicketComments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
