using System.ComponentModel.DataAnnotations;

namespace FinalProjApi.Models
{
    public class UserToken
    {
        [Key]
        public int Id { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string SessionId { get; set; }
    }

}
