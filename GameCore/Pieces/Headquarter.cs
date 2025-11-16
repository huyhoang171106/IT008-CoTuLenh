namespace GameCore;

public class Headquarter : Piece
{
    public override PieceType Type => PieceType.Headquarter;
    public override Player Color { get; }

    public Headquarter(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Headquarter copy = new Headquarter(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}