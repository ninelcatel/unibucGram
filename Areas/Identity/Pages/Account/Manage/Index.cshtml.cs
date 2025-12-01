using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using unibucGram.Models;

namespace unibucGram.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public string Username { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = null!;

        public class InputModel
        {
            [Required]
            [StringLength(50)]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [StringLength(50)]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime DateOfBirth { get; set; }

            [StringLength(200)]
            [Display(Name = "Bio")]
            public string? Bio { get; set; }

            [Display(Name = "Private Account")]
            public bool IsPrivate { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Profile Picture")]
            public IFormFile? ProfilePicture { get; set; }

            public string? PfpURL { get; set; }

            public string? ProfilePictureBase64 { get; set; }
        }

        private async Task LoadAsync(User user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName ?? string.Empty;

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Bio = user.Bio,
                IsPrivate = user.isPrivate,
                PhoneNumber = phoneNumber,
                PfpURL = user.PfpURL ?? "/uploads/default_pfp.jpg"
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Validate phone number format if provided
            if (!string.IsNullOrWhiteSpace(Input.PhoneNumber))
            {
                // Basic validation: must be digits/spaces/dashes/parentheses and between 10-15 chars
                var cleaned = new string(Input.PhoneNumber.Where(c => char.IsDigit(c)).ToArray());
                if (cleaned.Length < 10 || cleaned.Length > 15)
                {
                    ModelState.AddModelError("Input.PhoneNumber", "Phone number must contain 10-15 digits.");
                    await LoadAsync(user);
                    return Page();
                }
            }

            // Update user properties
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.DateOfBirth = Input.DateOfBirth;
            user.Bio = Input.Bio;
            user.isPrivate = Input.IsPrivate;

            // Check if user wants to remove profile picture (set to default)
            if (Input.PfpURL == "/uploads/default_pfp.jpg" && user.PfpURL != "/uploads/default_pfp.jpg")
            {
                // Delete old file if it exists
                if (!string.IsNullOrWhiteSpace(user.PfpURL))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.PfpURL.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }
                user.PfpURL = "/uploads/default_pfp.jpg";
            }
            // Check if user uploaded a new profile picture via base64
            else if (!string.IsNullOrEmpty(Input.ProfilePictureBase64))
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + ".jpg";
                var filePath = Path.Combine(uploadsFolder, fileName);

                var base64Data = Input.ProfilePictureBase64.Split(',')[1];
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                
                // Delete old file if it exists and is not default
                if (!string.IsNullOrWhiteSpace(user.PfpURL) && user.PfpURL != "/uploads/default_pfp.jpg")
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.PfpURL.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }
                
                user.PfpURL = "/uploads/" + fileName;
            }
            // Check if user uploaded via file input directly
            else if (Input.ProfilePicture != null && Input.ProfilePicture.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + ".jpg";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var image = Image.Load(Input.ProfilePicture.OpenReadStream()))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(300, 300),
                        Mode = ResizeMode.Crop
                    }));
                    await image.SaveAsJpegAsync(filePath);
                }

                // Delete old file if it exists and is not default
                if (!string.IsNullOrWhiteSpace(user.PfpURL) && user.PfpURL != "/uploads/default_pfp.jpg")
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.PfpURL.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                user.PfpURL = "/uploads/" + fileName;
            }

            // Update user via UserManager
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                StatusMessage = "Unexpected error when trying to update profile.";
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadAsync(user);
                return Page();
            }

            // Update phone number
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }
            

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
