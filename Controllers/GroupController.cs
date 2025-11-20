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
using System.Text.RegularExpressions;

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
            return View("Error!");
        }

        [HttpPost]
        public async Task<IActionResult> Create(string groupName, List<string> SelectedUserIds)
        {
            Console.WriteLine("Creating group with name: " + groupName);
            foreach (var id in SelectedUserIds)
            {
                Console.WriteLine("Member ID: " + id);
            }
            
            //TODO :: add into database (first create the models)
            return RedirectToAction("Index", "Home");
        }
    }
}