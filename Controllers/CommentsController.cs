using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using unibucGram.Models;
using System.Linq;
using unibucGram.Services;

namespace unibucGram.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly ContentModerationService _moderationService;

        public CommentsController(ApplicationDbContext db, UserManager<User> userManager, ContentModerationService moderationService)
        {
            _db = db;
            _userManager = userManager;
            _moderationService = moderationService;
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")] // Guests cannot comment
        // SA NU ADAUGI ALA CU ANTIFORGERYTOKEN CA SE STRICA
        public async Task<IActionResult> Add([FromForm] Comment comment)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Post");
            ModelState.Remove("CreatedAt");

            // Check content with AI moderation
            if (!string.IsNullOrWhiteSpace(comment.Content))
            {
                var (isAppropriate, reason) = await _moderationService.CheckContentAsync(comment.Content);
                if (!isAppropriate)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return BadRequest(new { message = "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi." });
                    }
                    return RedirectToAction("Post", "Posts", new { id = comment.PostId });
                }
            }

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

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("~/Views/Shared/_CommentPartial.cshtml", comment);
                }
                return RedirectToAction("Post", "Posts", new { id = comment.PostId });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
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
        [Authorize(Roles = "User,Editor,Admin")] // guests cannot edit comments
        public async Task<IActionResult> Edit(int id, [FromForm] Comment commentData)
        {
            ModelState.Remove("PostId");
            ModelState.Remove("UserId");
            ModelState.Remove("Post");
            ModelState.Remove("User");
            ModelState.Remove("CreatedAt");

            if (string.IsNullOrWhiteSpace(commentData.Content) || commentData.Content.Length > 1000)
            {
                 ModelState.AddModelError("Content", "Comment must be between 1 and 1000 characters.");
            }

            // Check content with AI moderation
            if (!string.IsNullOrWhiteSpace(commentData.Content))
            {
                var (isAppropriate, reason) = await _moderationService.CheckContentAsync(commentData.Content);
                if (!isAppropriate)
                {
                    var friendlyMessage = ContentModerationService.GetFriendlyErrorMessage(reason);
                    return BadRequest(new { success = false, message = friendlyMessage });
                }
            }

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null) return Unauthorized();

                var comment = await _db.Comments.FindAsync(id);
                if (comment == null)
                {
                    return NotFound(new { success = false, message = "Comment not found." });
                }

                // check permissions
                var currentUser = await _userManager.FindByIdAsync(userId);
                var isAdminOrEditor = await _userManager.IsInRoleAsync(currentUser, "Admin") || 
                                       await _userManager.IsInRoleAsync(currentUser, "Editor");

                if (comment.UserId != userId && !isAdminOrEditor)
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
        [IgnoreAntiforgeryToken]
        [Authorize(Roles = "User,Editor,Admin")] // Guests cannot delete comments
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var comment = await _db.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound(new { success = false, message = "Comment not found." });
            }

            var post = await _db.Posts.FindAsync(comment.PostId);
            var currentUser = await _userManager.FindByIdAsync(userId);
            var isAdminOrEditor = await _userManager.IsInRoleAsync(currentUser, "Admin") || 
                                   await _userManager.IsInRoleAsync(currentUser, "Editor");

            if (comment.UserId != userId && (post == null || post.UserId != userId) && !isAdminOrEditor)
            {
                return Forbid();
            }

            _db.Comments.Remove(comment);
            
            var notifications = await _db.Notifications.Where(n => n.CommentId == id).ToListAsync();
            if (notifications.Any())
            {
                _db.Notifications.RemoveRange(notifications);
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}