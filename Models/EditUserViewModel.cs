using System.ComponentModel.DataAnnotations;

namespace unibucGram.Models
{
    public class EditUserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required.")]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }
        
        [StringLength(200, ErrorMessage = "Bio cannot exceed 200 characters.")]
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
        
        [Display(Name = "Profile Picture File")]
        public IFormFile? ProfilePictureFile { get; set; }
    }
}