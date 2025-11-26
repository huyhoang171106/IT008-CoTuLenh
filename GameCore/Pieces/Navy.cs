namespace GameCore
{
    public class Navy : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public Navy(Player color) : base(PieceType.Navy, color)
        {
        }
    }
}