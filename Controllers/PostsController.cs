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
using Microsoft.AspNetCore.Authorization.Infrastructure;

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
        [Authorize(Roles = "User,Admin,Editor")]
        public IActionResult New()
        {
            // This line prevents the blank red box on the New Post page.
            ModelState.Clear();
            return View(new Post());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(524288000)] // 500 MB limit for video uploads
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]

        public async Task<IActionResult> Create([Bind("Content")] Post post, IFormFile? media, IFormFile? thumbnail)
        {
            // Adaugam o validare custom in ModelState
            if (string.IsNullOrWhiteSpace(post.Content) && (media == null || media.Length == 0))
            {
                ModelState.AddModelError(string.Empty, "You must add either text, an image, or a video.");
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

                if (media != null && media.Length > 0)
                {
                    var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                    Directory.CreateDirectory(uploads);

                    var contentType = media.ContentType.ToLower();
                    var isVideo = contentType.StartsWith("video/");

                    if (isVideo)
                    {
                        // Handle video upload
                        var extension = Path.GetExtension(media.FileName).ToLower();
                        var fileName = Guid.NewGuid().ToString() + extension;
                        var filePath = Path.Combine(uploads, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await media.CopyToAsync(stream);
                        }

                        post.VideoURL = "/uploads/" + fileName;
                        post.MediaType = "video";

                        // Handle thumbnail upload if provided
                        if (thumbnail != null && thumbnail.Length > 0)
                        {
                            var thumbFileName = Guid.NewGuid().ToString() + ".jpg";
                            var thumbFilePath = Path.Combine(uploads, thumbFileName);

                            using (var inStream = thumbnail.OpenReadStream())
                            {
                                using (var img = Image.Load(inStream))
                                {
                                    img.Mutate(x => x.AutoOrient());

                                    const int maxDim = 540;
                                    img.Mutate(x => x.Resize(new ResizeOptions
                                    {
                                        Size = new SixLabors.ImageSharp.Size(maxDim, maxDim),
                                        Mode = ResizeMode.Max
                                    }));

                                    var encoder = new JpegEncoder { Quality = 80 };
                                    using (var outStream = System.IO.File.Create(thumbFilePath))
                                    {
                                        img.Save(outStream, encoder);
                                    }
                                }
                            }

                            post.ThumbnailURL = "/uploads/" + thumbFileName;
                        }
                    }
                    else
                    {
                        // Handle image upload (existing logic)
                        var fileName = Guid.NewGuid().ToString() + ".jpg";
                        var filePath = Path.Combine(uploads, fileName);

                        using (var inStream = media.OpenReadStream())
                        {
                            using (var img = Image.Load(inStream))
                            {
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
                        post.MediaType = "image";
                    }
                }

                _db.Posts.Add(post);
                await _db.SaveChangesAsync();

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
            var currentUserId = _userManager.GetUserId(User);
            if (post.User.isPrivate && post.UserId != currentUserId)
            {
                var isFollowing = _db.Follows.Any(f => f.FollowerId == currentUserId && f.FolloweeId == post.UserId);
                if (!isFollowing)
                {
                    return Forbid();
                }
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

        [HttpPost()]
        [Authorize(Roles = "User,Admin,Editor")] // admins editors and guests shouldnt like other ppl posts.
        public async Task<IActionResult> ToggleLike(int postId, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var post = await _db.Posts.FindAsync(postId);
            if (post == null)
            {
                return NotFound();
            }

            var existingLike = await _db.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (existingLike == null)
            {
                // This is a LIKE action
                _db.Likes.Add(new Like { PostId = postId, UserId = userId });

                // Create a notification ONLY if someone else likes the post
                if (post.UserId != userId)
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId = post.UserId,
                        ActorUserId = userId,
                        Type = NotificationType.Like,
                        PostId = postId
                    });
                }
            }
            else
            {
                // This is an UNLIKE action, so we just remove the like
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
        [Authorize(Roles = "User,Admin,Editor")]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var isOwner = post.UserId == currentUser.Id;
            var isAdminOrEditor = await _userManager.IsInRoleAsync(currentUser, "Admin") || await _userManager.IsInRoleAsync(currentUser, "Editor");

            if (!isOwner && !isAdminOrEditor)
            {
                return Forbid();
            }

            ModelState.Clear();
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User,Admin,Editor")]
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

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var isOwner = postToUpdate.UserId == currentUser.Id;
            var isAdminOrEditor = await _userManager.IsInRoleAsync(currentUser, "Admin") || await _userManager.IsInRoleAsync(currentUser, "Editor");

            if (!isOwner && !isAdminOrEditor)
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
        [Authorize(Roles = "User,Admin,Editor")]
        public async Task<IActionResult> Delete(int postId)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var isOwner = post.UserId == currentUser.Id;
            var isAdminOrEditor = await _userManager.IsInRoleAsync(currentUser, "Admin") || await _userManager.IsInRoleAsync(currentUser, "Editor");

            if (!isOwner && !isAdminOrEditor)
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

            if (!string.IsNullOrEmpty(post.VideoURL))
            {
                var videoPath = Path.Combine(_env.WebRootPath, post.VideoURL.TrimStart('/'));
                if (System.IO.File.Exists(videoPath))
                {
                    System.IO.File.Delete(videoPath);
                }
            }

            if (!string.IsNullOrEmpty(post.ThumbnailURL))
            {
                var thumbnailPath = Path.Combine(_env.WebRootPath, post.ThumbnailURL.TrimStart('/'));
                if (System.IO.File.Exists(thumbnailPath))
                {
                    System.IO.File.Delete(thumbnailPath);
                }
            }

            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Profile");
        }

        [HttpGet]
        public async Task<IActionResult> GetCommentsPreview(int postId)
        {
            var post = await _db.Posts
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                return NotFound();
            }

            var latestComments = post.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Take(3)
                .Reverse()
                .ToList();

            if (!latestComments.Any())
            {
                return Content(""); // Return empty if no comments
            }

            return PartialView("_CommentsPreviewPartial", latestComments);
        }

        private bool PostExists(int id)
        {
            return _db.Posts.Any(e => e.Id == id);
        }
    }
}