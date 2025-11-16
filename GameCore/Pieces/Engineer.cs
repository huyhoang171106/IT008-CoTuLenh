namespace GameCore;

public class Engineer : Piece
{
    public override PieceType Type => PieceType.Engineer;
    public override Player Color { get; }

    public Engineer(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Engineer copy = new Engineer(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}