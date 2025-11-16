namespace GameCore;

public class Navy : Piece
{
    public override PieceType Type => PieceType.Navy;
    public override Player Color { get; }

    public Navy(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Navy copy = new Navy(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}