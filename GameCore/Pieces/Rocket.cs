namespace GameCore;

public class Rocket : Piece
{
    public override PieceType Type => PieceType.Rocket;
    public override Player Color { get; }

    public Rocket(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Rocket copy = new Rocket(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}