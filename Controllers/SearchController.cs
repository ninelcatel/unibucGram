using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;    
using Microsoft.AspNetCore.Mvc;
using unibucGram.Models;

public class SearchController : Controller
{
    private readonly ApplicationDbContext _db;
    public SearchController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Live(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return PartialView("_SearchResults", Enumerable.Empty<User>());
            

        var results = await _db.Users
            .Where(u => (u.UserName).Contains(q))
            .OrderBy(u => u.UserName)
            .Take(10)
            .ToListAsync();
        
        return PartialView("_SearchResults", results);
    }
}