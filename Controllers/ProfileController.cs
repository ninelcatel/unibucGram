using System.Linq;
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
    public IActionResult Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            var currentUser = _db.Users.Find(userId);
            if (currentUser == null)
            {
                return Redirect("/Identity/Account/Login");
            }

            var followersCount = _db.Follows.Count(f => f.FolloweeId == userId);
            var followingCount = _db.Follows.Count(f => f.FollowerId == userId);

            var posts = _db.Posts
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .ToList();

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
    }
}