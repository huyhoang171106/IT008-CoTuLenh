namespace GameCore
{
    public class AirForce : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public AirForce(Player color) : base(PieceType.AirForce, color)
        {
        }
    }
}