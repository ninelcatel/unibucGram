using System;
using System.IO;
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
        private readonly ILogger<GroupController> _logger;

        public GroupController(ApplicationDbContext context, UserManager<User> userManager, ILogger<GroupController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
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
                TempData["message"]="Created a new Group";
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
                    gm.Group.ImageURL,  
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
                Name = g.IsDirectMessage ? (g.OtherUser?.UserName ?? "Deleted User") : g.Name,
                Pfp = g.IsDirectMessage ? (g.OtherUser?.PfpURL ?? "/uploads/default_pfp.jpg") : null,
                ImageURL = g.ImageURL,  
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
                // --- START: DELETED SENDER CHECK ---
                // Check if the user who sent the message is deleted.
                // The m.User will be null if the user was hard-deleted.
                string senderName = m.User?.UserName ?? "Deleted User";
                string senderPfp = m.User?.PfpURL ?? "/uploads/default_pfp.jpg";
                // --- END: DELETED SENDER CHECK ---

                // Check if it's a shared post
                if (m.Content.StartsWith("[SHARED_POST:") && m.Content.EndsWith("]"))
                {
                    var postIdStr = m.Content.Substring(13, m.Content.Length - 14);
                    if (int.TryParse(postIdStr, out int postId))
                    {
                        var post = await _context.Posts
                            .Include(p => p.User) // User can be null if deleted
                            .Include(p => p.Likes)
                            .Include(p => p.Comments)
                            .FirstOrDefaultAsync(p => p.Id == postId);

                        // --- START: DELETED POST/AUTHOR CHECK ---
                        if (post != null)
                        {
                            // Check if the post's original author is deleted
                            string postAuthorUsername = post.User?.UserName ?? "Deleted User";
                            string postAuthorPfp = post.User?.PfpURL ?? "/uploads/default_pfp.jpg";

                            messageData.Add(new {
                                m.Id,
                                Content = "Attachment",
                                SharedPost = new {
                                    post.Id,
                                    post.ImageURL,
                                    post.VideoURL,
                                    post.Content,
                                    Username = postAuthorUsername, // Use checked username
                                    UserPfp = postAuthorPfp,       // Use checked PFP
                                    LikesCount = post.Likes.Count,
                                    CommentsCount = post.Comments.Count
                                },
                                SenderName = senderName, // Use checked sender name
                                SenderPfp = senderPfp,   // Use checked sender PFP
                                IsMe = m.UserId == currentUserId,
                                SentAt = m.SentAt.ToString("HH:mm")
                            });
                        }
                        else // The post itself was deleted
                        {
                            messageData.Add(new {
                                m.Id,
                                Content = "Attachment",
                                SharedPost = (object)null, // Send null to indicate a deleted post
                                SenderName = senderName,
                                SenderPfp = senderPfp,
                                IsMe = m.UserId == currentUserId,
                                SentAt = m.SentAt.ToString("HH:mm")
                            });
                        }
                        // --- END: DELETED POST/AUTHOR CHECK ---
                        continue; // Go to the next message
                    }
                }

                // Regular message
                messageData.Add(new {
                    m.Id,
                    m.Content,
                    SharedPost = (object)null,
                    SenderName = senderName, // Use checked sender name
                    SenderPfp = senderPfp,   // Use checked sender PFP
                    IsMe = m.UserId == currentUserId,
                    SentAt = m.SentAt.ToString("HH:mm")
                });
            }

            // Get header info (for DMs, get the other user's pfp)
            string? headerPfp = null;
            if (group.IsDirectMessage)
            {
                var otherUser = group.GroupMembers
                    .FirstOrDefault(gm => gm.UserId != currentUserId)?.User;
                headerPfp = otherUser?.PfpURL;
            }
            string? groupImageUrl = group.IsDirectMessage ? null : group.ImageURL;

            return Json(new { 
                messages = messageData, 
                isDm = group.IsDirectMessage,
                headerPfp, 
                groupImageUrl 
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
        [HttpGet]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> GetGroupMembers(int groupId)
        {   
            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .Include(gm => gm.User)
                .Select(gm => new {
                    userId = gm.UserId,  
                    userName = gm.User != null ? gm.User.UserName : "Unknown User",
                    pfpURL = gm.User != null ? gm.User.PfpURL : null,
                    role = gm.isModerator ? "Moderator" : "Member",
                })
                .ToListAsync();

            return Json(members);
        }
        [HttpGet]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> IsAuthorizedInGroup(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var groupMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == user.Id);

            if (groupMember == null) return NotFound();

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isEditor = await _userManager.IsInRoleAsync(user, "Editor");
            var groupRole = (groupMember.isModerator || isAdmin || isEditor) ? "Moderator" : "Member";
            return Json(new { groupRole, currentUserId = user.Id });
        }
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> UpdateSettings(int groupId, string groupName, IFormFile groupPfpFile, IEnumerable<string> moderatorIds)
        {   
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            
            var groupMember = await _context.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == user.Id);
            if (groupMember == null || !groupMember.isModerator)
            {
                TempData["message"] = "You don't have permission to update group settings.";
                return RedirectToAction("Index", "Home");
            }
            
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
            {
                TempData["message"] = "Group not found.";
                return RedirectToAction("Index", "Home");
            }
            
            group.Name = groupName;
            
            if (groupPfpFile != null && groupPfpFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "grouppfps");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                var uniqueFileName = $"{Guid.NewGuid()}_{groupPfpFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await groupPfpFile.CopyToAsync(fileStream);
                }
                
                group.ImageURL = $"/uploads/grouppfps/{uniqueFileName}";
            }
            
            var modList = (moderatorIds ?? Enumerable.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        
                var groupMembers = await _context.GroupMembers.Where(gm => gm.GroupId == groupId).ToListAsync();
                foreach (var member in groupMembers)
                {
                    member.isModerator = modList.Contains(member.UserId) || member.UserId == user.Id;
                }
            
            await _context.SaveChangesAsync();
            
            TempData["message"] = "Group settings updated successfully.";
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> LeaveGroup(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var groupMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == id && gm.UserId == user.Id);

            if (groupMember == null) return NotFound();

            // logic for ONLY moderator leaving, makes another grMember moderator then leaves
            if (groupMember.isModerator)
            {
                var otherMods = await _context.GroupMembers
                    .CountAsync(gm => gm.GroupId == id && gm.isModerator && gm.UserId != user.Id);
                var count = await _context.GroupMembers.CountAsync(gm => gm.GroupId == id && gm.UserId != user.Id);
                if(otherMods == 0 && count != 0)
                {
                    var newMod = await _context.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == id && !gm.isModerator && gm.UserId != user.Id);
                    if (newMod != null)
                    {
                        newMod.isModerator = true;
                    }
                }
            }
            
            _context.GroupMembers.Remove(groupMember);
            await _context.SaveChangesAsync();


            //if no members left, then delete group
            if(!await _context.GroupMembers.AnyAsync(gm => gm.GroupId == id))
            {
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == id);
                if(group != null)
                {
                    // delete group image file if present
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(group.ImageURL))
                        {
                            var relativePath = group.ImageURL.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                                _logger.LogInformation("Deleted group image file: {FilePath}", filePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete group image for group {GroupId}", id);
                       
                    }

                    _context.Groups.Remove(group);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted group {GroupId} because no members left", id);
               
                }
            }
            
            TempData["message"] = "You left the group.";
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        [Authorize(Roles="User,Editor,Admin")]
        public async Task<IActionResult> KickMember(int groupId, string userId){
            var user = await _userManager.GetUserAsync(User);
            if(user==null) return Unauthorized();
            var currentUser_groupMember = await _context.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == user.Id);
            if(currentUser_groupMember == null || !currentUser_groupMember.isModerator)
            {
                TempData["message"] = "You don't have permission to kick members.";
                return RedirectToAction("Index", "Home");
            }
            
            var groupMemberToKick = await _context.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
            if(groupMemberToKick == null)
            {
                TempData["message"] = "Member not found.";
                return RedirectToAction("Index", "Home");
            }
            _context.GroupMembers.Remove ( groupMemberToKick);
            await _context.SaveChangesAsync();
            
            TempData["message"] = "Member kicked from group.";
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        [Authorize(Roles="User,Editor,Admin")]
        public async Task<IActionResult> AddMember(int groupId, List<string> userIds){
            var user = await _userManager.GetUserAsync(User);
            if(user==null) return Unauthorized();
            var currentUser_groupMember = await _context.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == user.Id);
            if(currentUser_groupMember == null || !currentUser_groupMember.isModerator)
            {
                TempData["message"] = "You don't have permission to add members.";
                return RedirectToAction("Index", "Home");
            }
            
            if (userIds == null || userIds.Count == 0)
            {
                TempData["message"] = "No members selected.";
                return RedirectToAction("Index", "Home");
            }
            
            int addedCount = 0;
            foreach(var userId in userIds)
            {
                var userToAdd = await _userManager.FindByIdAsync(userId);
                if(userToAdd == null) continue;
                
                var alreadyMember = await _context.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
                if(alreadyMember) continue;
                
                var newMember = new GroupMember
                {
                    GroupId = groupId,
                    UserId = userId,
                    isModerator = false
                };
                await _context.GroupMembers.AddAsync(newMember);
                addedCount++;
            }
            
            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                TempData["message"] = $"{addedCount} member{(addedCount != 1 ? "s" : "")} added to group.";
            }
            else
            {
                TempData["message"] = "No new members added.";
            }
            
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> GetGroupInfo(int groupId)
        {
            var group = await _context.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return NotFound();

            var memberCount = await _context.GroupMembers
                .CountAsync(gm => gm.GroupId == groupId);

            return Json(new {
                id = group.Id,
                name = group.Name,
                imageURL = group.ImageURL,
                memberCount = memberCount,
                createdAt = group.CreatedAt,
                isDirectMessage = group.IsDirectMessage
            });
        }
    }
}