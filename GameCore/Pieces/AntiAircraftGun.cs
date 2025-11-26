namespace GameCore
{
    public class AntiAircraftGun : Piece
    {
        // Constructor chỉ việc gọi về base cha là đủ
        public AntiAircraftGun(Player color) : base(PieceType.AntiAircraftGun, color)
        {
        }
    }
}