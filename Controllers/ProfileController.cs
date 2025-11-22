using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using unibucGram.Models;

namespace unibucGram.Controllers
{
    [Authorize]
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
            ViewBag.isOwnProfile = true;
            return View(currentUser);
        }

        [HttpPost("FollowToggle")] // FIX: Removed "Profile/" prefix
        public async Task<IActionResult> FollowToggle(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var targetedUser = await _db.Users.FindAsync(userId);

            if (string.IsNullOrEmpty(currentUserId) || targetedUser == null || currentUserId == userId)
            {
                return BadRequest();
            }

            // --- UNFOLLOW LOGIC ---
            var existingFollow = await _db.Follows.FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FolloweeId == userId);
            if (existingFollow != null)
            {
                _db.Follows.Remove(existingFollow);
                await _db.SaveChangesAsync();
                return RedirectToAction("Show", new { name = targetedUser.UserName });
            }

            // --- FOLLOW/REQUEST LOGIC ---
            if (targetedUser.isPrivate)
            {
                var existingRequest = await _db.FollowRequests.FirstOrDefaultAsync(fr => fr.RequesterId == currentUserId && fr.RequesteeId == userId);
                if (existingRequest == null)
                {
                    _db.FollowRequests.Add(new FollowRequest { RequesterId = currentUserId, RequesteeId = userId });
                    _db.Notifications.Add(new Notification { UserId = userId, ActorUserId = currentUserId, Type = NotificationType.FollowRequest });
                }
            }
            else
            {
                _db.Follows.Add(new Follow { FollowerId = currentUserId, FolloweeId = userId });
                _db.Notifications.Add(new Notification { UserId = userId, ActorUserId = currentUserId, Type = NotificationType.Follow });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Show", new { name = targetedUser.UserName });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet("Error")] // FIX: Removed "Profile/" prefix
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        [HttpGet("Show/{name}")]
        public async Task<IActionResult> Show(string name) // username
        {
            if (string.IsNullOrEmpty(name))
            {
                return NotFound();
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserName == name);

            if (user == null)
            {
                return NotFound();
            }

            // --- START OF FIX ---
            // Calculate and pass all necessary data to the view
            var posts = await _db.Posts
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .ToListAsync();

            ViewBag.Posts = posts;
            ViewBag.FollowersCount = await _db.Follows.CountAsync(f => f.FolloweeId == user.Id);
            ViewBag.FollowingCount = await _db.Follows.CountAsync(f => f.FollowerId == user.Id);
            // --- END OF FIX ---

            var currentUserId = _userManager.GetUserId(User);
            var isOwnProfile = currentUserId == user.Id;
            ViewBag.IsOwnProfile = isOwnProfile;

            ViewBag.IsFollowedByCurrentUser = false;
            ViewBag.FollowRequestSent = false;

            if (!isOwnProfile && currentUserId != null)
            {
                ViewBag.IsFollowedByCurrentUser = await _db.Follows
                    .AnyAsync(f => f.FollowerId == currentUserId && f.FolloweeId == user.Id);
                
                if (!(bool)ViewBag.IsFollowedByCurrentUser)
                {
                    ViewBag.FollowRequestSent = await _db.FollowRequests
                        .AnyAsync(fr => fr.RequesterId == currentUserId && fr.RequesteeId == user.Id);
                }
            }

            return View("Index", user);
        }
        [HttpGet("Edit")]
        public IActionResult Edit()
        {
            return Redirect("/Identity/Account/Manage");
        }


        [HttpPost("HandleFollowRequest")] // FIX: Removed "Profile/" prefix
        [Authorize]
        public async Task<IActionResult> HandleFollowRequest(string actorUsername, string actionType)
        {
            var currentUserId = _userManager.GetUserId(User);
            var actor = await _db.Users.FirstOrDefaultAsync(u => u.UserName == actorUsername);

            if (actor == null || currentUserId == null) return BadRequest(new { success = false, message = "User not found." });

            var request = await _db.FollowRequests.FirstOrDefaultAsync(fr => fr.RequesterId == actor.Id && fr.RequesteeId == currentUserId);
            if (request == null) return NotFound(new { success = false, message = "Request not found." });

            if (actionType == "accept")
            {
                _db.Follows.Add(new Follow { FollowerId = actor.Id, FolloweeId = currentUserId });
            }

            _db.FollowRequests.Remove(request);

            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.ActorUserId == actor.Id && n.UserId == currentUserId && n.Type == NotificationType.FollowRequest);
            if (notification != null)
            {
                notification.IsRead = true;
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}