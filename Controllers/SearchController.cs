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
    [Authorize]
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
        [Authorize(Roles = "User,Editor,Admin")] // Guests cannot search
        public async Task<IActionResult> Live(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) 
                return PartialView("_SearchResults", Enumerable.Empty<User>());

            var allUsers = await _db.Users
                .Where(u => u.UserName.Contains(q) || 
                           u.FirstName.Contains(q) || 
                           u.LastName.Contains(q))
                .OrderBy(u => u.UserName)
                .Take(50) // Take more, then filter
                .ToListAsync();

            // Filter by role
            var filteredResults = new List<User>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("User"))
                {
                    filteredResults.Add(user);
                    if (filteredResults.Count >= 10) break; // Limit results
                }
            }

            return PartialView("_SearchResults", filteredResults);
        }

        [HttpGet]
        [Authorize(Roles = "User,Editor,Admin")] // Guests cannot search for chats
        public async Task<IActionResult> LiveChat(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) 
                return PartialView("_SearchResults_LiveChat", Enumerable.Empty<User>());

            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            // Find followers only
            var allFollowers = await _db.Users
                .Where(u => (u.UserName.Contains(q) || 
                            u.FirstName.Contains(q) || 
                            u.LastName.Contains(q)) && 
                            _db.Follows.Any(f => f.FollowerId == u.Id && f.FolloweeId == currentUserId))
                .OrderBy(u => u.UserName)
                .Take(50)
                .ToListAsync();

            // Filter by role
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
    }
}