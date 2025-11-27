using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using unibucGram.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace unibucGram.Controllers
{
    [Authorize(Roles = "Editor,Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _environment; 

        public DashboardController(ApplicationDbContext db, UserManager<User> userManager, IWebHostEnvironment environment)
        {
            _db = db;
            _userManager = userManager;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _db.Users.OrderBy(u => u.UserName).ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> ManagePosts()
        {
            var posts = await _db.Posts
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(posts);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            
            var model = new EditUserViewModel
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                PfpURL = user.PfpURL,
                IsAdmin = roles.Contains("Admin"),
                IsEditor = roles.Contains("Editor"),
                IsUser = roles.Contains("User")
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // profile picture upload
            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfilePictureFile.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePictureFile.CopyToAsync(stream);
                }

                model.PfpURL = "/uploads/" + fileName;
            }

            // Update all fields
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Bio = model.Bio;
            user.PfpURL = model.PfpURL;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Update roles (only Admin can change roles)
            var currentRoles = await _userManager.GetRolesAsync(user);
            
            // Remove all current roles
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Add selected roles
            if (model.IsAdmin)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            if (model.IsEditor)
            {
                await _userManager.AddToRoleAsync(user, "Editor");
            }
            if (model.IsUser)
            {
                await _userManager.AddToRoleAsync(user, "User");
            }

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction("ManageUsers");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditPost(int id)
        {
            var post = await _db.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (post == null) return NotFound();

            var model = new EditPostViewModel
            {
                PostId = post.Id,
                Content = post.Content,
                ImageURL = post.ImageURL,
                AuthorUsername = post.User.UserName!
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(EditPostViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var post = await _db.Posts.FindAsync(model.PostId);
            if (post == null) return NotFound();

            //  new image upload
            if (model.NewImage != null && model.NewImage.Length > 0)
            {
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.NewImage.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.NewImage.CopyToAsync(stream);
                }

                model.ImageURL = "/uploads/" + fileName;
            }

            post.Content = model.Content;
            post.ImageURL = model.ImageURL;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Post updated successfully.";
            return RedirectToAction("ManagePosts");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Prevent deleting admins
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                TempData["Error"] = "Cannot delete admin users.";
                return RedirectToAction("ManageUsers");
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();

            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Post deleted successfully.";
            return RedirectToAction("ManagePosts");
        }
    }
}