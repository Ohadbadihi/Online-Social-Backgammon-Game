using FinalProjApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProjApi.Data
{
    public class DataBaseContext : DbContext
    {
        public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<GameInvite> GameInvites { get; set; }
        public DbSet<ChatRoomUser> ChatRoomUsers { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
        public DbSet<InvalidatedToken> InvalidatedTokens { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Unique index on Username to avoid duplicate users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            // Configure many-to-many relationship for friends
            modelBuilder.Entity<Friendship>()
               .HasOne(f => f.User1)
               .WithMany(u => u.Friendships)
               .HasForeignKey(f => f.User1Id)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.User2)
                .WithMany()
                .HasForeignKey(f => f.User2Id)
                .OnDelete(DeleteBehavior.Restrict);


            // Define relationship for FriendRequest
            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany()
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany()
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Define relationship for GameInvite
            modelBuilder.Entity<GameInvite>()
                .HasOne(gi => gi.Sender)
                .WithMany()
                .HasForeignKey(gi => gi.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GameInvite>()
                .HasOne(gi => gi.Receiver)
                .WithMany()
                .HasForeignKey(gi => gi.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-many ChatRoom and User
            modelBuilder.Entity<ChatRoomUser>()
                .HasKey(cru => new { cru.ChatRoomId, cru.UserId });

            modelBuilder.Entity<ChatRoomUser>()
                .HasOne(cru => cru.ChatRoom)
                .WithMany(cr => cr.ChatRoomUsers)
                .HasForeignKey(cru => cru.ChatRoomId);

            modelBuilder.Entity<ChatRoomUser>()
                .HasOne(cru => cru.User)
                .WithMany(u => u.ChatRoomUsers)
                .HasForeignKey(cru => cru.UserId);

            // Messages: ensure messages are unique per user and chat room
            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.UserId, m.ChatRoomId, m.TimeSent })
                .IsUnique();

            modelBuilder.Entity<Message>()
               .HasIndex(m => new { m.UserId, m.ChatRoomId, m.TimeSent });


            modelBuilder.Entity<UserToken>()
               .HasIndex(ut => new { ut.Token, ut.Username }) // Ensure token uniqueness per user
               .IsUnique();


            base.OnModelCreating(modelBuilder);
        }
    }
}


