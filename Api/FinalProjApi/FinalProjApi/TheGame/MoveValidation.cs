using FinalProjApi.Game.GameModels;

namespace FinalProjApi.TheGame
{
    public class MoveValidation
    {
        public static MoveValidation Instance { get; } = new MoveValidation();

        public MoveValidation() { }

        public bool ValidateMove(Board board, int fromIndex, int toIndex, int[] dice, PieceColor turn)
        {
            if (!IsMoveInBounds(fromIndex, toIndex)) return false;
            if (!IsValidSourceStack(board, fromIndex, turn)) return false;
            if (!IsValidDiceMove(fromIndex, toIndex, dice)) return false;
            if (!CanMoveToDestination(board, toIndex, turn)) return false;
            if (!IsBarClear(board, turn)) return false;
            if (IsBearingOff(toIndex, turn) && !ValidateBearOff(board, fromIndex, turn)) return false;

            return true;
        }

        private static bool IsMoveInBounds(int fromIndex, int toIndex)
        {
            return fromIndex >= 0 && fromIndex < 24 && toIndex >= 0 && toIndex < 24;
        }

        private static bool IsValidSourceStack(Board board, int fromIndex, PieceColor turn)
        {
            var fromStack = board.Positions[fromIndex];
            return fromStack.Count > 0 && fromStack.Peek().Color == turn;
        }

        private static bool IsValidDiceMove(int fromIndex, int toIndex, int[] dice)
        {
            int moveDistance = Math.Abs(toIndex - fromIndex);
            return dice.Contains(moveDistance);
        }

        private static bool CanMoveToDestination(Board board, int toIndex, PieceColor turn)
        {
            var toStack = board.Positions[toIndex];
            return toStack.Count == 0 || toStack.Peek().Color == turn || toStack.Count == 1;
        }

        private static bool IsBarClear(Board board, PieceColor turn)
        {
            var barPieces = turn == PieceColor.White ? board.WhiteOut : board.BlackOut;
            return barPieces.Count == 0;
        }

        private static bool IsBearingOff(int toIndex, PieceColor turn)
        {
            return (turn == PieceColor.White && toIndex == 24) || (turn == PieceColor.Black && toIndex == -1);
        }

        private static bool ValidateBearOff(Board board, int fromIndex, PieceColor turn)
        {
            // Ensures all pieces are in the home board before bearing off
            int startIdx = turn == PieceColor.White ? 18 : 0;
            int endIdx = turn == PieceColor.White ? 23 : 5;

            for (int i = 0; i < 24; i++)
            {
                if (i < startIdx || i > endIdx)
                {
                    foreach (var piece in board.Positions[i])
                    {
                        if (piece.Color == turn) return false; // If any piece outside the home board, bearing off is invalid
                    }
                }
            }
            return true;
        }
    }
}
