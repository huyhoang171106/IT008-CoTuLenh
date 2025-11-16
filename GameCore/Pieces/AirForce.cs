namespace GameCore;

public class AirForce : Piece
{
    public override PieceType Type => PieceType.AirForce;
    public override Player Color { get; }

    public AirForce(Player color)
    {
        Color = color;
    }

    public override Piece Copy()
    {
        AirForce copy = new AirForce(Color);
        copy.HasMoved = HasMoved;
        return copy;
    }
}