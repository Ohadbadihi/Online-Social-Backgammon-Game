using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FinalProjApi.Models
{
    [Index(nameof(UserId), nameof(ChatRoomId), nameof(TimeSent))]
    public class Message
    {
        [Key]   
        public int MessageId { get; set; }

        [StringLength(500, MinimumLength =1)]
        public string TheMessage { get; set; } = string.Empty;

        public DateTime TimeSent { get; set; } = DateTime.UtcNow;   

        public int UserId { get; set; }
        public User Sender { get; set; } 
        
        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; }

    }
}
