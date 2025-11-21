using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using unibucGram.Models;
using Microsoft.AspNetCore.Identity;

namespace unibucGram.Controllers
{
    public class ConversationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ConversationsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // Fetch conversations where the current user is either User1 or User2
            var conversations = await _context.Conversations
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages)
                .Select(c => new {
                    c.Id,
                    // Determine who the "other" person is
                    OtherUser = c.User1Id == userId ? c.User2 : c.User1,
                    LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault()
                })
                .ToListAsync();

            var result = conversations.Select(c => new {
                c.Id,
                Name = c.OtherUser.UserName,
                Pfp = c.OtherUser.PfpURL,
                LastMessage = c.LastMessage
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            var messages = await _context.Messages
                .Where(m => m.ConversationId == id)
                .Include(m => m.Sender)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new {
                    m.Id,
                    m.Content,
                    SenderName = m.Sender.UserName,
                    SenderPfp = m.Sender.PfpURL,
                    IsMe = m.SenderId == currentUserId,
                    SentAt = m.CreatedAt.ToString("HH:mm")
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int conversationId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(content)) return BadRequest();

            var msg = new Message
            {
                ConversationId = conversationId,
                SenderId = user.Id,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}