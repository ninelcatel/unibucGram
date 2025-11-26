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

        [HttpPost("FollowToggle")]
        [Authorize(Roles = "User,Editor,Admin")] // Allow all authenticated users except Guests
        public async Task<IActionResult> FollowToggle(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var targetedUser = await _db.Users.FindAsync(userId);

            if (string.IsNullOrEmpty(currentUserId) || targetedUser == null || currentUserId == userId)
            {
                return BadRequest();
            }

            // UNFOLLOW LOGIC 
            var existingFollow = await _db.Follows.FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FolloweeId == userId);
            if (existingFollow != null)
            {
                _db.Follows.Remove(existingFollow);
                await _db.SaveChangesAsync();
                return RedirectToAction("Show", new { name = targetedUser.UserName });
            }

            // FOLLOW/REQUEST LOGIC 
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

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == name);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var isOwnProfile = currentUserId == user.Id;
            
            var isFollowing = !isOwnProfile && currentUserId != null && 
                              await _db.Follows.AnyAsync(f => f.FollowerId == currentUserId && f.FolloweeId == user.Id);

            // Determine if the current user can view the posts
            bool canViewPosts = !user.isPrivate || isOwnProfile || isFollowing;
            ViewBag.CanViewPosts = canViewPosts;

            if (canViewPosts)
            {
                ViewBag.Posts = await _db.Posts
                    .Where(p => p.UserId == user.Id)
                    .OrderByDescending(p => p.CreatedAt)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .ToListAsync();
            }
            else
            {
                ViewBag.Posts = new List<Post>(); // Provide an empty list for private profiles
            }

            ViewBag.FollowersCount = await _db.Follows.CountAsync(f => f.FolloweeId == user.Id);
            ViewBag.FollowingCount = await _db.Follows.CountAsync(f => f.FollowerId == user.Id);
            
            ViewBag.IsOwnProfile = isOwnProfile;
            ViewBag.IsFollowedByCurrentUser = isFollowing; // Reuse the variable
            ViewBag.FollowRequestSent = false;

            if (!isOwnProfile && currentUserId != null && !isFollowing)
            {
                ViewBag.FollowRequestSent = await _db.FollowRequests
                    .AnyAsync(fr => fr.RequesterId == currentUserId && fr.RequesteeId == user.Id);
            }

            return View("Index", user);
        }
        [HttpGet("Edit")]
        public IActionResult Edit()
        {   
            return Redirect("/Identity/Account/Manage");
        }


        [HttpPost("HandleFollowRequest")] 
        [Authorize(Roles ="User,Admin,Editor")]
        public async Task<IActionResult> HandleFollowRequest(string actorUsername, string actionType)
        {
            var currentUserId = _userManager.GetUserId(User);
            var actor = await _db.Users.FirstOrDefaultAsync(u => u.UserName == actorUsername);

            if (actor == null || currentUserId == null) return BadRequest(new { success = false, message = "User not found." });

            var request = await _db.FollowRequests.FirstOrDefaultAsync(fr => fr.RequesterId == actor.Id && fr.RequesteeId == currentUserId);
            if (request == null) return NotFound(new { success = false, message = "Request not found." });

            if (request.RequesteeId != currentUserId)
            {
                return Forbid();
            }

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
    
        [HttpGet("GetFollowers/{username}")]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> GetFollowers(string username, int page = 1, int pageSize = 20)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null) return NotFound();

            var followers = await _db.Follows
                .Where(f => f.FolloweeId == user.Id)
                .Include(f => f.Follower)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.Follower.Id,
                    f.Follower.UserName,
                    f.Follower.FirstName,
                    f.Follower.LastName,
                    f.Follower.PfpURL
                })
                .ToListAsync();

            return Json(new { users = followers });
        }

        [HttpGet("GetFollowing/{username}")]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> GetFollowing(string username, int page = 1, int pageSize = 20)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null) return NotFound();

            var following = await _db.Follows
                .Where(f => f.FollowerId == user.Id)
                .Include(f => f.Followee)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.Followee.Id,
                    f.Followee.UserName,
                    f.Followee.FirstName,
                    f.Followee.LastName,
                    f.Followee.PfpURL
                })
                .ToListAsync();

            return Json(new { users = following });
        }

        [HttpGet("GetNotFollowingBack/{username}")]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> GetNotFollowingBack(string username, int page = 1, int pageSize = 20)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId != user.Id) return Forbid();

            var following = await _db.Follows
                .Where(f => f.FollowerId == user.Id)
                .Select(f => f.FolloweeId)
                .ToListAsync();

            var followers = await _db.Follows
                .Where(f => f.FolloweeId == user.Id)
                .Select(f => f.FollowerId)
                .ToListAsync();

            var notFollowingBackIds = following.Except(followers).ToList();

            var notFollowingBackUsers = await _db.Users
                .Where(u => notFollowingBackIds.Contains(u.Id))
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.FirstName,
                    u.LastName,
                    u.PfpURL
                })
                .ToListAsync();

            return Json(new { users = notFollowingBackUsers });
        }
    }
}
