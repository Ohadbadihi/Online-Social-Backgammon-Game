using FinalProjApi.Game;
using FinalProjApi.Game.GameModels;
using FinalProjApi.Service.UserLogic;
using FinalProjApi.TheGame;
using FinalProjApi.TheGame.GameModels;
using FinalProjApi.TheGame.GameModels.GameStatusEnum;
using System.Collections.Concurrent;


namespace FinalProjApi.Service.Game
{
    public class GameService : IGameService
    {
        private readonly ConcurrentDictionary<string, GameState> _games = new();
        private readonly ConcurrentDictionary<string, string> _playerGameMapping = new();
        private readonly MoveValidation moveValidation = MoveValidation.Instance;
        private readonly IServiceProvider _serviceProvider;
        private readonly static Random random = new Random();

        public GameService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }



        public bool IsPlayerInGame(string playerId)
        {
            return _playerGameMapping.ContainsKey(playerId);
        }

        public string? GetPlayerGame(string playerId)
        {
            return _playerGameMapping.TryGetValue(playerId, out var gameId) ? gameId : null;
        }

        public GameState? CreateGame(string creator, string opponent, string gameId)
        {
            if (_games.ContainsKey(gameId))
            {
                return null; // Game already exists
            }

            if (IsPlayerInGame(creator) || IsPlayerInGame(opponent))
            {
                return null; // One of the players is already in a game
            }

            var gameState = new GameState
            {
                GameId = gameId,
                Player1 = creator,
                Player2 = opponent,
                Status = GameStatus.WaitingForOpponent,
                Board = InitializeBoard(),
                DiceRoll = new Dice(),
                WhiteTimer = new PlayerTimer(TimeSpan.FromMinutes(5)),
                BlackTimer = new PlayerTimer(TimeSpan.FromMinutes(5)),
                IsGameOver = false
            };

            if (_games.TryAdd(gameId, gameState))
            {
                return gameState;
            }

            return null;
        }

        public (bool Success, GameState? GameState) JoinGame(string gameId, string joiningPlayer)
        {
            if (!_games.TryGetValue(gameId, out var gameState))
            {
                return (false, null);
            }

            if (gameState.Status != GameStatus.WaitingForOpponent && gameState.Status != GameStatus.InProgress)
            {
                return (false, null);
            }

            if (gameState.Player1 != joiningPlayer && gameState.Player2 != joiningPlayer)
            {
                Console.WriteLine("Fail here JoinGame In GameService => if gameState.Player2 != joiningPlayer ");
                return (false, null);
            }

            if (_playerGameMapping.TryGetValue(joiningPlayer, out var existingGameId))
            {
                if (existingGameId != gameId)
                {
                    return (false, null); // Player is in a different game
                }
            }
            else
            {
                _playerGameMapping.TryAdd(joiningPlayer, gameId);
            }

            // Initialize game for players directly
            if (gameState.Status == GameStatus.WaitingForOpponent )
            {
                InitializeGameForPlayers(gameState);
                gameState.Status = GameStatus.InProgress;
            }

            return (true, gameState);
        }

        private static void InitializeGameForPlayers(GameState gameState)
        {
            var isPlayer1White = random.Next(2) == 0;

            gameState.Player1Color = isPlayer1White ? PieceColor.White : PieceColor.Black;
            gameState.Player2Color = isPlayer1White ? PieceColor.Black : PieceColor.White;
            gameState.CurrentTurn = PieceColor.White;
            gameState.Status = GameStatus.InProgress;
        }


        public Dice RollDice(string gameId)
        {
            if (_games.TryGetValue(gameId, out var game) && game != null )
            {
                game.DiceRoll.RollDice();

                if (game.CurrentTurn == PieceColor.White)
                {
                    game.WhiteTimer.Start();
                }
                else
                {
                    game.BlackTimer.Start();
                }

                return game.DiceRoll;

            }
            throw new KeyNotFoundException("Game not found");
        }


        public bool MakeMove(string gameId, int fromIndex, int toIndex)
        {
            if (!_games.TryGetValue(gameId, out var game))
            {
                throw new KeyNotFoundException("Game not found");
            }

            lock (game)
            {
                var moveValid = moveValidation.ValidateMove(game.Board, fromIndex, toIndex, game.DiceRoll.Rolls, game.CurrentTurn);

                if (!moveValid)
                {
                    return false;
                }

                if (IsHitPossible(game.Board, toIndex, game.CurrentTurn))
                {
                    HitPiece(game.Board, toIndex);
                }

                if (IsBearingOff(toIndex, game.CurrentTurn))
                {
                    BearOffChecker(game.Board, fromIndex, game.CurrentTurn);
                }
                else
                {
                    // Execute move
                    ExecuteMove(game.Board, fromIndex, toIndex, game.CurrentTurn);
                }

                if (CheckWinCondition(game)) // Check for win condition after the move
                {
                    game.IsGameOver = true;
                    game.Winner = game.CurrentTurn.ToString();
                    _ = EndGame(game, game.Winner);
                }

                UpdateDiceRolls(game, Math.Abs(toIndex - fromIndex));
                return true;
            }
        
        }

        public List<(int From, int To)> GetPossibleMoves(string gameId)
        {
            if (_games.TryGetValue(gameId, out var gameState))
            {
                var possibleMoves = BackgammonMoveCalculator.CalculatePossibleMoves(gameState, gameState.DiceRoll.Rolls);
                return possibleMoves;
            }
            return new List<(int, int)>();
        }



        public void EndTurn(string gameId)
        {
            if (_games.TryGetValue(gameId, out var game))
            {
                if (game.CurrentTurn == PieceColor.White)
                {
                    game.WhiteTimer.Stop();
                }
                else
                {
                    game.BlackTimer.Stop();
                }

                game.CurrentTurn = game.CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            }
            else
            {
                throw new KeyNotFoundException("Game not found");
            }
        }


        public async Task EndGame(GameState gameState, string winner)
        {
            try
            {
                // Create a scope to resolve the scoped UserService
                using (var scope = _serviceProvider.CreateScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                    var winnerUser = await userService.GetUserByName(winner);
                    var loserUser = await userService.GetUserByName(
                        gameState.Player1 == winner ? gameState.Player2 : gameState.Player1);

                    if (winnerUser != null && loserUser != null)
                    {
                        winnerUser.Wins++;
                        loserUser.Loses++;
                        await userService.UpdateUserWinsOrLoses(winnerUser);
                        await userService.UpdateUserWinsOrLoses(loserUser);
                    }

                    // Clean up game resources
                    _playerGameMapping.TryRemove(gameState.Player1, out _);
                    _playerGameMapping.TryRemove(gameState.Player2, out _);
                    _games.TryRemove(gameState.GameId, out _);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        private static Board InitializeBoard()
        {
            var board = new Board();

            // White pieces (moving counterclockwise from position 23)
            SetInitialPosition(board, 23, 2, PieceColor.White); // Position 24 (2 pieces)
            SetInitialPosition(board, 12, 5, PieceColor.White); // Position 13 (5 pieces)
            SetInitialPosition(board, 7, 3, PieceColor.White);  // Position 8 (3 pieces)
            SetInitialPosition(board, 5, 5, PieceColor.White);  // Position 6 (5 pieces)

            // Black pieces (moving clockwise from position 0)
            SetInitialPosition(board, 0, 2, PieceColor.Black);  // Position 1 (2 pieces)
            SetInitialPosition(board, 11, 5, PieceColor.Black); // Position 12 (5 pieces)
            SetInitialPosition(board, 16, 3, PieceColor.Black); // Position 17 (3 pieces)
            SetInitialPosition(board, 18, 5, PieceColor.Black); // Position 19 (5 pieces)

            return board;
        }

        private static void ExecuteMove(Board board, int fromIndex, int toIndex, PieceColor turn)
        {
            var piece = board.Positions[fromIndex].Pop();

            if (IsHitPossible(board, toIndex, turn))
            {
                HitPiece(board, toIndex);
            }

            board.Positions[toIndex].Push(piece);
        }

        private static void UpdateDiceRolls(GameState game, int usedValue)
        {
            game.DiceRoll.RemoveRoll(usedValue); // Update rolls using the provided method
        }

        private static void SetInitialPosition(Board board, int position, int count, PieceColor color)
        {
            for (int i = 0; i < count; i++)
            {
                board.Positions[position].Push(new Piece { Color = color });
            }
        }

        private static bool IsHitPossible(Board board, int toIndex, PieceColor color)
        {
            var toStack = board.Positions[toIndex];
            return toStack.Count == 1 && toStack.Peek().Color != color;
        }

        private static void BearOffChecker(Board board, int fromIndex, PieceColor turn)
        {
            var piece = board.Positions[fromIndex].Pop();
            if (turn == PieceColor.White)
            {
                board.WhiteHome.Add(piece);
            }
            else
            {
                board.BlackHome.Add(piece);
            }
        }


        // Check if bearing off is possible
        private static bool IsBearingOff(int toIndex, PieceColor turn)
        {
            return (turn == PieceColor.White && toIndex == 24) || (turn == PieceColor.Black && toIndex == -1);
        }


        private static void HitPiece(Board board, int toIndex)
        {
            var piece = board.Positions[toIndex].Pop();
            // Move the piece to the bar area depending on its color
            if (piece.Color == PieceColor.White)
            {
                board.WhiteOut.Add(piece);
            }
            else
            {
                board.BlackOut.Add(piece);
            }
        }

        public string TimeOut(string gameId, string player)
        {
            if (_games.TryGetValue(gameId, out var game))
            {
                game.IsGameOver = true;
                var winner = game.Player1 == player ? game.Player2 : game.Player1;
                game.Winner = winner;
                return winner;
            }
            throw new KeyNotFoundException("Game not found");
        }

        public bool CheckWinCondition(GameState gameState)
        {
            return gameState.Board.WhiteHome.Count == 15 || gameState.Board.BlackHome.Count == 15;
        }

        public GameState? GetGameState(string gameId)
        {

            if (_games.TryGetValue(gameId, out var gameState))
            {
                Console.WriteLine("Game found.");
                return gameState;
            }
            return null;
        }


    }

}

