namespace FinalProjApi.Dto
{
    public class NotificationDto
    {

        public string Type { get; set; }  // Type of notification ("FriendRequest" or "GameInvite")
        public string FromUsername { get; set; }  // Sender's username
        public DateTime SentAt { get; set; }  // Time the notification was sent
        public DateTime? ExpiryTime { get; set; }  // Only for game invites, can be null for friend requests
    }
}
