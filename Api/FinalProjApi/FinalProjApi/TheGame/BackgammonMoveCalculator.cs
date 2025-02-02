
using FinalProjApi.Game.GameModels;
using FinalProjApi.TheGame.GameModels;

namespace FinalProjApi.TheGame
{
    public static class BackgammonMoveCalculator
    {
        private const int BOARD_SIZE = 24;
        private const int BAR_POSITION = -1;
        private const int BEAR_OFF_POSITION = 24;

        public static List<(int From, int To)> CalculatePossibleMoves(GameState gameState, int[] diceRolls)
        {
            var possibleMoves = new List<(int From, int To)>();
            var board = gameState.Board;
            var currentColor = gameState.CurrentTurn;

            // Cache distinct dice rolls to avoid multiple Distinct() calls
            var distinctDiceRolls = diceRolls.Distinct().ToArray();

            // Cache result to avoid multiple checks
            bool hasPiecesOnBar = HasPiecesOnBar(board, currentColor);

            // If there are pieces on the bar, handle moves from the bar first
            if (hasPiecesOnBar)
            {
                return CalculateMovesFromBar(board, currentColor, distinctDiceRolls);
            }

            // Check if bearing off is possible
            bool canBearOff = CanBearOff(board, currentColor);

            // Get all positions with current player's pieces
            var positions = GetPositionsWithPlayerPieces(board, currentColor);

            // Calculate moves for individual dice rolls and combined dice rolls
            foreach (var fromPos in positions)
            {
                // Check moves for individual dice rolls
                foreach (var diceValue in distinctDiceRolls)
                {
                    var toPos = CalculateDestination(fromPos, diceValue, currentColor);
                    if (IsWithinBounds(toPos) && IsValidMove(board, fromPos, toPos, currentColor))
                    {
                        possibleMoves.Add((fromPos, toPos));
                    }
                }

                // Check moves using both dice rolls combined
                if (distinctDiceRolls.Length == 2)
                {
                    var combinedValue = distinctDiceRolls[0] + distinctDiceRolls[1];
                    var combinedToPos = CalculateDestination(fromPos, combinedValue, currentColor);
                    if (IsWithinBounds(combinedToPos) && IsValidMove(board, fromPos, combinedToPos, currentColor))
                    {
                        possibleMoves.Add((fromPos, combinedToPos));
                    }
                }
            }

            return possibleMoves;
        }

        private static List<(int From, int To)> CalculateMovesFromBar(Board board, PieceColor color, int[] diceRolls)
        {
            var moves = new List<(int From, int To)>();
            var entryPoints = diceRolls.Select(d => CalculateDestination(BAR_POSITION, d, color))
                                      .Where(pos => IsWithinBounds(pos));

            foreach (var entryPoint in entryPoints)
            {
                if (CanEnterFromBar(board, entryPoint, color))
                {
                    moves.Add((BAR_POSITION, entryPoint));
                }
            }

            return moves;
        }

        private static bool HasPiecesOnBar(Board board, PieceColor color)
        {
            return color == PieceColor.White
                ? board.WhiteOut.Any()
                : board.BlackOut.Any();
        }

        private static bool CanBearOff(Board board, PieceColor color)
        {
            var homeBoard = GetHomeBoard(color);

            // Early exit if pieces outside the home board are found
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                if (!homeBoard.Contains(i) && board.Positions[i].Any(p => p.Color == color))
                {
                    return false; // Exit early if a piece is found outside the home board
                }
            }

            return !HasPiecesOnBar(board, color);
        }

        private static int[] GetHomeBoard(PieceColor color)
        {
            return color == PieceColor.White
                ? Enumerable.Range(0, 6).ToArray()
                : Enumerable.Range(18, 6).ToArray();
        }

        private static bool IsValidBearOff(Board board, PieceColor color, int fromPos, int diceValue)
        {
            var homeBoard = GetHomeBoard(color);
            if (!homeBoard.Contains(fromPos)) return false;

            var targetPos = CalculateDestination(fromPos, diceValue, color);
            if (targetPos == BEAR_OFF_POSITION) return true;

            if (color == PieceColor.White && fromPos < diceValue) return true;
            if (color == PieceColor.Black && (BOARD_SIZE - fromPos - 1) < diceValue) return true;

            return false;
        }

        private static bool IsValidMove(Board board, int fromPos, int toPos, PieceColor color)
        {
            var destinationStack = board.Positions[toPos];

            // Early exit if destination is empty or has same color pieces
            if (destinationStack.Count == 0 || destinationStack.Peek().Color == color)
                return true;

            // Can hit opponent's piece only if there is exactly one piece
            return destinationStack.Count == 1 && destinationStack.Peek().Color != color;
        }

        private static int CalculateDestination(int fromPos, int diceValue, PieceColor color)
        {
            if (fromPos == BAR_POSITION)
            {
                return color == PieceColor.White
                    ? diceValue - 1
                    : BOARD_SIZE - diceValue;
            }

            var rawPos = color == PieceColor.White
                ? fromPos - diceValue
                : fromPos + diceValue;

            if (color == PieceColor.White && rawPos < 0) return BEAR_OFF_POSITION;
            if (color == PieceColor.Black && rawPos >= BOARD_SIZE) return BEAR_OFF_POSITION;

            return rawPos;
        }

        private static List<int> GetPositionsWithPlayerPieces(Board board, PieceColor color)
        {
            var positions = new List<int>();
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                if (board.Positions[i].Any() && board.Positions[i].Peek().Color == color)
                {
                    positions.Add(i);
                }
            }
            return positions;
        }

        private static bool CanEnterFromBar(Board board, int position, PieceColor color)
        {
            return !board.Positions[position].Any() ||
                   board.Positions[position].Peek().Color == color ||
                   board.Positions[position].Count == 1;
        }

        private static bool IsWithinBounds(int position)
        {
            return position >= 0 && position < BOARD_SIZE;
        }
    }
}
