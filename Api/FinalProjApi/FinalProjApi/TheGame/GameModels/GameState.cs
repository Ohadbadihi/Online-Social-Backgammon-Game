using FinalProjApi.Game;
using FinalProjApi.Game.GameModels;
using FinalProjApi.TheGame.GameModels.GameStatusEnum;

namespace FinalProjApi.TheGame.GameModels
{
    public class GameState
    {
        public string GameId { get; set; }
        public Board Board { get; set; } = new Board();
        public string Player1 { get; set; } 
        public string Player2 { get; set; }
        public PieceColor Player1Color { get; set; }
        public PieceColor Player2Color { get; set; }
        public Dice DiceRoll { get; set; }
        public PieceColor CurrentTurn { get; set; }
        public PlayerTimer WhiteTimer { get; set; } 
        public PlayerTimer BlackTimer { get; set; }
        public GameStatus Status { get; set; }
        public bool IsGameOver { get; set; }
        public string Winner { get; set; }
    }
}
