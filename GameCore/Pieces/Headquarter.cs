namespace GameCore
{
    public class Headquarter : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public Headquarter(Player color) : base(PieceType.Headquarter, color)
        {
        }
    }
}