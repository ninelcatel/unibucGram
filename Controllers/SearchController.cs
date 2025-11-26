using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;    
using Microsoft.AspNetCore.Mvc;
using unibucGram.Models;
using Microsoft.AspNetCore.Identity; // Required for UserManager

public class SearchController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager; // Inject UserManager

    public SearchController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Live(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return PartialView("_SearchResults", Enumerable.Empty<User>());


        var results = await _db.Users
            .Where(u => u.UserName.Contains(q))
            .OrderBy(u => u.UserName)
            .Take(13)
            .ToListAsync();
        foreach (var user in results)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if(roles.Contains("Guest") || roles.Contains("Admin") || roles.Contains("Editor")) 
            {
                results.Remove(user);
            }
        }
        return PartialView("_SearchResults", results);
    }

    // Create a Group form search bar --> followers only
    [HttpGet]
    public async Task<IActionResult> LiveChat(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return PartialView("_SearchResults", Enumerable.Empty<User>());
            

        var currentUserId = _userManager.GetUserId(User);

        // Find users matching 'q' WHO ALSO follow the current user
        // Assuming 'Follows' table has FollowerId (who follows) and FolloweeId (who is followed)
        // We want people where FollowerId = FoundUser AND FolloweeId = CurrentUser
        
        var results = await _db.Users
            .Where(u => u.UserName.Contains(q) && 
                        _db.Follows.Any(f => f.FollowerId == u.Id && f.FolloweeId == currentUserId))
            .OrderBy(u => u.UserName)
            .Take(13)
            .ToListAsync();
        foreach (var user in results)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if(roles.Contains("Guest") || roles.Contains("Admin") || roles.Contains("Editor")) 
            {
                results.Remove(user);
            }
        }
        return PartialView("_SearchResults_LiveChat", results);
    }
}