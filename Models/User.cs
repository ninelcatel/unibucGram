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
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty; 

        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DateOfBirth { get; set; }
        public bool isPrivate { get; set; } = false; // default public profile
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(200)]
        public string? Bio { get; set; } = string.Empty; // e optionala bio

        public string? PfpURL { get; set; } = "/uploads/default_pfp.svg"; // putem adauga un url pt basic pfp dar inca nu stiu cum ,vom vedea

        // lazy loading navigations
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        //si aici e cam la fel ca la conversatii,practic asa e si pe Insta oricum,ai lista cu persoanele pe care le urmaresti si lista cu persoanele care te urmaresc
        //practic adaugam mai multe liste pentru usurinta in navigare
        public virtual ICollection<Follow> Followers { get; set; } = new List<Follow>();
        public virtual ICollection<Follow> Following { get; set; } = new List<Follow>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        // eu spune sa putem lista de likes la posturile unui utilizator,adica deja e acolo,deci nu cred ca mai e nevoie aici,spune mi ce parere ai
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
        //daca lasam o singura lista de conversatii se provoaca ambiguitati,adica am vazut ca tu in conversatation ai fk la userA si userB si in momentu 
        // in care iei o conversatie cu un user,nu ai sti daca e UserA sau UserB.asa ca daca vrem de exemplu conversatiile pe care le are userA luam ConversationsAsUserA
        public virtual ICollection<Conversation> ConversationsAsUserA { get; set; } = new List<Conversation>(); // conversatii unde user-ul este UserA
        public virtual ICollection<Conversation> ConversationsAsUserB { get; set; } = new List<Conversation>(); // conversatii unde user-ul este UserB
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }  
}