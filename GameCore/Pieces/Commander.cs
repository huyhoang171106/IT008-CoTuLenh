namespace GameCore;

public class Commander : Piece
{
    public override PieceType Type => PieceType.Commander;
    public override Player Color { get; }

    public Commander(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Commander copy = new Commander(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}