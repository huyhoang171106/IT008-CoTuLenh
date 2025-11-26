using System.Collections.Generic;
using System.Linq;

namespace GameCore;

public static class MovementRules
{
    private static readonly Direction[] Orthogonal = { Direction.North, Direction.South, Direction.East, Direction.West };
    private static readonly Direction[] Diagonal = { Direction.NorthEast, Direction.NorthWest, Direction.SouthEast, Direction.SouthWest };
    private static readonly Direction[] All8 = { Direction.North, Direction.South, Direction.East, Direction.West, Direction.NorthEast, Direction.NorthWest, Direction.SouthEast, Direction.SouthWest };

    // --- HÀM CHÍNH ---
    public static IEnumerable<Position> GenerateMoves(Board board, Position from, Piece piece, bool includePassenger = true)
    {
        List<Position> moves = (piece.Type switch
        {
            PieceType.Commander => CommanderMoves(board, from, piece),
            PieceType.Infantry => InfantryMoves(board, from, piece),
            
            // [SỬA LẠI ĐÚNG]: Gọi hàm riêng MilitiaMoves
            PieceType.Militia => MilitiaMoves(board, from, piece), 
            
            PieceType.Tank => TankMoves(board, from, piece),
            PieceType.Engineer => StepMoves(board, from, piece, Orthogonal), // Công binh chữ thập 1 ô
            PieceType.Artillery => ArtilleryMoves(board, from, piece),
            PieceType.AntiAircraftGun => AntiAirMoves(board, from, piece),
            PieceType.Rocket => RocketMoves(board, from, piece),
            PieceType.AirForce => AirForceMoves(board, from, piece),
            PieceType.Navy => NavyMoves(board, from, piece),
            _ => Enumerable.Empty<Position>()
        }).ToList();

        if (includePassenger && piece.Passenger != null)
        {
            moves.AddRange(GetPassengerMoves(board, from, piece.Passenger));
        }

        return moves;
    }
    
    private static IEnumerable<Position> GetPassengerMoves(Board board, Position from, Piece passenger)
    {
        return GenerateMoves(board, from, passenger, includePassenger: false);
    }

    public static IEnumerable<Position> GenerateThreatSquares(Board board, Position from, Piece piece) 
        => GenerateMoves(board, from, piece, includePassenger: true);

    // ================== LOGIC CHI TIẾT ==================

    // 1. DÂN QUÂN (MILITIA) - [LOGIC CHUẨN HÌNH 2]
    // - Đi Thẳng: 2 ô (Linear)
    // - Đi Chéo: 1 ô (Step)
    private static IEnumerable<Position> MilitiaMoves(Board board, Position from, Piece piece)
    {
        var moves = new List<Position>();
        
        // Thẳng 2 ô
        AddLinearMoves(board, from, Orthogonal, 2, piece.Color, moves, canJump: false);
        
        // Chéo 1 ô
        AddStepMoves(board, from, Diagonal, 1, piece.Color, moves);
        
        return moves;
    }

    // 2. TƯ LỆNH
    private static IEnumerable<Position> CommanderMoves(Board board, Position from, Piece piece)
    {
        var moves = new List<Position>();
        foreach (var dir in Orthogonal)
        {
            for (int k = 1; k <= 10; k++)
            {
                int r = from.Row + (dir.RowDelta * k);
                int c = from.Column + (dir.ColumnDelta * k);
                var target = new Position(r, c);

                if (!board.IsInside(target)) break;
                
                var targetPiece = board.GetPieceAt(target);
                if (targetPiece == null)
                {
                    moves.Add(target);
                }
                else
                {
                    if (k == 1) 
                    {
                        if (targetPiece.Color != piece.Color) moves.Add(target);
                        else if (targetPiece.CanCarry(piece)) moves.Add(target);
                    }
                    break; 
                }
            }
        }
        return moves;
    }

    // 3. HẢI QUÂN
    private static IEnumerable<Position> NavyMoves(Board board, Position from, Piece piece)
    {
        var moves = new List<Position>();
        AddLinearMoves(board, from, new[] { Direction.North, Direction.South }, 4, piece.Color, moves, canJump: true, mustBeSea: true);
        AddLinearMoves(board, from, new[] { Direction.East, Direction.West }, 3, piece.Color, moves, canJump: true, mustBeSea: false);
        AddStepMoves(board, from, Diagonal, 1, piece.Color, moves, mustBeSea: false);
        return moves;
    }

    // 4. KHÔNG QUÂN
    private static IEnumerable<Position> AirForceMoves(Board board, Position from, Piece piece)
    {
        var moves = new List<Position>();
        AddLinearMoves(board, from, All8, 4, piece.Color, moves, canJump: true);
        return moves;
    }

    // 5. XE TĂNG
    private static IEnumerable<Position> TankMoves(Board board, Position from, Piece piece)
    {
        var moves = new List<Position>();
        AddLinearMoves(board, from, Orthogonal, 2, piece.Color, moves, canJump: false);
        return moves;
    }

    // 6. TÊN LỬA
    private static IEnumerable<Position> RocketMoves(Board board, Position from, Piece piece)
    {
        var moves = new List<Position>();
        AddLinearMoves(board, from, Orthogonal, 2, piece.Color, moves, canJump: false);
        AddStepMoves(board, from, Diagonal, 1, piece.Color, moves);
        return moves;
    }

    // 7. PHÁO BINH
    private static IEnumerable<Position> ArtilleryMoves(Board board, Position from, Piece piece)
    {
        var moves = new List<Position>();
        AddLinearMoves(board, from, All8, 3, piece.Color, moves, canJump: false);
        return moves;
    }

    // 8. CAO XẠ
    private static IEnumerable<Position> AntiAirMoves(Board board, Position from, Piece piece)
    {
        var moves = new List<Position>();
        AddStepMoves(board, from, All8, 1, piece.Color, moves);
        return moves;
    }

    // 9. BỘ BINH
    private static IEnumerable<Position> InfantryMoves(Board board, Position from, Piece piece)
    {
        var moves = new List<Position>();
        AddStepMoves(board, from, Orthogonal, 1, piece.Color, moves);
        return moves;
    }

    // --- CÁC HÀM TIỆN ÍCH ---
    private static IEnumerable<Position> StepMoves(Board board, Position from, Piece piece, Direction[] dirs)
    {
        var moves = new List<Position>();
        AddStepMoves(board, from, dirs, 1, piece.Color, moves);
        return moves;
    }

    private static void AddLinearMoves(Board board, Position from, Direction[] dirs, int maxRange, Player myColor, List<Position> moves, bool canJump, bool mustBeSea = false)
    {
        foreach (var dir in dirs)
        {
            for (int k = 1; k <= maxRange; k++)
            {
                int r = from.Row + (dir.RowDelta * k);
                int c = from.Column + (dir.ColumnDelta * k);
                var target = new Position(r, c);

                if (!board.IsInside(target)) break;
                if (mustBeSea && !board.IsSea(target)) break;

                var targetPiece = board.GetPieceAt(target);

                if (targetPiece == null)
                {
                    moves.Add(target);
                }
                else 
                {
                    if (targetPiece.Color != myColor)
                    {
                        moves.Add(target); 
                        if (!canJump) break;
                        break; 
                    }
                    else
                    {
                        var movingPiece = board.GetPieceAt(from);
                        if (movingPiece != null && (targetPiece.CanCarry(movingPiece) || movingPiece.CanCarry(targetPiece)))
                        {
                            moves.Add(target);
                        }
                        if (!canJump) break;
                    }
                }
            }
        }
    }

    private static void AddStepMoves(Board board, Position from, Direction[] dirs, int range, Player myColor, List<Position> moves, bool mustBeSea = false)
    {
        foreach (var dir in dirs)
        {
            int r = from.Row + dir.RowDelta;
            int c = from.Column + dir.ColumnDelta;
            var target = new Position(r, c);

            if (board.IsInside(target))
            {
                if (mustBeSea && !board.IsSea(target)) continue;

                var targetPiece = board.GetPieceAt(target);
                if (targetPiece == null)
                {
                    moves.Add(target);
                }
                else if (targetPiece.Color != myColor)
                {
                    moves.Add(target);
                }
                else
                {
                    var movingPiece = board.GetPieceAt(from);
                    if (movingPiece != null && (targetPiece.CanCarry(movingPiece) || movingPiece.CanCarry(targetPiece)))
                    {
                        moves.Add(target);
                    }
                }
            }
        }
    }
}