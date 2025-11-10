using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace unibucGram.Models
{
    public class User : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; } 

        [Required]
        public DateTime DateOfBirth { get; set; }
        public bool isPrivate { get; set; } = false; // default public profile
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Bio { get; set; } = string.Empty; // e optionala bio

        public string? PfpURL { get; set; } = string.Empty; // putem adauga un url pt basic pfp dar inca nu stiu cum ,vom vedea
        /*  
        TODO:
        - Add navigation properties for relationships 
        (e.g., Posts, Followers, Following, Commments, Likes, etc.)
        with virtual keyworkd (+collections daca este 1-M/M-M) for lazy loading 
        */
    }
}