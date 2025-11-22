using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace unibucGram.Models;

//la fel ca in Program.cs,cred ca e mai ok sa folosim user,avem mai multe proprietati

// asa trb bravo n-am vz pe mmoment
public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for your entities,
    // public DbSet<User> Users { get; set; } l-am inclus sus la mostenire
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<FollowRequest> FollowRequests { get; set; } // ADD THIS
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<GroupMessage> GroupMessages { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // IMPORTANT: Trebuie sa apelam base.OnModelCreating(builder) PRIMUL
        // pentru ca Identity sa configureze corect tabelele sale (AspNetUsers, AspNetRoles, etc.)
        base.OnModelCreating(builder);

        // ====================================================================
        // 1. CONFIGURAREA RELATIEI POST -> USER (Many-to-One)
        // ====================================================================
        // Ce facem: Un Post apartine unui singur User, dar un User poate avea mai multe Post-uri
        // De ce: Trebuie sa stim cine a creat postul
        builder.Entity<Post>()
            .HasOne(p => p.User)                    // Un Post are un User
            .WithMany(u => u.Posts)                 // Un User are mai multe Posts
            .HasForeignKey(p => p.UserId)           // FK este UserId din Post
            .OnDelete(DeleteBehavior.Cascade);      // Daca stergi User-ul, stergi si toate Post-urile sale
        // DeleteBehavior.Cascade = daca stergi un user, se sterg automat toate posturile lui
        // Alternativa: DeleteBehavior.Restrict = nu poti sterge user-ul daca are posturi (aruncă eroare)

        // ====================================================================
        // 2. CONFIGURAREA RELATIEI COMMENT -> USER si COMMENT -> POST (Many-to-One)
        // ====================================================================
        // Ce facem: Un Comment apartine unui User (cine a comentat) si unui Post (la ce post este comentariul)
        // De ce: Trebuie sa stim cine a comentat si la ce post
        builder.Entity<Comment>()
            .HasOne(c => c.User)                    // Un Comment are un User
            .WithMany(u => u.Comments)              // Un User are mai multe Comments
            .HasForeignKey(c => c.UserId)           // FK este UserId din Comment
            .OnDelete(DeleteBehavior.Restrict);     // Daca stergi User-ul, NU se sterg comment-urile (previne multiple cascade paths)
        // De ce Restrict in loc de Cascade?
        // SQL Server detecteaza un ciclu: User -> Post -> Comment SI User -> Comment
        // Daca User-ul este sters, Post-urile lui se sterg (Cascade), si apoi Comment-urile de pe acele Post-uri se sterg (Cascade prin Post)
        // Deci deja avem o cale de la User la Comment prin Post
        // Nu putem avea si Cascade direct de la User la Comment (ar fi doua cai)
        // Restrict = nu poti sterge user-ul daca are comentarii directe (trebuie sterse manual sau prin stergerea Post-ului)

        builder.Entity<Comment>()
            .HasOne(c => c.Post)                    // Un Comment apartine unui Post
            .WithMany(p => p.Comments)              // Un Post are mai multe Comments
            .HasForeignKey(c => c.PostId)           // FK este PostId din Comment
            .OnDelete(DeleteBehavior.Cascade);      // Daca stergi Post-ul, stergi si toate Comment-urile
                                                    // Cascade pentru Post: daca stergi un post, nu mai are sens sa existe comentariile la el

        // ====================================================================
        // 3. CONFIGURAREA RELATIEI LIKE (Many-to-Many cu cheie compusa)
        // ====================================================================
        // Ce facem: Like este o tabela "join" intre User si Post
        //           Un User poate da Like la mai multe Post-uri
        //           Un Post poate primi Like-uri de la mai multi Useri
        // De ce: Trebuie sa prevenim ca un user sa dea like de doua ori la acelasi post

        // PASUL 1: Definim cheia primara compusa (UserId, PostId)
        builder.Entity<Like>()
            .HasKey(l => new { l.UserId, l.PostId });
        // De ce cheie compusa? Pentru ca combinația (UserId, PostId) trebuie sa fie UNICA
        // Ex: User "john" nu poate da like de doua ori la Post-ul cu Id=5
        // Fara cheie compusa, ai putea avea duplicate: (john, 5) si (john, 5) din nou

        // PASUL 2: Configuram relatia Like -> User
        builder.Entity<Like>()
            .HasOne(l => l.User)                    // Un Like are un User
            .WithMany(u => u.Likes)                 // Un User are mai multe Likes
            .HasForeignKey(l => l.UserId)           // FK este UserId din Like
            .OnDelete(DeleteBehavior.Restrict);     // Daca stergi User-ul, NU se sterg like-urile (previne multiple cascade paths)
        // De ce Restrict in loc de Cascade?
        // SQL Server detecteaza un ciclu: User -> Post -> Like SI User -> Like
        // Daca User-ul este sters, Post-urile lui se sterg (Cascade), si apoi Like-urile de pe acele Post-uri se sterg (Cascade prin Post)
        // Deci deja avem o cale de la User la Like prin Post
        // Nu putem avea si Cascade direct de la User la Like (ar fi doua cai)
        // Restrict = nu poti sterge user-ul daca are like-uri directe (trebuie sterse manual sau prin stergerea Post-ului)

        // PASUL 3: Configuram relatia Like -> Post
        builder.Entity<Like>()
            .HasOne(l => l.Post)                    // Un Like apartine unui Post
            .WithMany(p => p.Likes)                 // Un Post are mai multe Likes
            .HasForeignKey(l => l.PostId)           // FK este PostId din Like
            .OnDelete(DeleteBehavior.Cascade);      // Daca stergi Post-ul, stergi si toate Like-urile
                                                    // Cascade pentru ambele: daca stergi user/post, nu mai are sens sa existe like-urile

        // ====================================================================
        // 4. CONFIGURAREA RELATIEI FOLLOW (Many-to-Many cu cheie compusa)
        // ====================================================================
        // Ce facem: Follow este o tabela "join" intre doi Useri
        //           Un User (Follower) poate urmari mai multi Useri (Followees)
        //           Un User (Followee) poate fi urmarit de mai multi Useri (Followers)
        // De ce: Trebuie sa prevenim auto-follow si follow-uri duplicate

        // PASUL 1: Definim cheia primara compusa (FollowerId, FolloweeId)
        builder.Entity<Follow>()
            .HasKey(f => new { f.FollowerId, f.FolloweeId });
        // De ce cheie compusa? Pentru ca combinația (FollowerId, FolloweeId) trebuie sa fie UNICA
        // Ex: User "john" nu poate urmari user "alice" de doua ori

        // PASUL 2: Configuram relatia Follow -> User (Follower)
        builder.Entity<Follow>()
            .HasOne(f => f.Follower)                // Un Follow are un Follower (cine urmareste)
            .WithMany(u => u.Following)             // Un User are mai multe Follow-uri unde este Follower
            .HasForeignKey(f => f.FollowerId)       // FK este FollowerId din Follow
            .OnDelete(DeleteBehavior.Cascade);      // Daca stergi User-ul, stergi si toate Follow-urile unde este Follower

        // PASUL 3: Configuram relatia Follow -> User (Followee)
        builder.Entity<Follow>()
            .HasOne(f => f.Followee)                // Un Follow are un Followee (cine este urmarit)
            .WithMany(u => u.Followers)             // Un User are mai multe Follow-uri unde este Followee
            .HasForeignKey(f => f.FolloweeId)       // FK este FolloweeId din Follow
            .OnDelete(DeleteBehavior.Restrict);     // Daca stergi User-ul, NU se sterg follow-urile (previne multiple cascade paths)
        // De ce Restrict in loc de Cascade?
        // SQL Server nu permite multiple cascade paths catre aceeasi tabela (AspNetUsers)
        // Avem deja Cascade pe FollowerId, deci FolloweeId trebuie sa fie Restrict
        // Restrict = nu poti sterge user-ul daca e urmarit de altii (arunca eroare)
        // Trebuie sa stergi manual follow-urile sau sa schimbi logica de cleanup

        // PASUL 4: Adaugam constraint pentru a preveni auto-follow
        builder.Entity<Follow>()
            .ToTable(t => t.HasCheckConstraint("CK_Follow_NoSelfFollow", "[FollowerId] <> [FolloweeId]"));
        // De ce? Un user nu ar trebui sa se poata urmari pe el insusi
        // Check constraint = o validare la nivel de baza de date
        // Daca incerci sa inserezi (john, john), baza de date va arunca o eroare

        // ====================================================================
        // 5. CONFIGURAREA RELATIEI CONVERSATION -> USER (Many-to-Many cu doua FK-uri)
        // ====================================================================
        // Ce facem: Conversation reprezinta o conversatie intre doi Useri (UserA si UserB)
        //           Un User poate fi UserA in mai multe Conversation-uri
        //           Un User poate fi UserB in mai multe Conversation-uri
        // De ce: Trebuie sa stii cu cine vorbesti (ambele parti ale conversatiei)

        // PASUL 1: Configuram relatia Conversation -> UserA
        builder.Entity<Conversation>()
            .HasOne(c => c.UserA)                   // Un Conversation are un UserA
            .WithMany(u => u.ConversationsAsUserA)  // Un User are mai multe Conversation-uri unde este UserA
            .HasForeignKey(c => c.UserAId)          // FK este UserAId din Conversation
            .OnDelete(DeleteBehavior.Cascade);      // Daca stergi User-ul, stergi si toate Conversation-urile unde este UserA

        // PASUL 2: Configuram relatia Conversation -> UserB
        builder.Entity<Conversation>()
            .HasOne(c => c.UserB)                   // Un Conversation are un UserB
            .WithMany(u => u.ConversationsAsUserB)  // Un User are mai multe Conversation-uri unde este UserB
            .HasForeignKey(c => c.UserBId)          // FK este UserBId din Conversation
            .OnDelete(DeleteBehavior.Restrict);     // Daca stergi User-ul, NU se sterg conversatiile (previne multiple cascade paths)
        // De ce Restrict in loc de Cascade?
        // SQL Server nu permite multiple cascade paths catre aceeasi tabela (AspNetUsers)
        // Avem deja Cascade pe UserAId, deci UserBId trebuie sa fie Restrict
        // Restrict = nu poti sterge user-ul daca e UserB in conversatii active (arunca eroare)
        // Trebuie sa stergi manual conversatiile sau sa schimbi logica de cleanup

        // De ce doua liste separate? Pentru ca Conversation are DOUA FK-uri catre User
        // EF Core nu poate mapa automat o singura lista la doua FK-uri diferite
        // ConversationsAsUserA = conversatiile unde user-ul este primul participant
        // ConversationsAsUserB = conversatiile unde user-ul este al doilea participant

        // PASUL 3: Adaugam constraint pentru a preveni conversatii cu sine
        builder.Entity<Conversation>()
            .ToTable(t => t.HasCheckConstraint("CK_Conversation_NoSelfConversation", "[UserAId] <> [UserBId]"));
        // De ce? Un user nu ar trebui sa poata avea conversatie cu el insusi
        // Check constraint = validare la nivel de baza de date

        // ====================================================================
        // 6. CONFIGURAREA RELATIEI MESSAGE -> CONVERSATION si MESSAGE -> USER
        // ====================================================================
        // Ce facem: Un Message apartine unei Conversation si are un Sender (User)
        //           O Conversation poate avea mai multe Message-uri
        //           Un User poate trimite mai multe Message-uri
        // De ce: Trebuie sa stim in ce conversatie este mesajul si cine l-a trimis

        // PASUL 1: Configuram relatia Message -> Conversation
        builder.Entity<Message>()
            .HasOne(m => m.Conversation)            // Un Message apartine unei Conversation
            .WithMany(c => c.Messages)              // O Conversation are mai multe Message-uri
            .HasForeignKey(m => m.ConversationId)   // FK este ConversationId din Message
            .OnDelete(DeleteBehavior.Cascade);      // Daca stergi Conversation-ul, stergi si toate Message-urile
                                                    // Cascade pentru Conversation: daca stergi conversatia, nu mai are sens sa existe mesajele

        // PASUL 2: Configuram relatia Message -> User (Sender)
        builder.Entity<Message>()
            .HasOne(m => m.Sender)                  // Un Message are un Sender
            .WithMany(u => u.Messages)              // Un User are mai multe Message-uri trimise
            .HasForeignKey(m => m.SenderId)         // FK este SenderId din Message
            .OnDelete(DeleteBehavior.Restrict);     // Daca stergi User-ul, NU stergi Message-urile
        // De ce Restrict in loc de Cascade?
        // Pentru ca in aplicatii de mesagerie, vrem sa pastram istoricul mesajelor
        // Chiar daca un user isi sterge contul, mesajele trimise ar trebui sa ramana
        // Restrict = nu poti sterge user-ul daca are mesaje (arunca eroare)
        // Daca vrei sa permiti stergerea, poti schimba la Cascade, dar atunci se pierd mesajele

        // ====================================================================
        // 7. CONFIGURAREA RELATIEI GROUP (Chat)
        // ====================================================================

        // GROUP MEMBER
        // Chiar daca avem Id (surogat), vrem ca un user sa fie in grup o singura data
        builder.Entity<GroupMember>()
            .HasIndex(gm => new { gm.GroupId, gm.UserId })
            .IsUnique();

        builder.Entity<GroupMember>()
            .HasOne(gm => gm.Group)
            .WithMany(g => g.GroupMembers)
            .HasForeignKey(gm => gm.GroupId)
            .OnDelete(DeleteBehavior.Cascade); // Stergem grupul -> stergem membrii

        builder.Entity<GroupMember>()
            .HasOne(gm => gm.User)
            .WithMany(u => u.GroupMembers) 
            .HasForeignKey(gm => gm.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Stergem userul -> NU stergem automat intrarea (evitam cicluri)

        // GROUP MESSAGE
        builder.Entity<GroupMessage>()
            .HasOne(m => m.Group)
            .WithMany(g => g.Messages)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade); // Stergem grupul -> stergem mesajele

        builder.Entity<GroupMessage>()
            .HasOne(m => m.User)
            .WithMany(u => u.GroupMessages)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Stergem userul -> pastram mesajele (sau Restrict)

        // NOTIFICATION
        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Notification>()
            .HasOne(n => n.ActorUser)
            .WithMany()
            .HasForeignKey(n => n.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FollowRequest>(entity =>
        {
            entity.HasOne(fr => fr.Requester)
                .WithMany()
                .HasForeignKey(fr => fr.RequesterId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete from this path

            
            entity.HasOne(fr => fr.Requestee)
                .WithMany()
                .HasForeignKey(fr => fr.RequesteeId)
                .OnDelete(DeleteBehavior.Cascade); // Allow cascade delete from this path
        });
    }
}
