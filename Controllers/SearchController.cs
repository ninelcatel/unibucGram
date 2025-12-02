using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using unibucGram.Models;

namespace unibucGram.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public SearchController(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Live(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) 
                return PartialView("_SearchResults", Enumerable.Empty<User>());

            var allUsers = await _db.Users
                .Where(u => u.UserName.Contains(q) || 
                           u.FirstName.Contains(q) || 
                           u.LastName.Contains(q))
                .OrderBy(u => u.UserName)
                .Take(50)
                .ToListAsync();

            var filteredResults = new List<User>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("User"))
                {
                    filteredResults.Add(user);
                    if (filteredResults.Count >= 10) break;
                }
            }

            return PartialView("_SearchResults", filteredResults);
        }

        [HttpGet]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> LiveChat(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) 
                return PartialView("_SearchResults_LiveChat", Enumerable.Empty<User>());

            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var allFollowers = await _db.Users
                .Where(u => (u.UserName.Contains(q) || 
                            u.FirstName.Contains(q) || 
                            u.LastName.Contains(q)) && 
                            _db.Follows.Any(f => f.FollowerId == u.Id && f.FolloweeId == currentUserId))
                .OrderBy(u => u.UserName)
                .Take(50)
                .ToListAsync();

            var filteredResults = new List<User>();
            foreach (var user in allFollowers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                if (roles.Contains("User"))
                {
                    filteredResults.Add(user);
                    if (filteredResults.Count >= 10) break;
                }
            }

            return PartialView("_SearchResults_LiveChat", filteredResults);
        }

        // Dashboard search for users (Editors and Admins only)
        [HttpGet]
        [Authorize(Roles = "Editor,Admin")]
        public async Task<IActionResult> DashboardUsers(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return PartialView("_DashboardUserResults", Enumerable.Empty<User>());

            var users = await _db.Users
                .Where(u => u.UserName.Contains(q) || 
                           u.FirstName.Contains(q) || 
                           u.LastName.Contains(q) ||
                           u.Email.Contains(q))
                .OrderBy(u => u.UserName)
                .Take(20)
                .ToListAsync();

            // Attach roles for each user
            var usersWithRoles = new List<(User User, IList<string> Roles)>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add((user, roles));
            }

            return PartialView("_DashboardUserResults", usersWithRoles);
        }

        // Dashboard search for posts (Editors and Admins only)
        [HttpGet]
        [Authorize(Roles = "Editor,Admin")]
        public async Task<IActionResult> DashboardPosts(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return PartialView("_DashboardPostResults", Enumerable.Empty<Post>());

            var posts = await _db.Posts
                .Include(p => p.User)
                .Where(p => p.Content.Contains(q) || 
                           p.User.UserName.Contains(q))
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            return PartialView("_DashboardPostResults", posts);
        }
    }
}