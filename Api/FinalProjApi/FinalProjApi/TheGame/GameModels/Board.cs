namespace FinalProjApi.Game.GameModels
{
    public class Board
    {
        public Stack<Piece>[] Positions { get; set; } = new Stack<Piece>[24]; // Backgammon board with 24 positions
        public List<Piece> WhiteOut { get; set; } = new List<Piece>(); // Pieces in white's out area
        public List<Piece> BlackOut { get; set; } = new List<Piece>(); // Pieces in black's out area
        public List<Piece> WhiteHome { get; set; } = new List<Piece>(); // White pieces borne off
        public List<Piece> BlackHome { get; set; } = new List<Piece>(); // Black pieces borne off

        public Board()
        {
            for (int i = 0; i < 24; i++)
            {
                Positions[i] = new Stack<Piece>();
            }
        }

    }
}
