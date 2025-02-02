
using FinalProjApi.Game.GameModels;
using FinalProjApi.TheGame.GameModels;

namespace FinalProjApi.Service.Game
{
    public interface IGameService
    {
        //GameState StartNewGame(string player1, string player2, string gameId);
        GameState? CreateGame(string creator, string opponent, string gameId);
        (bool Success, GameState? GameState) JoinGame(string gameId, string joiningPlayer);
        Dice RollDice(string gameId);
        bool MakeMove(string gameId, int fromIndex, int toIndex);
        List<(int From, int To)> GetPossibleMoves(string gameId);
        void EndTurn(string gameId);
        bool CheckWinCondition(GameState gameState);
        Task EndGame(GameState gameState, string winner);
        GameState? GetGameState(string gameId);
        string TimeOut(string gameId, string player);
        bool IsPlayerInGame(string playerId);
        string? GetPlayerGame(string playerId);
    }
}
