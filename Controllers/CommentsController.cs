using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Ensure this is present
using System.Threading.Tasks;
using unibucGram.Models;
using System.Linq; // Necesar pentru a selecta erorile

namespace unibucGram.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public CommentsController(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromForm] Comment comment)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Post");
            ModelState.Remove("CreatedAt");

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null) return Unauthorized();

                comment.UserId = userId;
                comment.CreatedAt = System.DateTime.UtcNow;

                _db.Comments.Add(comment);
                await _db.SaveChangesAsync();

                comment.User = await _userManager.FindByIdAsync(userId);

                var post = await _db.Posts.FindAsync(comment.PostId);
                if (post != null && post.UserId != userId)
                {
                    _db.Notifications.Add(new Notification {
                        UserId = post.UserId,
                        ActorUserId = userId,
                        Type = NotificationType.Comment,
                        PostId = post.Id,
                        CommentId = comment.Id
                    });
                    await _db.SaveChangesAsync();
                }

                // If it's an AJAX call, return partial; else redirect to the post page
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("~/Views/Shared/_CommentPartial.cshtml", comment);
                }
                return RedirectToAction("Post", "Posts", new { id = comment.PostId });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            // For non-AJAX, send user back to the post page with a flash message if you prefer
            if (Request.Headers["X-Requested-With"] != "XMLHttpRequest")
                return RedirectToAction("Post", "Posts", new { id = comment.PostId });

            return BadRequest(new { message = string.Join(" ", errors) });
        }

        [HttpGet]
        public async Task<IActionResult> LoadComments(int postId, int page = 1, int pageSize = 10)
        {
            var comments = await _db.Comments
                .Include(c => c.User)
                .Where(c => c.PostId == postId)
                .OrderBy(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("~/Views/Shared/_CommentsListPartial.cshtml", comments);
            }

            return Json(comments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] Comment commentData)
        {
            // Eliminam campurile setate de server din validare
            ModelState.Remove("PostId");
            ModelState.Remove("UserId");
            ModelState.Remove("Post");
            ModelState.Remove("User");
            ModelState.Remove("CreatedAt");

            // Verificam doar campul Content, deoarece doar el este trimis
            if (string.IsNullOrWhiteSpace(commentData.Content) || commentData.Content.Length > 1000)
            {
                 ModelState.AddModelError("Content", "Comment must be between 1 and 1000 characters.");
            }

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var comment = await _db.Comments.FindAsync(id);

                if (comment == null)
                {
                    return NotFound(new { success = false, message = "Comment not found." });
                }

                if (comment.UserId != userId)
                {
                    return Forbid();
                }

                comment.Content = commentData.Content;
                await _db.SaveChangesAsync();

                return Json(new { success = true, content = comment.Content });
            }
            
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join(" ", errors) });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // CHANGED: Use this attribute for the AJAX endpoint
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                // This returns a 401 Unauthorized, which is a "not ok" response.
                return Unauthorized();
            }

            var comment = await _db.Comments.FindAsync(id);
            
            if (comment == null)
            {
                // This returns a 404 Not Found, also "not ok".
                return NotFound(new { success = false, message = "Comment not found." });
            }

            // Security Check: Allow deletion only if the user is the comment author OR the post author.
            var post = await _db.Posts.FindAsync(comment.PostId);
            if (comment.UserId != userId && (post == null || post.UserId != userId))
            {
                // This returns a 403 Forbidden, also "not ok".
                return Forbid();
            }

            _db.Comments.Remove(comment);
            
            // Clean up related notifications
            var notifications = await _db.Notifications.Where(n => n.CommentId == id).ToListAsync();
            if (notifications.Any())
            {
                _db.Notifications.RemoveRange(notifications);
            }

            await _db.SaveChangesAsync();

            // On success, return a 200 OK with the success payload.
            return Json(new { success = true });
        }
    }
}