namespace GameCore;

public class Position
{
    public int Row { get; }
    public int Column { get; }

    public Position(int row, int column)
    {
        Row = row;
        Column = column;
    }
    public override bool Equals(object obj)
    {
        return obj is Position position &&
               Row == position.Row &&
               Column == position.Column;
    }
    // Tìm kiếm đối tượng Position
    public override int GetHashCode()
    {
        return HashCode.Combine(Row, Column);
    }

    public static bool operator ==(Position left, Position right)
    {
        return EqualityComparer<Position>.Default.Equals(left, right);
    }

    public static bool operator !=(Position left, Position right)
    {
        return !(left == right);
    }
    // Vị trí + Hướng đi = Vị trí mới.
    public static Position operator +(Position pos, Direction dir)
    {
        return new Position(pos.Row + dir.RowDelta , pos.Column + dir.ColumnDelta);
    }
}