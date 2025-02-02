namespace FinalProjApi.Dto
{
    public class GameInviteDto
    {
        public string SenderUsername { get; set; } = string.Empty;
        public DateTime InviteSentAt { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
