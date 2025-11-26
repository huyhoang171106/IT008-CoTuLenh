namespace GameCore
{
    public class Engineer : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public Engineer(Player color) : base(PieceType.Engineer, color)
        {
        }
    }
}