namespace GameCore;

public class Infantry : Piece
{
    public override PieceType Type => PieceType.Infantry;
    public override Player Color { get; }

    public Infantry(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Infantry copy = new Infantry(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}