using System.ComponentModel.DataAnnotations;

namespace unibucGram.Models
{
    public class EditUserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;
        
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }
        
        [Display(Name = "Bio")]
        public string? Bio { get; set; }
        
        [Display(Name = "Profile Picture URL")]
        public string? PfpURL { get; set; }
        
        [Display(Name = "Admin Role")]
        public bool IsAdmin { get; set; }
        
        [Display(Name = "Editor Role")]
        public bool IsEditor { get; set; }
        
        [Display(Name = "User Role")]
        public bool IsUser { get; set; }
        
        // Add this for file upload
        [Display(Name = "Profile Picture File")]
        public IFormFile? ProfilePictureFile { get; set; }
    }
}