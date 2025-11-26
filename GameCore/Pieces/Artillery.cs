namespace GameCore
{
    public class Artillery : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public Artillery(Player color) : base(PieceType.Artillery, color)
        {
        }
    }
}