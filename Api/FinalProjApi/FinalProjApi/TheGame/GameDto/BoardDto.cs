namespace FinalProjApi.TheGame.GameDto
{
    public class BoardDTO
    {
        public Stack<PieceDto>[] Positions { get; set; }
        public int WhiteOut { get; set; }
        public int BlackOut { get; set; }
        public int WhiteHome { get; set; }
        public int BlackHome { get; set; }
    }
}
