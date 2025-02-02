using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProjApi.Models
{
    public class Friendship
    {
        [Key]
        public int Id { get; set; } 

        public int User1Id { get; set; }
        [ForeignKey("User1Id")]
        public User User1 { get; set; }

        public int User2Id { get; set; }
        [ForeignKey("User2Id")]
        public User User2 { get; set; }

    }
}
