namespace GameCore
{
    public class Rocket : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public Rocket(Player color) : base(PieceType.Rocket, color)
        {
        }
    }
}