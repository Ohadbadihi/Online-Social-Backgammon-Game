using System.ComponentModel.DataAnnotations;

namespace FinalProjApi.Models
{
    public class ChatRoom
    {
        [Key]
        public int RoomId { get; set; }

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

        public virtual ICollection<ChatRoomUser> ChatRoomUsers { get; set; } = new List<ChatRoomUser>();
    }
}
