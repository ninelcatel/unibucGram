using System.ComponentModel.DataAnnotations;

namespace unibucGram.Models
{
    public class EditPostViewModel
    {
        public int PostId { get; set; }
        
        [StringLength(2000, ErrorMessage = "Content cannot exceed 2000 characters.")]
        [Display(Name = "Content")]
        public string? Content { get; set; }
        
        [Display(Name = "Image URL")]
        public string? ImageURL { get; set; }
        
        [Required(ErrorMessage = "Author username is required.")]
        public string AuthorUsername { get; set; } = string.Empty;
        
        [Display(Name = "New Image File")]
        public IFormFile? NewImage { get; set; }
    }
}