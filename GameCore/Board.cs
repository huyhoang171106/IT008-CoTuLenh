using System;
using System.Collections.Generic;
using System.Linq;

namespace GameCore;

// Bàn cờ 11 cột x 12 hàng (giao điểm) cột:0..10, hàng:0..11
public class Board
{
    public const int Rows = 12; // Y 0..11
    public const int Columns = 11; // X 0..10
    private readonly Dictionary<Position, Piece> _pieces = new();

    // Cấu hình địa hình (mặc định theo mô tả game: cột 0,1 là biển; hàng 5,6 là sông)
    public IReadOnlyCollection<int> SeaColumns { get; } = new HashSet<int> { 0, 1 };
    public IReadOnlyCollection<int> RiverRows { get; } = new HashSet<int> { 5, 6 };

    // Lịch sử nước đi đơn giản
    public List<MoveRecord> MoveHistory { get; } = new();

    // Người chơi hiện tại (mặc định Đỏ đi trước)
    public Player ActivePlayer { get; private set; } = Player.Red;

    public bool IsInside(Position pos) => pos.Row >= 0 && pos.Row < Rows && pos.Column >= 0 && pos.Column < Columns;
    public Piece? GetPieceAt(Position pos) => _pieces.TryGetValue(pos, out var p) ? p : null;

    public IEnumerable<KeyValuePair<Position, Piece>> Pieces => _pieces;

    public void PlacePiece(Position pos, Piece piece)
    {
        if (!IsInside(pos)) throw new ArgumentOutOfRangeException(nameof(pos));
        _pieces[pos] = piece;
    }

    public void RemovePiece(Position pos) => _pieces.Remove(pos);

    public bool TryMove(Position from, Position to)
    {
        if (from == to) return false;
        if (!_pieces.ContainsKey(from)) return false;
        var piece = _pieces[from];
        // Kiểm tra quyền sở hữu lượt
        if (piece.Color != ActivePlayer) return false;
        // Kiểm tra nước đi hợp lệ với an toàn tư lệnh
        var legal = GetLegalMoves(from).ToHashSet();
        if (!legal.Contains(to)) return false;
        if (!IsCommanderSafeAfter(from, to)) return false;
        var captured = GetPieceAt(to);
        var originalHasMoved = piece.HasMoved;
        _pieces.Remove(from);
        _pieces[to] = piece;
        piece.HasMoved = true;
        MoveHistory.Add(new MoveRecord(from, to, piece, captured, originalHasMoved));
        SwitchTurn();
        return true;
    }

    private void SwitchTurn()
    {
        ActivePlayer = ActivePlayer.Opponent();
    }

    public IEnumerable<Position> GetLegalMoves(Position from)
    {
        if (!_pieces.ContainsKey(from)) return Enumerable.Empty<Position>();
        var piece = _pieces[from];
        // Chỉ sinh nước đi cơ sở (không lọc Commander an toàn ở đây)
        return MovementRules.GenerateMoves(this, from, piece)
            .Where(p => CanEnter(p, piece) && !IsFriendly(p, piece.Color) && IsCommanderSafeAfter(from, p));
    }

    // Kiểm tra có phải ô biển / sông
    public bool IsSea(Position pos) => SeaColumns.Contains(pos.Column);
    public bool IsRiver(Position pos) => RiverRows.Contains(pos.Row);

    // Quy tắc vào ô: Hải quân chỉ ở biển; bộ binh/militia/tank/arty/hq không vào biển; AirForce/Rocket/Commander/Engineer có thể vào mọi ô; Hải quân không vào sông (tạm thời), tùy chỉnh sau
    public bool CanEnter(Position pos, Piece piece)
    {
        if (!IsInside(pos)) return false;
        if (IsSea(pos))
        {
            // Chỉ Navy hoặc AirForce đi vào biển
            if (piece.Type == PieceType.Navy || piece.Type == PieceType.AirForce) return true;
            return false;
        }
        if (IsRiver(pos))
        {
            // Có thể thêm hạn chế: chỉ Engineer/AirForce vượt sông thoải mái; tạm thời tất cả trừ Headquarter
            if (piece.Type == PieceType.Headquarter) return false;
            return true;
        }
        return true;
    }

    // Clone sâu dùng để mô phỏng
    public Board Clone()
    {
        var b = new Board();
        foreach (var kv in _pieces)
        {
            b._pieces[new Position(kv.Key.Row, kv.Key.Column)] = kv.Value.Copy();
        }
        b.ActivePlayer = ActivePlayer; // sao chép lượt hiện tại
        // MoveHistory intentionally not cloned for simulation
        return b;
    }

    public bool IsCommanderSafeAfter(Position from, Position to)
    {
        var sim = Clone();
        var moving = sim.GetPieceAt(from);
        if (moving == null) return false;
        sim.RemovePiece(from);
        sim.RemovePiece(to); // capture if enemy
        sim.PlacePiece(new Position(to.Row, to.Column), moving);
        // Tìm vị trí tư lệnh của màu di chuyển
        Position? commanderPos = sim.Pieces.FirstOrDefault(kv => kv.Value.Type == PieceType.Commander && kv.Value.Color == moving.Color).Key;
        if (commanderPos is null) return true; // nếu chưa đặt tư lệnh
        var threats = sim.GetThreatenedSquares(moving.Color.Opponent());
        return !threats.Contains(commanderPos);
    }

    public HashSet<Position> GetThreatenedSquares(Player attacker)
    {
        var set = new HashSet<Position>();
        foreach (var kv in _pieces.Where(kv => kv.Value.Color == attacker))
        {
            foreach (var p in MovementRules.GenerateThreatSquares(this, kv.Key, kv.Value))
            {
                if (IsInside(p)) set.Add(p);
            }
        }
        return set;
    }

    // Kiểm tra đang bị chiếu
    public bool IsInCheck(Player player)
    {
        Position? commanderPos = _pieces.FirstOrDefault(kv => kv.Value.Type == PieceType.Commander && kv.Value.Color == player).Key;
        if (commanderPos is null) return false; // chưa đặt tư lệnh => không tính
        var threats = GetThreatenedSquares(player.Opponent());
        return threats.Contains(commanderPos);
    }

    // Lấy tất cả nước đi hợp lệ của một người chơi (phục vụ AI / kiểm tra hết nước)
    public Dictionary<Position, List<Position>> GetAllLegalMoves(Player player)
    {
        var dict = new Dictionary<Position, List<Position>>();
        foreach (var kv in _pieces.Where(kv => kv.Value.Color == player))
        {
            var from = kv.Key;
            var moves = MovementRules.GenerateMoves(this, from, kv.Value)
                .Where(p => CanEnter(p, kv.Value) && !IsFriendly(p, player) && IsCommanderSafeAfter(from, p))
                .ToList();
            if (moves.Count > 0) dict[from] = moves;
        }
        return dict;
    }

    public bool HasAnyLegalMove(Player player) => GetAllLegalMoves(player).Any();

    public GameStatus EvaluateStatus(Player player)
    {
        var inCheck = IsInCheck(player);
        var hasMove = HasAnyLegalMove(player);
        if (inCheck && !hasMove) return GameStatus.Checkmate;
        if (!inCheck && !hasMove) return GameStatus.Stalemate;
        if (inCheck) return GameStatus.Check;
        return GameStatus.Normal;
    }

    // Hoàn tác nước đi cuối (để AI thử nghiệm) trả về true nếu thành công
    public bool UndoLastMove()
    {
        if (MoveHistory.Count == 0) return false;
        var last = MoveHistory[^1];
        MoveHistory.RemoveAt(MoveHistory.Count - 1);
        // Di chuyển quân về vị trí cũ
        _pieces.Remove(last.To);
        _pieces[last.From] = last.Piece;
        last.Piece.HasMoved = last.OriginalHasMoved;
        if (last.Captured != null)
        {
            // Khôi phục quân bị ăn
            _pieces[last.To] = last.Captured;
        }
        // Đảo lượt lại
        ActivePlayer = ActivePlayer.Opponent();
        return true;
    }

    private bool IsFriendly(Position pos, Player color) => GetPieceAt(pos)?.Color == color;
}

public enum GameStatus
{
    Normal,
    Check,
    Checkmate,
    Stalemate
}

public record MoveRecord(Position From, Position To, Piece Piece, Piece? Captured, bool OriginalHasMoved);
