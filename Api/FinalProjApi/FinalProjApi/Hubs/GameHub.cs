using FinalProjApi.Game.GameModels;
using FinalProjApi.Service.Game;
using FinalProjApi.TheGame.GameDto;
using FinalProjApi.TheGame.GameModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;


namespace FinalProjApi.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();
        private static readonly ConcurrentDictionary<string, string> _userGames = new();
        private readonly IGameService _gameService;
        private readonly ILogger<GameHub> _logger;


        public GameHub(IGameService gameService, ILogger<GameHub> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }


        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            if (username != null)
            {
                _userConnections[username] = Context.ConnectionId;

            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.Identity?.Name;
            if (username != null)
            {
                _userConnections.TryRemove(username, out _);
                if (_userGames.TryGetValue(username, out var gameId))
                {
                    // Handle disconnection
                    await HandleDisconnection(username, gameId);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }


        public async Task CreateGame(string gameId, string opponentName)
        {

            var username = Context.User?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                await Clients.Caller.SendAsync("GameError", "Invalid creator.");
                return;
            }

            var gameState = _gameService.CreateGame(username, opponentName, gameId);

            if (gameState == null)
            {
                await Clients.Caller.SendAsync("GameError", "Failed to create game");
                return;
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            _userGames[username] = gameId;
            _userConnections[username] = Context.ConnectionId;
            await Clients.Caller.SendAsync("GameCreated", gameState.ToDto());
        }


        public async Task JoinGame(string gameId)
        {
            var username = Context.User?.Identity?.Name;
            Console.WriteLine("JoinGame => username : " + username);
            if (string.IsNullOrEmpty(username)) return;

            var (success, gameState) = _gameService.JoinGame(gameId, username);

            if (!success || gameState == null)
            {
                await Clients.Caller.SendAsync("GameError", "Failed to join game");
                return;
            }

            _userConnections[username] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            _userGames[username] = gameId;

            if (_userGames.ContainsKey(gameState.Player1) && _userGames.ContainsKey(gameState.Player2))
            {
                var updatedGameState = _gameService.GetGameState(gameId);

                if (updatedGameState != null)
                {
                   
                    await Clients.Group(gameId).SendAsync("GameStarted", updatedGameState.ToDto());
                }

            }
        }

        public async Task RollDice(string gameId)
        {
            try
            {
                var gameState = _gameService.GetGameState(gameId);
                if (gameState == null)
                {
                    await Clients.Caller.SendAsync("GameError", "Game not found.");
                    return;
                }

                // Validate it's the player's turn
                if (!IsPlayersTurn(gameState, Context.User?.Identity?.Name))
                {
                    await Clients.Caller.SendAsync("GameError", "It's not your turn.");
                    return;
                }

                var dice = _gameService.RollDice(gameId);


                await Clients.Group(gameId).SendAsync("DiceRolled", dice.Rolls);

                var possibleMoves = _gameService.GetPossibleMoves(gameId);

                await Clients.Caller.SendAsync("PossibleMoves", possibleMoves);


                await Clients.Group(gameId).SendAsync("UpdateBoard", gameState.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RollDice method for gameId: {GameId}, user: {Username}.", gameId, Context.User?.Identity?.Name);

                await Clients.Caller.SendAsync("GameError", "An error occurred while rolling the dice. Please try again.");
            }
        }

        public async Task MakeMove(string gameId, int fromIndex, int toIndex)
        {
            try
            {
                var gameState = _gameService.GetGameState(gameId);
                if (gameState == null)
                {
                    await Clients.Caller.SendAsync("GameError", "Game not found.");
                    return;
                }

            
                if (!IsPlayersTurn(gameState, Context.User?.Identity?.Name))
                {
                    await Clients.Caller.SendAsync("GameError", "Not your turn");
                    return;
                }

                var moveValid = _gameService.MakeMove(gameId, fromIndex, toIndex);

                if (moveValid)
                {
                    // Check for win condition after the move
                    if (_gameService.CheckWinCondition(gameState))
                    {
                        gameState.IsGameOver = true;
                        var winnerName = Context.User?.Identity?.Name;
                        if (string.IsNullOrEmpty(winnerName) || string.IsNullOrWhiteSpace(winnerName)) return;
                        await _gameService.EndGame(gameState, winnerName);

                        await Clients.Group(gameId).SendAsync("GameOver", new { Winner = winnerName });

                        // Clean up game tracking
                        _userGames.TryRemove(gameState.Player1, out _);
                        _userGames.TryRemove(gameState.Player2, out _);
                        return;
                    }

                    await Clients.Group(gameId).SendAsync("UpdateBoard", gameState.ToDto());

                    var possibleMoves = _gameService.GetPossibleMoves(gameId);
                    await Clients.Caller.SendAsync("PossibleMoves", possibleMoves);
                }
                else
                {
                    await Clients.Caller.SendAsync("InvalidMove");
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("GameError", ex.Message);
            }
        }

        public async Task GetPossibleMoves(string gameId)
        {
            try
            {
                var gameState = _gameService.GetGameState(gameId);
                if (gameState == null)
                {
                    await Clients.Caller.SendAsync("GameError", "Game not found.");
                    return;
                }

                var possibleMoves = _gameService.GetPossibleMoves(gameId);
                await Clients.Caller.SendAsync("PossibleMoves", possibleMoves);
            }
            catch (Exception ex)
            {

                await Clients.Caller.SendAsync("GameError", ex.Message);
            }
        }

        public async Task EndTurn(string gameId)
        {
            try
            {
                var gameState = _gameService.GetGameState(gameId);
                if (gameState == null)
                {
                    await Clients.Caller.SendAsync("GameError", "Game not found.");
                    return;
                }
                var currentPlayer = Context.User?.Identity?.Name;

                if (currentPlayer != null && !IsPlayersTurn(gameState, currentPlayer))
                {
                    await Clients.Caller.SendAsync("GameError", "Not your turn");
                    return;
                }

                _gameService.EndTurn(gameId);

                var nextPlayerName = gameState.CurrentTurn == PieceColor.White ? gameState.Player1 : gameState.Player2;

                await Clients.Group(gameId).SendAsync("TurnChanged", nextPlayerName);

                await RollDice(gameId);

            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("GameError", ex.Message);

            }
        }

        public async Task TimerEnded(string gameId)
        {
            try
            {
                var currentPlayer = Context.User?.Identity?.Name;
                var gameState = _gameService.GetGameState(gameId);
                if (gameState == null || string.IsNullOrEmpty(currentPlayer))
                {
                    await Clients.Caller.SendAsync("GameError", "Game not found.");
                    return;
                }

                var winner = _gameService.TimeOut(gameId, currentPlayer);
                await _gameService.EndGame(gameState, winner);
                await Clients.Group(gameId).SendAsync("GameOver", new { Winner = winner, GameState = gameState });

                // Clean up game tracking
                _userGames.TryRemove(gameState.Player1, out _);
                _userGames.TryRemove(gameState.Player2, out _);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("GameError", ex.Message);
            }
        }

        public async Task ReconnectToGame(string gameId)
        {
            var username = Context.User?.Identity?.Name;
            if (username == null) return;

            var gameState = _gameService.GetGameState(gameId);
            if (gameState != null && (gameState.Player1 == username || gameState.Player2 == username))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
                await Clients.Caller.SendAsync("GameReconnected", gameState);
            }
        }

        private static bool IsPlayersTurn(GameState gameState, string? player)
        {
            PieceColor playerColor = gameState.Player1 == player ? gameState.Player1Color : gameState.Player2Color;
            return gameState.CurrentTurn == playerColor;
        }

        private async Task HandleDisconnection(string username, string gameId)
        {
            try
            {
                var gameState = _gameService.GetGameState(gameId);
                if (gameState != null && !gameState.IsGameOver)
                {
                    var winner = username == gameState.Player1 ? gameState.Player2 : gameState.Player1;
                    await _gameService.EndGame(gameState, winner);
                    await Clients.Group(gameId).SendAsync("GameOver", new
                    {
                        Winner = winner,
                        Reason = "Opponent disconnected"
                    });

                    // Clean up game tracking
                    _userGames.TryRemove(gameState.Player1, out _);
                    _userGames.TryRemove(gameState.Player2, out _);
                }
            }
            catch (Exception ex)
            {
                await Clients.Group(gameId).SendAsync("GameError", ex.Message);
            }
        }

    }
}

