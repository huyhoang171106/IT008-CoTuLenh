namespace GameCore
{
    public class Infantry : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public Infantry(Player color) : base(PieceType.Infantry, color)
        {
        }
    }
}