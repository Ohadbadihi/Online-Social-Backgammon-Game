namespace FinalProjApi.Game.GameModels
{
    public class Player
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public bool Won { get; private set; }

        public PieceColor PlayColor { get; set; }
    }
}
