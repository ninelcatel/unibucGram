using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using unibucGram.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace unibucGram.Controllers
{
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public GroupController(ApplicationDbContext context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
        _context = context;
            _userManager = userManager;
        }

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
        public async Task<IActionResult> Create(Group Group, List<string> SelectedUserIds)
        {
            // Console.WriteLine("Creating group with name: " + groupName);
            // foreach (var id in SelectedUserIds)
            // {
            //     Console.WriteLine("Member ID: " + id);
            // }


            try
            {
                await _context.Groups.AddAsync(Group);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return RedirectToAction("Login", "Account");
                var members = new List<GroupMember>();
                members.Add(new GroupMember
                {
                    GroupId = Group.Id,
                    UserId = currentUser.Id
                });
                if (SelectedUserIds != null)
                {
                    var distinctIds = SelectedUserIds.Distinct();
                    foreach (var u in distinctIds)
                    {
                        members.Add(new GroupMember
                        {
                            GroupId = Group.Id,
                            UserId = u,
                        });
                    }
                }
                await _context.GroupMembers.AddRangeAsync(members);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");    
            } 
            catch(Exception e)
            {
                Console.WriteLine(e);
                return Error();
            }
        }

        

        [HttpGet]
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
        public async Task<IActionResult> GetMessages(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            var messages = await _context.GroupMessages
                .Where(m => m.GroupId == id)
                .Include(m => m.User)
                .OrderBy(m => m.SentAt)
                .Select(m => new {
                    m.Id,
                    m.Content,
                    SenderName = m.User.UserName,
                    SenderPfp = m.User.PfpURL,
                    IsMe = m.UserId == currentUserId,
                    SentAt = m.SentAt.ToString("HH:mm")
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpPost]
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
    }
}