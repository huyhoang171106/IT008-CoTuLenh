namespace GameCore
{
    public class Militia : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public Militia(Player color) : base(PieceType.Militia, color)
        {
        }
    }
}