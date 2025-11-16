namespace GameCore;

public class AntiAircraftGun : Piece
{
    public override PieceType Type => PieceType.AntiAircraftGun;
    public override Player Color { get; }

    public AntiAircraftGun(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        AntiAircraftGun copy = new AntiAircraftGun(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}