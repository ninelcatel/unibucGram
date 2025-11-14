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
    }
}