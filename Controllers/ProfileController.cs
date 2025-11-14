using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using unibucGram.Models;

namespace unibucGram.Controllers
{
    [Route("[controller]")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ApplicationDbContext db, UserManager<User> userManager, ILogger<ProfileController> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            var currentUser = await _db.Users.FindAsync(userId);
            if (currentUser == null)
            {
                return Redirect("/Identity/Account/Login");
            }

            var followersCount = await _db.Follows.CountAsync(f => f.FolloweeId == userId);
            var followingCount = await _db.Follows.CountAsync(f => f.FollowerId == userId);

            var posts = await _db.Posts
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .ToListAsync();

            ViewBag.Posts = posts;
            ViewBag.FollowersCount = followersCount;
            ViewBag.FollowingCount = followingCount;

            return View(currentUser);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet("Show/{name}")]
        public async Task<IActionResult> Show(string name) // username
        {
            if (string.IsNullOrEmpty(name))
                return RedirectToAction("Index", "Home");

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserName == name);

            if (user == null)
            {
                return NotFound();
            }

            var followersCount = await _db.Follows.CountAsync(f => f.FolloweeId == user.Id);
            var followingCount = await _db.Follows.CountAsync(f => f.FollowerId == user.Id);

            var posts = await _db.Posts
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .ToListAsync();

            ViewBag.Posts = posts;
            ViewBag.FollowersCount = followersCount;
            ViewBag.FollowingCount = followingCount;

            return View("Index", user);
        }
    }
}