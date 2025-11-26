namespace GameCore
{
    public class Commander : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public Commander(Player color) : base(PieceType.Commander, color)
        {
        }
    }
}