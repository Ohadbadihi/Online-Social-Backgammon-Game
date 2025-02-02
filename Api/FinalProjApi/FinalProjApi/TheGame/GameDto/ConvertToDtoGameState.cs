using FinalProjApi.TheGame.GameModels;

namespace FinalProjApi.TheGame.GameDto
{
    public static class ConvertToDtoGameState
    {
        public static GameStateDto ToDto(this GameState gameState)
        {
            return new GameStateDto
            {
                GameId = gameState.GameId,
                Player1 = gameState.Player1,
                Player2 = gameState.Player2,
                Player1Color = gameState.Player1Color.ToString(),
                Player2Color = gameState.Player2Color.ToString(),
                CurrentTurn = gameState.CurrentTurn.ToString(),
                Board = new BoardDTO
                {
                    Positions = gameState.Board.Positions
                        .Select(stack =>
                            new Stack<PieceDto>(
                                stack.Select(piece => new PieceDto
                                {
                                    Color = piece.Color.ToString()
                                })
                            )
                        ).ToArray(),
                    WhiteOut = gameState.Board.WhiteOut.Count,
                    BlackOut = gameState.Board.BlackOut.Count,
                    WhiteHome = gameState.Board.WhiteHome.Count,
                    BlackHome = gameState.Board.BlackHome.Count,
                },
                DiceRoll = gameState.DiceRoll.Rolls,
                Status = gameState.Status.ToString(),
                Winner = gameState.Winner,
                IsGameOver = gameState.IsGameOver,
                WhiteTimeRemaining = (int)gameState.WhiteTimer.TimeRemaining.TotalSeconds,
                BlackTimeRemaining = (int)gameState.BlackTimer.TimeRemaining.TotalSeconds,
            };
        }
    }
}
