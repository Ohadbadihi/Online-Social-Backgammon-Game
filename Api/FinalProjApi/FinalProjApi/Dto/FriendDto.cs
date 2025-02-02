namespace FinalProjApi.Dto
{
    public class FriendDto
    {
        public string Username { get; set; } = string.Empty;

        public bool IsOnline { get; set; } 


        public uint Wins { get; set; }

        public uint Loses { get; set; }

        public bool HasUnreadMessages { get; set; }
    }
}
