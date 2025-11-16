namespace GameCore;

public class Tank : Piece
{
    public override PieceType Type => PieceType.Tank;
    public override Player Color { get; }

    public Tank(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        Tank copy = new Tank(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}