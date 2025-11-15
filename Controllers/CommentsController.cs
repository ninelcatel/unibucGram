using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using unibucGram.Models;

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
        public async Task<IActionResult> Add(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { message = "Comment cannot be empty." });
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var comment = new Comment
            {
                Content = content,
                PostId = postId,
                UserId = userId,
                CreatedAt = System.DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            comment.User = await _userManager.FindByIdAsync(userId);

            return PartialView("~/Views/Shared/_CommentPartial.cshtml", comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] string content)
        {
            var userId = _userManager.GetUserId(User);
            var comment = await _db.Comments.FindAsync(id);

            if (comment == null)
            {
                return NotFound(new { success = false, message = "Comment not found." });
            }

            if (comment.UserId != userId)
            {
                return Forbid(); // Returns a 403 Forbidden status
            }

            comment.Content = content;
            await _db.SaveChangesAsync();

            return Json(new { success = true, content = comment.Content });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var comment = await _db.Comments.FindAsync(id);

            if (comment == null)
            {
                return NotFound(new { success = false, message = "Comment not found." });
            }

            // Security check: Only the owner can delete the comment
            if (comment.UserId != userId)
            {
                return Forbid(); // Returns a 403 Forbidden status
            }

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}