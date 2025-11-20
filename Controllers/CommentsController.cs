using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
            // Eliminam campurile setate manual din procesul de validare
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Post");
            ModelState.Remove("CreatedAt");

            // Verificam daca modelul respecta regulile ramase (ex: [Required] pe Content)
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Unauthorized();
                }

                // Setam proprietatile care nu vin din formular
                comment.UserId = userId;
                comment.CreatedAt = System.DateTime.UtcNow;

                _db.Comments.Add(comment);
                await _db.SaveChangesAsync();

                // Incarcam datele userului pentru a le afisa in partial view
                comment.User = await _userManager.FindByIdAsync(userId);

                return PartialView("~/Views/Shared/_CommentPartial.cshtml", comment);
            }

            // Daca modelul nu e valid, returnam erorile pentru a fi procesate de AJAX
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { message = string.Join(" ", errors) });
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