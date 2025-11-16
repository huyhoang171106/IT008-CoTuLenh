namespace GameCore;

public class Direction
{
    // Các hướng đi cơ bản(4 hướng)
    public readonly static Direction North = new Direction(-1, 0);
    public readonly static Direction South = new Direction(1, 0);
    public readonly static Direction East = new Direction(0, 1);
    public readonly static Direction West = new Direction(0, -1);
    // Các hướng đi chéo
    public readonly static Direction NorthEast = North + East;
    public readonly static Direction NorthWest = North + West;
    public readonly static Direction SouthEast = South + East;
    public readonly static Direction SouthWest = South + West;  
    // Đọc và lưu sự thay đổi hàng cột
    public int RowDelta { get; }
    public int ColumnDelta { get; }
    public Direction(int rowDelta, int columnDelta)
    {
        RowDelta = rowDelta;
        ColumnDelta = columnDelta;  
    }
    // Hướng đi 1 + Hướng đi 2 = Hướng đi mới.
    public static Direction operator +(Direction dir1, Direction dir2)
    {
        return new Direction(dir1.RowDelta + dir2.RowDelta, dir1.ColumnDelta + dir2.ColumnDelta);
    }
    // Ví dụ: 3 * Direction.North(-1, 0) = new Direction(-3, 0) (đi lên 3 ô).
    public static Direction operator *(int scalar, Direction dir)
    {
        return new Direction(scalar * dir.RowDelta, scalar * dir.ColumnDelta);
    }
}