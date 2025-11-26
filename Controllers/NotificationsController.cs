using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using unibucGram.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
// !!!!!
// FOLLOW REQUESTS THAT ARE MARKED AS READ WILL BE DELETED 
// !!!!!

namespace unibucGram.Controllers
{
    [Authorize(Roles = "User,Editor,Admin")] // Guests cannot access notifications
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        public NotificationsController(ApplicationDbContext db, UserManager<User> um)
        {
            _db = db; _userManager = um;
        }

        [HttpGet]
        public async Task<IActionResult> Unread()
        {
            var uid = _userManager.GetUserId(User);
            if (uid == null) return Unauthorized();

            var list = await _db.Notifications
                .Where(n => n.UserId == uid && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Include(n => n.ActorUser)
                .Include(n => n.Post)
                .Take(20)
                .Select(n => new
                {
                    id = n.Id,
                    type = n.Type.ToString(),
                    actor = n.ActorUser!.UserName,
                    actorPfp = n.ActorUser!.PfpURL,
                    postId = n.PostId,
                    commentId = n.CommentId,
                    time = n.CreatedAt
                })
                .ToListAsync();

            return Json(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var uid = _userManager.GetUserId(User);
            if (uid == null) return Unauthorized();
            var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (n == null) return NotFound();
            n.IsRead = true;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAll()
        {
            var uid = _userManager.GetUserId(User);
            if (uid == null) return Unauthorized();

            var items = await _db.Notifications
                .Where(x => x.UserId == uid && !x.IsRead)
                .ToListAsync();

            items.ForEach(i => i.IsRead = true);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}