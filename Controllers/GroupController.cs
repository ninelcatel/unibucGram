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
    }
}