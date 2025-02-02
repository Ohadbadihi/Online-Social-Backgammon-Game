namespace FinalProjApi.Models
{
    public class ChatRoomUser
    {
        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime LastReadMessageAt { get; set; } = DateTime.UtcNow;
    }
}
