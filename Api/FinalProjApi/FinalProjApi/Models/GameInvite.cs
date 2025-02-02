using System.ComponentModel.DataAnnotations;

namespace FinalProjApi.Models
{
    public class GameInvite
    {
        [Key]
        public int InviteId { get; set; }

        public int SenderId { get; set; }
        public User Sender { get; set; }

        public int ReceiverId { get; set; }
        public User Receiver { get; set; }

        public DateTime InviteSentAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiryTime { get; set; }
    }
}
