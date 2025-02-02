using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProjApi.Models
{
    [Index(nameof(Username), IsUnique = true)]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, StringLength(15, MinimumLength = 2)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        public uint Wins { get; set; }

        public uint Loses { get; set; }

        public bool IsOnline { get; set; } = false;

        public virtual ICollection<Friendship> Friendships { get; set; } = new List<Friendship>();
        public virtual ICollection<FriendRequest> FriendRequests { get; set; } = new List<FriendRequest>();
        public virtual ICollection<GameInvite> GameInvites { get; set; } = new List<GameInvite>();

        public virtual ICollection<ChatRoomUser> ChatRoomUsers { get; set; } = new List<ChatRoomUser>();

    }
}
