using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using unibucGram.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace unibucGram.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public GroupController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return RedirectToAction("Index","Home");
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")] // Guests cannot create groups
        public async Task<IActionResult> Create(Group Group, List<string> SelectedUserIds)
        {
            try
            {
                await _context.Groups.AddAsync(Group);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                var members = new List<GroupMember>
                {
                    new GroupMember
                    {
                        GroupId = Group.Id,
                        UserId = currentUser.Id,
                        isModerator = true
                    }
                };

                if (SelectedUserIds != null)
                {
                    var distinctIds = SelectedUserIds.Distinct();
                    foreach (var userId in distinctIds)
                    {   
                        var user = await _userManager.FindByIdAsync(userId);
                        if (user == null) continue; // Skip invalid user IDs
                        
                        members.Add(new GroupMember
                        {
                            GroupId = Group.Id,
                            UserId = userId,
                            isModerator = false
                        });
                    }
                }

                await _context.GroupMembers.AddRangeAsync(members);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");    
            } 
            catch (Exception e)
            {
                Console.WriteLine(e);
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> GetUserGroups()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var groups = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => new {
                    gm.Group.Id,
                    gm.Group.Name,
                    gm.Group.IsDirectMessage,
                    OtherUser = gm.Group.GroupMembers
                        .Where(m => m.UserId != userId)
                        .Select(m => new { m.User.UserName, m.User.PfpURL })
                        .FirstOrDefault(),
                    LastMessage = gm.Group.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.Content)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = groups.Select(g => new {
                g.Id,
                Name = g.IsDirectMessage ? g.OtherUser?.UserName : g.Name,
                Pfp = g.IsDirectMessage ? g.OtherUser?.PfpURL : null,
                IsDm = g.IsDirectMessage,
                LastMessage = g.LastMessage
            });

            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> GetMessages(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            var group = await _context.Groups
                .Include(g => g.GroupMembers)
                .ThenInclude(gm => gm.User)
                .FirstOrDefaultAsync(g => g.Id == id);
            
            if (group == null) return NotFound();
            
            var messages = await _context.GroupMessages
                .Where(m => m.GroupId == id)
                .Include(m => m.User)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var messageData = new List<object>();

            foreach (var m in messages)
            {
                // Check if it's a shared post
                if (m.Content.StartsWith("[SHARED_POST:") && m.Content.EndsWith("]"))
                {
                    var postIdStr = m.Content.Substring(13, m.Content.Length - 14);
                    if (int.TryParse(postIdStr, out int postId))
                    {
                        var post = await _context.Posts
                            .Include(p => p.User)
                            .Include(p => p.Likes)
                            .Include(p => p.Comments)
                            .FirstOrDefaultAsync(p => p.Id == postId);

                        if (post != null)
                        {
                            messageData.Add(new {
                                m.Id,
                                Content = "Attachment", // Display "Attachment" instead of [SHARED_POST:...]
                                SharedPost = new {
                                    post.Id,
                                    post.ImageURL,
                                    post.Content,
                                    Username = post.User?.UserName,
                                    UserPfp = post.User?.PfpURL,
                                    LikesCount = post.Likes.Count,
                                    CommentsCount = post.Comments.Count
                                },
                                SenderName = m.User.UserName,
                                SenderPfp = m.User.PfpURL,
                                IsMe = m.UserId == currentUserId,
                                SentAt = m.SentAt.ToString("HH:mm")
                            });
                            continue;
                        }
                    }
                }

                // Regular message
                messageData.Add(new {
                    m.Id,
                    m.Content,
                    SharedPost = (object)null,
                    SenderName = m.User.UserName,
                    SenderPfp = m.User.PfpURL,
                    IsMe = m.UserId == currentUserId,
                    SentAt = m.SentAt.ToString("HH:mm")
                });
            }

            // Get header info (for DMs, get the other user's pfp)
            string headerPfp = null;
            if (group.IsDirectMessage)
            {
                var otherUser = group.GroupMembers
                    .FirstOrDefault(gm => gm.UserId != currentUserId)?.User;
                headerPfp = otherUser?.PfpURL;
            }

            return Json(new { 
                messages = messageData, 
                isDm = group.IsDirectMessage,
                headerPfp 
            });
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> SendMessage(int groupId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(content)) return BadRequest();

            var msg = new GroupMessage
            {
                GroupId = groupId,
                UserId = user.Id,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _context.GroupMessages.Add(msg);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> SharePost([FromBody] SharePostRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == request.PostId);
            
            if (post == null) return NotFound();

            // Create a message for each selected group
            foreach (var groupId in request.GroupIds)
            {
                // Verify user is member of the group
                var isMember = await _context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == user.Id);
                
                if (!isMember) continue;

                var msg = new GroupMessage
                {
                    GroupId = groupId,
                    UserId = user.Id,
                    Content = $"[SHARED_POST:{request.PostId}]", // Special format to identify shared posts
                    SentAt = DateTime.UtcNow
                };

                _context.GroupMessages.Add(msg);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public class SharePostRequest
        {
            public int PostId { get; set; }
            public List<int> GroupIds { get; set; }
        }

        [HttpGet]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> SearchGroups(string q)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var groups = await _context.GroupMembers
                .Where(gm => gm.UserId == userId && gm.Group.Name.Contains(q))
                .Select(gm => new {
                    gm.Group.Id,
                    gm.Group.Name,
                    gm.Group.IsDirectMessage,
                    OtherUser = gm.Group.GroupMembers
                        .Where(m => m.UserId != userId)
                        .Select(m => new { m.User.UserName, m.User.PfpURL })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = groups.Select(g => new {
                g.Id,
                Name = g.IsDirectMessage ? g.OtherUser?.UserName : g.Name,
                Pfp = g.IsDirectMessage ? g.OtherUser?.PfpURL : null,
                IsDm = g.IsDirectMessage
            });

            return Json(result);
        }
    }
}