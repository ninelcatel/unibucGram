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

        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(int PostId, string Content)
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                // Optionally, handle empty comment submission
                return RedirectToAction("Post", "Posts", new { id = PostId });
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var comment = new Comment
            {
                Content = Content,
                PostId = PostId,
                UserId = userId,
                CreatedAt = System.DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            return RedirectToAction("Post", "Posts", new { id = PostId });
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

            return PartialView("_SingleComment", comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int commentId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Comment cannot be empty.";
                
            }

            var userId = _userManager.GetUserId(User);
            var comment = await _db.Comments.FindAsync(commentId);

            if (comment == null)
            {
                return NotFound();
            }

            if (comment.UserId != userId)
            {
                return Forbid();
            }

            comment.Content = content;
            await _db.SaveChangesAsync();

            // Redirect back to the post page
            return RedirectToAction("Post", "Posts", new { id = comment.PostId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int commentId)
        {
            var userId = _userManager.GetUserId(User);
            var comment = await _db.Comments.FindAsync(commentId);

            if (comment == null)
            {
                return NotFound();
            }

            // Security check: Only the owner can delete the comment
            if (comment.UserId != userId)
            {
                return Forbid();
            }

            var postId = comment.PostId; // Store PostId for redirection

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();

            // Redirect back to the post page
            return RedirectToAction("Post", "Posts", new { id = postId });
        }
    }
}