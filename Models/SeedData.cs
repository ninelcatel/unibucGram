using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace unibucGram.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                
                if (context.Roles.Any())
                {
                    return;   
                }

         
                context.Roles.AddRange(
                    new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
                    new IdentityRole { Name = "User", NormalizedName = "USER" },
                    new IdentityRole { Name = "Editor", NormalizedName = "EDITOR" },
                    new IdentityRole { Name = "Guest", NormalizedName = "GUEST" }
                );
                
                context.SaveChanges(); 

              
                var hasher = new PasswordHasher<User>();
                
                context.Users.AddRange(
                    new User{
                        UserName = "admin@test.com",
                        NormalizedUserName = "ADMIN@TEST.COM",
                        Email = "admin@test.com",
                        NormalizedEmail = "ADMIN@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "Admin123!"),
                        SecurityStamp = Guid.NewGuid().ToString(),
                        FirstName = "Admin",
                        LastName = "Administrator",
                        PfpURL = "/uploads/default_pfp.jpg"
                    },
                    new User{
                        UserName = "editor@test.com",
                        NormalizedUserName = "EDITOR@TEST.COM",
                        Email = "editor@test.com",
                        NormalizedEmail = "EDITOR@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "Editor123!"),
                        SecurityStamp = Guid.NewGuid().ToString(),
                        FirstName = "Editor",
                        LastName = "Staff",
                        PfpURL = "/uploads/default_pfp.jpg"
                    },
                    new User{
                        UserName = "guest@test.com",
                        NormalizedUserName = "GUEST@TEST.COM",
                        Email = "guest@test.com",
                        NormalizedEmail = "GUEST@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "Guest123!"),
                        SecurityStamp = Guid.NewGuid().ToString(),
                        FirstName = "Guest",
                        LastName = "User",
                        PfpURL = "/uploads/default_pfp.jpg"
                    }
                );

                context.SaveChanges(); 

                
                var adminUser = context.Users.Single(u => u.NormalizedEmail == "ADMIN@TEST.COM");
                var editorUser = context.Users.Single(u => u.NormalizedEmail == "EDITOR@TEST.COM");
                var normalUser = context.Users.Single(u => u.NormalizedEmail == "USER@TEST.COM");
                var adminRole = context.Roles.Single(r => r.NormalizedName == "ADMIN");
                var editorRole = context.Roles.Single(r => r.NormalizedName == "EDITOR");
                var userRole = context.Roles.Single(r => r.NormalizedName == "USER");

                context.UserRoles.AddRange(
                    new IdentityUserRole<string> { UserId = adminUser.Id, RoleId = adminRole.Id },
                    new IdentityUserRole<string> { UserId = editorUser.Id, RoleId = editorRole.Id },
                    new IdentityUserRole<string> { UserId = normalUser.Id, RoleId = userRole.Id }
                );

                context.SaveChanges();
            }
        }
    }
}