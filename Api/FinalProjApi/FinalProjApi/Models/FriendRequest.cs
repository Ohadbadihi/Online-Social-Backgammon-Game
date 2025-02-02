using System.ComponentModel.DataAnnotations;

namespace FinalProjApi.Models
{
    public class FriendRequest
    {
        [Key]
        public int Id { get; set; }

        public int SenderId { get; set; }
        public User Sender { get; set; }

        public int ReceiverId { get; set; }
        public User Receiver { get; set; }

        public DateTime RequestSentAt { get; set; } = DateTime.UtcNow;
    }
}
