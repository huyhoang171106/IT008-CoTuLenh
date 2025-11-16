namespace GameCore;

public class Artillery : Piece
{
    public override PieceType Type => PieceType.Artillery;
    public override Player Color { get; }

    public Artillery(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Artillery copy = new Artillery(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}