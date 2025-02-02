using System.ComponentModel.DataAnnotations;

namespace FinalProjApi.Models
{
    public class InvalidatedToken
    {
        [Key]
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime InvalidatedAt { get; set; }
    }
}
