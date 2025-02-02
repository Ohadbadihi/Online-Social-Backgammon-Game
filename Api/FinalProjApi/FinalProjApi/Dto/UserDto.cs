namespace FinalProjApi.Dto
{
    public class UserDto
    {
        public string Username { get; set; } = string.Empty;
        public uint Wins { get; set; }
        public uint Loses { get; set; }

        public bool IsOnline { get; set; } = false;

    }
}
