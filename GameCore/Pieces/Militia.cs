namespace GameCore;

public class Militia : Piece
{
    public override PieceType Type => PieceType.Militia;
    public override Player Color { get; }

    public Militia(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Militia copy = new Militia(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}