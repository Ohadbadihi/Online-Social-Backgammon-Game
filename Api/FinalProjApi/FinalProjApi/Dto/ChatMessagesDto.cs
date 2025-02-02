namespace FinalProjApi.Dto
{
    public class ChatMessagesDto
    {
        public string SenderUsername { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TimeSent { get; set; }
    }
}
