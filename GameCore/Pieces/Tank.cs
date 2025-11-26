namespace GameCore
{
    public class Tank : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public Tank(Player color) : base(PieceType.Tank, color)
        {
        }
    }
}