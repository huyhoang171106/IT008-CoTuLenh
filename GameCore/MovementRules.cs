using System.Collections.Generic;
using System.Linq;
namespace GameCore;
// Quy tắc di chuyển cơ bản cho từng loại quân (tạm thời, có thể tinh chỉnh)
public static class MovementRules
{
    public static IEnumerable<Position> GenerateMoves(Board board, Position from, Piece piece)
    {
        return piece.Type switch
        {
            PieceType.Commander => OneStepAllDirections(board, from, piece),
            PieceType.Infantry => InfantryMoves(board, from, piece),
            PieceType.Militia => OneStepAllDirections(board, from, piece),
            PieceType.Engineer => EngineerMoves(board, from, piece),
            PieceType.Tank => TankMoves(board, from, piece),
            PieceType.Artillery => RangedLinear(board, from, piece, maxRange: 3),
            PieceType.AntiAircraftGun => RangedLinear(board, from, piece, maxRange: 2),
            PieceType.Rocket => RangedLinear(board, from, piece, maxRange: 5),
            PieceType.AirForce => AirForceMoves(board, from, piece),
            PieceType.Navy => NavyMoves(board, from, piece),
            PieceType.Headquarter => Enumerable.Empty<Position>(), // static
            _ => Enumerable.Empty<Position>()
        };
    }

    // Ô bị đe dọa (có thể khác di chuyển thực tế đối với một số quân). Hiện giữ nguyên: pháo/rocket chỉ đe dọa trong phạm vi di chuyển.
    public static IEnumerable<Position> GenerateThreatSquares(Board board, Position from, Piece piece)
    {
        if (piece.Type == PieceType.Headquarter) return Enumerable.Empty<Position>();
        // Tương lai: nếu pháo/rocket có tầm bắn vượt quá không di chuyển, tách logic ở đây.
        return GenerateMoves(board, from, piece);
    }

    private static IEnumerable<Position> OneStepAllDirections(Board board, Position from, Piece piece)
    {
        Direction[] dirs = { Direction.North, Direction.South, Direction.East, Direction.West, Direction.NorthEast, Direction.NorthWest, Direction.SouthEast, Direction.SouthWest };
        foreach (var d in dirs)
        {
            var p = from + d;
            if (board.IsInside(p) && !IsFriendly(board, p, piece.Color)) yield return p;
        }
    }

    private static IEnumerable<Position> InfantryMoves(Board board, Position from, Piece piece)
    {
        // Bộ binh: di chuyển 1 ô 4 hướng (N,S,E,W); ăn quân nếu ô đó có địch
        Direction[] dirs = { Direction.North, Direction.South, Direction.East, Direction.West };
        foreach (var d in dirs)
        {
            var p = from + d;
            if (!board.IsInside(p)) continue;
            var target = board.GetPieceAt(p);
            if (target == null || (target.Color != piece.Color && target.Color != Player.None))
                yield return p;
        }
    }

    private static IEnumerable<Position> EngineerMoves(Board board, Position from, Piece piece)
    {
        return OneStepAllDirections(board, from, piece);
    }

    private static IEnumerable<Position> TankMoves(Board board, Position from, Piece piece)
    {
        // Xe tăng: đi thẳng tối đa 2 ô, không nhảy qua quân cản
        Direction[] dirs = { Direction.North, Direction.South, Direction.East, Direction.West };
        foreach (var d in dirs)
        {
            var p1 = from + d;
            if (!board.IsInside(p1)) continue;
            if (board.GetPieceAt(p1) == null || IsEnemy(board, p1, piece.Color)) yield return p1;
            if (board.GetPieceAt(p1) != null) continue; // bị chặn
            var p2 = p1 + d;
            if (board.IsInside(p2) && (board.GetPieceAt(p2) == null || IsEnemy(board, p2, piece.Color))) yield return p2;
        }
    }

    private static IEnumerable<Position> RangedLinear(Board board, Position from, Piece piece, int maxRange)
    {
        Direction[] dirs = { Direction.North, Direction.South, Direction.East, Direction.West };
        foreach (var d in dirs)
        {
            for (int step = 1; step <= maxRange; step++)
            {
                var p = from + (step * d);
                if (!board.IsInside(p)) break;
                if (board.GetPieceAt(p) == null)
                {
                    yield return p;
                    continue;
                }
                if (IsEnemy(board, p, piece.Color)) yield return p;
                break; // chặn bởi bất kỳ quân nào
            }
        }
    }

    private static IEnumerable<Position> AirForceMoves(Board board, Position from, Piece piece)
    {
        // Không quân: nhảy đến bất kỳ ô trong hình vuông 2 bước (Manhattan <=2), bỏ qua quân cản
        for (int dr=-2; dr<=2; dr++)
        {
            for (int dc=-2; dc<=2; dc++)
            {
                if (System.Math.Abs(dr) + System.Math.Abs(dc) == 0) continue;
                if (System.Math.Abs(dr) + System.Math.Abs(dc) > 2) continue;
                var p = new Position(from.Row + dr, from.Column + dc);
                if (board.IsInside(p) && !IsFriendly(board, p, piece.Color)) yield return p;
            }
        }
    }

    private static IEnumerable<Position> NavyMoves(Board board, Position from, Piece piece)
    {
        foreach (var p in TankMoves(board, from, piece)) yield return p;
        Direction[] diag = { Direction.NorthEast, Direction.NorthWest, Direction.SouthEast, Direction.SouthWest };
        foreach (var d in diag)
        {
            var p = from + d;
            if (board.IsInside(p) && !IsFriendly(board, p, piece.Color)) yield return p;
        }
    }

    private static bool IsFriendly(Board board, Position pos, Player color)
        => board.GetPieceAt(pos)?.Color == color;
    private static bool IsEnemy(Board board, Position pos, Player color)
        => board.GetPieceAt(pos)?.Color is Player other && other != color && other != Player.None;
}
