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

            // Cascade delete all related data to avoid foreign key constraints
            // Order matters: 
            var userPosts = await _db.Posts.Where(p => p.UserId == id).ToListAsync();
            foreach (var post in userPosts)
            {
                
                var postNotifications = await _db.Notifications.Where(n => n.PostId == post.Id).ToListAsync();
                _db.Notifications.RemoveRange(postNotifications);

                var postComments = await _db.Comments.Where(c => c.PostId == post.Id).ToListAsync();
                _db.Comments.RemoveRange(postComments);

                var postLikes = await _db.Likes.Where(l => l.PostId == post.Id).ToListAsync();
                _db.Likes.RemoveRange(postLikes);
            }
            _db.Posts.RemoveRange(userPosts);

            var userComments = await _db.Comments.Where(c => c.UserId == id).ToListAsync();
            _db.Comments.RemoveRange(userComments);

            
            var userLikes = await _db.Likes.Where(l => l.UserId == id).ToListAsync();
            _db.Likes.RemoveRange(userLikes);

            var userNotifications = await _db.Notifications
                .Where(n => n.UserId == id)
                .ToListAsync();
            _db.Notifications.RemoveRange(userNotifications);

            var followRelations = await _db.Follows
                .Where(f => f.FollowerId == id || f.FolloweeId == id)
                .ToListAsync();
            _db.Follows.RemoveRange(followRelations);

            var followRequests = await _db.FollowRequests
                .Where(f => f.RequesterId == id || f.RequesteeId == id)
                .ToListAsync();
            _db.FollowRequests.RemoveRange(followRequests);

            var groupMembers = await _db.GroupMembers.Where(gm => gm.UserId == id).ToListAsync();
            _db.GroupMembers.RemoveRange(groupMembers);

            var groupMessages = await _db.GroupMessages.Where(gm => gm.UserId == id).ToListAsync();
            _db.GroupMessages.RemoveRange(groupMessages);

            
            _db.Users.Remove(user);

            await _db.SaveChangesAsync();

            TempData["Success"] = $"User {user.UserName} and all related content deleted successfully.";
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();

           
            var notifications = await _db.Notifications.Where(n => n.PostId == id).ToListAsync();
            _db.Notifications.RemoveRange(notifications);

            
            var comments = await _db.Comments.Where(c => c.PostId == id).ToListAsync();
            _db.Comments.RemoveRange(comments);

            var likes = await _db.Likes.Where(l => l.PostId == id).ToListAsync();
            _db.Likes.RemoveRange(likes);

            _db.Posts.Remove(post);

            await _db.SaveChangesAsync();

            TempData["Success"] = "Post and all related content deleted successfully.";
            return RedirectToAction("ManagePosts");
        }
    }
}