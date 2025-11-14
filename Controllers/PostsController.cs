using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using unibucGram.Models;

namespace unibucGram.Controllers
{
    [Authorize]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _env;

        public PostsController(ApplicationDbContext db, UserManager<User> userManager, IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        [HttpGet]
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Content")] Post post, IFormFile? image)
        {
            // basic validation: require either content or image
            if (string.IsNullOrWhiteSpace(post.Content) && (image == null || image.Length == 0))
            {
                ModelState.AddModelError(string.Empty, "Trebuie să adaugi text sau o imagine.");
                return View("New", post);
            }

            var userId = _userManager.GetUserId(User);
            post.UserId = userId ?? string.Empty;
            post.CreatedAt = DateTime.UtcNow;

            if (image != null && image.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                Directory.CreateDirectory(uploads);

                // normalize to jpeg and resize to a max dimension (1080x1080)
                var fileName = Guid.NewGuid().ToString() + ".jpg";
                var filePath = Path.Combine(uploads, fileName);

                using (var inStream = image.OpenReadStream())
                {
                    // Load image (ImageSharp will detect format)
                    using (var img = Image.Load(inStream))
                    {
                        // Auto orient based on EXIF so phone images display correctly
                        img.Mutate(x => x.AutoOrient());

                        const int maxDim = 540;
                        img.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new SixLabors.ImageSharp.Size(maxDim, maxDim),
                            Mode = ResizeMode.Max
                        }));

                        var encoder = new JpegEncoder { Quality = 80 };
                        using (var outStream = System.IO.File.Create(filePath))
                        {
                            img.Save(outStream, encoder);
                        }
                    }
                }

                post.ImageURL = "/uploads/" + fileName;
            }

            _db.Posts.Add(post);
            _db.SaveChanges();

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Post(int id)
        {
            var post = _db.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int postId, string returnUrl)
        {
            var userId = _userManager.GetUserId(User);

            if (userId != null)
            {
                var existingLike = await _db.Likes
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

                if (existingLike == null)
                {
                    _db.Likes.Add(new Like { PostId = postId, UserId = userId });
                }
                else
                {
                    _db.Likes.Remove(existingLike);
                }
                await _db.SaveChangesAsync();
            }

            return LocalRedirect(returnUrl);
        }
    }
}
