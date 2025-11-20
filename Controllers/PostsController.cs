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
            // This line prevents the blank red box on the "New Post" page.
            ModelState.Clear();
            return View(new Post());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Content")] Post post, IFormFile? image)
        {
            // Adaugam o validare custom in ModelState
            if (string.IsNullOrWhiteSpace(post.Content) && (image == null || image.Length == 0))
            {
                ModelState.AddModelError(string.Empty, "You must add either text or an image.");
            }

            // Eliminam campurile setate de server din validare
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("CreatedAt");

            // Verificam atat validarile din model ([StringLength]) cat si cea custom
            if (ModelState.IsValid)
            {
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

            // Daca modelul nu e valid, ne intoarcem la formular cu erorile
            return View("New", post);
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

        [HttpGet]
        public IActionResult PostPartial(int id)
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

            return PartialView("~/Views/Shared/_PostModalContent.cshtml", post);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(int postId, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

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

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var likesCount = await _db.Likes.CountAsync(l => l.PostId == postId);
                return Json(new { success = true, likesCount = likesCount, liked = existingLike == null });
            }

            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var post = await _db.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            // Security check: Only the owner can edit the post
            if (post.UserId != userId)
            {
                return Forbid();
            }
            
            // This line prevents the blank red box on the "Edit Post" page.
            ModelState.Clear();
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Content")] Post postData, IFormFile? newImage, bool removeImage = false)
        {
            if (id != postData.Id)
            {
                return NotFound();
            }

            // Eliminam campurile setate de server din validare
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("CreatedAt");

            var postToUpdate = await _db.Posts.FindAsync(id);
            if (postToUpdate == null)
            {
                return NotFound();
            }

            // Security check
            var userId = _userManager.GetUserId(User);
            if (postToUpdate.UserId != userId)
            {
                return Forbid();
            }

            // --- LOGICA NOUA DE VALIDARE SI PROCESARE IMAGINE ---

            // 1. Daca se doreste stergerea imaginii
            if (removeImage && !string.IsNullOrEmpty(postToUpdate.ImageURL))
            {
                var oldImagePath = Path.Combine(_env.WebRootPath, postToUpdate.ImageURL.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
                postToUpdate.ImageURL = null;
            }
            // 2. Daca se incarca o imagine noua
            else if (newImage != null && newImage.Length > 0)
            {
                // Stergem imaginea veche daca exista
                if (!string.IsNullOrEmpty(postToUpdate.ImageURL))
                {
                    var oldImagePath = Path.Combine(_env.WebRootPath, postToUpdate.ImageURL.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Salvam imaginea noua
                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                var fileName = Guid.NewGuid().ToString() + ".jpg";
                var filePath = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newImage.CopyToAsync(stream);
                }
                postToUpdate.ImageURL = "/uploads/" + fileName;
            }

            // 3. Validare finala: postarea nu poate fi complet goala
            if (string.IsNullOrWhiteSpace(postData.Content) && string.IsNullOrEmpty(postToUpdate.ImageURL))
            {
                ModelState.AddModelError("Content", "Post cannot be empty. Please add a caption or an image.");
            }

            if (ModelState.IsValid)
            {
                postToUpdate.Content = postData.Content;

                try
                {
                    _db.Update(postToUpdate);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(postToUpdate.Id)) { return NotFound(); }
                    else { throw; }
                }
                return RedirectToAction(nameof(Post), new { id = postToUpdate.Id });
            }

            // Daca validarea esueaza, retrimitem modelul actualizat la view
            return View(postToUpdate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int postId)
        {
            var userId = _userManager.GetUserId(User);
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                return NotFound();
            }

            if (post.UserId != userId)
            {
                return Forbid();
            }

            if (!string.IsNullOrEmpty(post.ImageURL))
            {
                var imagePath = Path.Combine(_env.WebRootPath, post.ImageURL.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Profile");
        }

        private bool PostExists(int id)
        {
            return _db.Posts.Any(e => e.Id == id);
        }
    }
}