using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using unibucGram.Models;

namespace unibucGram.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public HomeController(ApplicationDbContext context, UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (_signInManager.IsSignedIn(User))
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return View(new List<Post>());
            }

            var followingIds = await _context.Follows
                .Where(f => f.FollowerId == currentUser.Id)
                .Select(f => f.FolloweeId)
                .ToListAsync();

            var posts = await _context.Posts
                .Where(p => followingIds.Contains(p.UserId))
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10) 
                .ToListAsync();

            ViewBag.CurrentUser = currentUser;
            return View(posts);
        }

        return View(new List<Post>());
    }

    [HttpGet]
    public async Task<IActionResult> LoadMorePosts(int page = 1)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null || !_signInManager.IsSignedIn(User))
        {
            return BadRequest();
        }

        var followingIds = await _context.Follows
            .Where(f => f.FollowerId == currentUser.Id)
            .Select(f => f.FolloweeId)
            .ToListAsync();

        var posts = await _context.Posts
            .Where(p => followingIds.Contains(p.UserId))
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .ThenInclude(c => c.User)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(page * 10) 
            .Take(10)      
            .ToListAsync();

        return PartialView("_PostFeed", posts);
    }

    [HttpPost]
    [Authorize(Roles = "Guest")] // Only Guests can upgrade
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyAccount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        await _userManager.RemoveFromRoleAsync(user, "Guest");
        await _userManager.AddToRoleAsync(user, "User");
        await _signInManager.RefreshSignInAsync(user);

        TempData["StatusMessage"] = "Success! Your account has been upgraded to User. You can now like, comment, and follow others.";

        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
