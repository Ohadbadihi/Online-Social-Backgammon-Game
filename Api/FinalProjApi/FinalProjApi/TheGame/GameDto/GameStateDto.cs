using FinalProjApi.Game.GameModels;

namespace FinalProjApi.TheGame.GameDto
{
    public class GameStateDto
    {
        public string GameId { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public string Player1Color { get; set; }
        public string Player2Color { get; set; }
        public string CurrentTurn { get; set; }
        public BoardDTO Board { get; set; }
        public int[] DiceRoll { get; set; }
        public string Winner { get; set; }
        public string Status { get; set; }
        public bool IsGameOver { get; set; }
        public int WhiteTimeRemaining { get; set; }
        public int BlackTimeRemaining { get; set; }
    }
}
