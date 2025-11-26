using System;
using System.Collections.Generic;
using System.Linq;

namespace GameCore;

public class Board
{
    public const int Rows = 12; 
    public const int Columns = 11; 
    private readonly Dictionary<Position, Piece> _pieces = new();

    public IReadOnlyCollection<int> SeaColumns { get; } = new HashSet<int> { 0, 1 };
    public IReadOnlyCollection<int> RiverRows { get; } = new HashSet<int> { 5, 6 };
    // Ngầm (vị trí nước nông qua được): Cột 3 và 7
    public IReadOnlyCollection<int> ShallowRiverCols { get; } = new HashSet<int> { 3, 7 }; 

    public List<MoveRecord> MoveHistory { get; } = new();
    public Player ActivePlayer { get; private set; } = Player.Red;

    public bool IsInside(Position pos) => pos.Row >= 0 && pos.Row < Rows && pos.Column >= 0 && pos.Column < Columns;
    public Piece? GetPieceAt(Position pos) => _pieces.TryGetValue(pos, out var p) ? p : null;
    public IEnumerable<KeyValuePair<Position, Piece>> Pieces => _pieces;
    public void PlacePiece(Position pos, Piece piece) { if(!IsInside(pos)) throw new ArgumentOutOfRangeException(); _pieces[pos] = piece; }
    public void RemovePiece(Position pos) => _pieces.Remove(pos);

    public bool IsSea(Position pos) => SeaColumns.Contains(pos.Column);
    public bool IsRiver(Position pos) => RiverRows.Contains(pos.Row);
    public bool IsShallowRiver(Position pos) => IsRiver(pos) && ShallowRiverCols.Contains(pos.Column);

    // --- KIỂM TRA ĐỊA HÌNH ---
    // --- CẬP NHẬT LOGIC CAN ENTER ---
    public bool CanEnter(Position pos, Piece piece)
    {
        if (!IsInside(pos)) return false;

        // 1. XỬ LÝ VÙNG BIỂN (Cột 0, 1)
        if (IsSea(pos))
        {
            // Hải quân luôn được vào biển
            if (piece.Type == PieceType.Navy) return true;

            // Không quân (AirForce):
            // Chỉ được phép đáp xuống biển NẾU tại đó có Tàu chiến (Tàu sân bay)
            if (piece.Type == PieceType.AirForce)
            {
                var target = GetPieceAt(pos);
                // Nếu có Tàu chiến ta -> OK (đậu lên)
                if (target != null && target.Type == PieceType.Navy && target.Color == piece.Color) 
                    return true;
                
                // Nếu là ô trống -> KHÔNG ĐƯỢC ĐẬU (Máy bay không bơi được)
                return false; 
            }

            return false; // Các quân khác không xuống biển
        }

        // ... (Các logic khác giữ nguyên) ...

        // [QUAN TRỌNG] Nếu Tàu chở Máy bay -> Được trỏ vào đất liền (để Máy bay bay)
        if (piece.Type == PieceType.Navy && piece.Passenger?.Type == PieceType.AirForce) return true;

        // 2. Ven bờ (Cột 2): Navy được vào
        if (piece.Type == PieceType.Navy && pos.Column == 2) return true;

        // 3. Sông
        if (IsRiver(pos))
        {
            if (piece.Type == PieceType.Headquarter) return false;
            if (IsShallowRiver(pos)) return true; // Ngầm -> Ai cũng qua được

            // Nước sâu -> Chặn hỏa lực nặng đi bộ
            if (piece.Type == PieceType.Artillery || 
                piece.Type == PieceType.Rocket || 
                piece.Type == PieceType.AntiAircraftGun)
            {
                return false;
            }
            return true;
        }

        // 4. Đất liền sâu (Cột > 2): Navy không vào
        if (piece.Type == PieceType.Navy && !IsSea(pos) && !IsRiver(pos)) return false;

        return true;
    }

    // --- LOGIC DI CHUYỂN CHÍNH (TRY MOVE) ---
    public bool TryMove(Position from, Position to)
    {
        if (from == to) return false;
        if (!_pieces.ContainsKey(from)) return false;
        var movingPiece = _pieces[from];

        if (movingPiece.Color != ActivePlayer) return false;

        var legalMoves = GetLegalMoves(from).ToHashSet();
        if (!legalMoves.Contains(to)) return false;

        if (!IsCommanderSafeAfter(from, to)) return false;

        var targetPiece = GetPieceAt(to);
        Piece? captured = null;

        // PHÂN TÍCH: XE ĐI HAY NGƯỜI XUỐNG?
        // Kiểm tra xem nước đi này có phải của Carrier (Xe) không?
        bool isCarrierMove = false;
        var carrierMoves = MovementRules.GenerateMoves(this, from, movingPiece, includePassenger: false);
        
        if (carrierMoves.Contains(to)) isCarrierMove = true; // Đích nằm trong tầm xe -> Xe đi
        else if (movingPiece.Passenger == null) isCarrierMove = true; // Không có khách -> Xe đi

        // THỰC HIỆN
        if (targetPiece == null) // Đi vào ô trống
        {
            if (isCarrierMove)
            {
                _pieces.Remove(from);
                _pieces[to] = movingPiece; // Đi cả cụm
            }
            else
            {
                // Tách quân (Unload): Lính nhảy ra 'to'
                var passenger = movingPiece.Passenger;
                movingPiece.Passenger = null;
                _pieces[to] = passenger!;
                passenger!.HasMoved = true;
            }
        }
        else if (targetPiece.Color != movingPiece.Color) // Ăn quân
        {
            captured = targetPiece;
            if (isCarrierMove)
            {
                _pieces.Remove(from);
                _pieces[to] = movingPiece;
            }
            else
            {
                // Lính nhảy ra ăn
                var passenger = movingPiece.Passenger;
                movingPiece.Passenger = null;
                _pieces[to] = passenger!;
                passenger!.HasMoved = true;
            }
        }
        else // Gặp quân ta (Stacking / Ẩn nấp)
        {
            if (targetPiece.CanCarry(movingPiece)) // VD: Tư lệnh vào Sở chỉ huy, Lính lên Xe
            {
                targetPiece.Passenger = movingPiece;
                _pieces.Remove(from);
            }
            else if (movingPiece.CanCarry(targetPiece)) // VD: Xe đón Lính
            {
                movingPiece.Passenger = targetPiece;
                _pieces.Remove(from);
                _pieces[to] = movingPiece; 
            }
            else return false;
        }

        if (isCarrierMove) movingPiece.HasMoved = true;
        
        MoveHistory.Add(new MoveRecord(from, to, movingPiece, captured, movingPiece.HasMoved));
        SwitchTurn();
        return true;
    }

    private void SwitchTurn() => ActivePlayer = ActivePlayer.Opponent();

    public IEnumerable<Position> GetLegalMoves(Position from)
    {
        if (!_pieces.ContainsKey(from)) return Enumerable.Empty<Position>();
        var piece = _pieces[from];
        return MovementRules.GenerateMoves(this, from, piece)
            .Where(p => CanEnter(p, piece) && IsCommanderSafeAfter(from, p));
    }

    // ... (Phần còn lại giữ nguyên) ...
    public Dictionary<Position, List<Position>> GetAllLegalMoves(Player player) {
        var dict = new Dictionary<Position, List<Position>>();
        foreach (var kv in _pieces.Where(kv => kv.Value.Color == player)) {
            var moves = GetLegalMoves(kv.Key).ToList();
            if (moves.Count > 0) dict[kv.Key] = moves;
        }
        return dict;
    }
    public bool IsCommanderSafeAfter(Position from, Position to) {
        var sim = Clone();
        var moving = sim.GetPieceAt(from);
        if (moving == null) return false;
        sim.RemovePiece(from); sim.RemovePiece(to); sim.PlacePiece(new Position(to.Row, to.Column), moving);
        Position? commanderPos = sim.Pieces.FirstOrDefault(kv => kv.Value.Type == PieceType.Commander && kv.Value.Color == moving.Color).Key;
        if (commanderPos is null) return true;
        var threats = sim.GetThreatenedSquares(moving.Color.Opponent());
        return !threats.Contains(commanderPos);
    }
    public HashSet<Position> GetThreatenedSquares(Player attacker) {
        var set = new HashSet<Position>();
        foreach (var kv in _pieces.Where(kv => kv.Value.Color == attacker)) {
            foreach (var p in MovementRules.GenerateThreatSquares(this, kv.Key, kv.Value)) if (IsInside(p)) set.Add(p);
        }
        return set;
    }
    public Board Clone() {
        var b = new Board();
        foreach (var kv in _pieces) b._pieces[new Position(kv.Key.Row, kv.Key.Column)] = kv.Value.Copy();
        b.ActivePlayer = ActivePlayer;
        return b;
    }
    public GameStatus EvaluateStatus(Player player) {
        var inCheck = IsInCheck(player);
        var hasMove = HasAnyLegalMove(player);
        if (inCheck && !hasMove) return GameStatus.Checkmate;
        if (!inCheck && !hasMove) return GameStatus.Stalemate;
        if (inCheck) return GameStatus.Check;
        return GameStatus.Normal;
    }
    public bool UndoLastMove() {
        if (MoveHistory.Count == 0) return false;
        var last = MoveHistory[^1];
        MoveHistory.RemoveAt(MoveHistory.Count - 1);
        _pieces.Remove(last.To);
        _pieces[last.From] = last.Piece;
        last.Piece.HasMoved = last.OriginalHasMoved;
        if (last.Captured != null) _pieces[last.To] = last.Captured;
        ActivePlayer = ActivePlayer.Opponent();
        return true;
    }
    public bool HasAnyLegalMove(Player player) => GetAllLegalMoves(player).Any();
    public bool IsInCheck(Player player) {
        Position? commanderPos = _pieces.FirstOrDefault(kv => kv.Value.Type == PieceType.Commander && kv.Value.Color == player).Key;
        if (commanderPos is null) return false;
        return GetThreatenedSquares(player.Opponent()).Contains(commanderPos);
    }
    public void ResetToInitialPosition() {
        _pieces.Clear();
        ActivePlayer = Player.Red;
        void Add(int x, int viewY, Piece piece) {
            int row = Rows - 1 - viewY; 
            int col = x;
            _pieces[new Position(row, col)] = piece;
        }
        Add(6,0, new Commander(Player.Red));
        Add(3,4, new Engineer(Player.Red)); Add(9,4, new Engineer(Player.Red));
        Add(1,1, new Navy(Player.Red));
        Add(4,1, new AirForce(Player.Red));
        Add(5,1, new Headquarter(Player.Red));
        Add(7,1, new Headquarter(Player.Red));
        Add(8,1, new AirForce(Player.Red));
        Add(3,2, new Artillery(Player.Red)); Add(6,2, new Rocket(Player.Red)); Add(9,2, new Artillery(Player.Red));
        Add(2,3, new Navy(Player.Red));
        Add(4,3, new AntiAircraftGun(Player.Red)); Add(5,3, new Tank(Player.Red)); Add(7,3, new Tank(Player.Red)); Add(8,3, new AntiAircraftGun(Player.Red));
        Add(2,4, new Infantry(Player.Red)); Add(6,4, new Militia(Player.Red)); Add(10,4, new Infantry(Player.Red));
        Add(2,7, new Infantry(Player.Blue)); Add(6,7, new Militia(Player.Blue)); Add(10,7, new Infantry(Player.Blue));
        Add(2,8, new Navy(Player.Blue));
        Add(4,8, new AntiAircraftGun(Player.Blue)); Add(5,8, new Tank(Player.Blue)); Add(7,8, new Tank(Player.Blue)); Add(8,8, new AntiAircraftGun(Player.Blue));
        Add(3,9, new Artillery(Player.Blue)); Add(6,9, new Rocket(Player.Blue)); Add(9,9, new Artillery(Player.Blue));
        Add(1,10, new Navy(Player.Blue));
        Add(4,10, new AirForce(Player.Blue));
        Add(5,10, new Headquarter(Player.Blue));
        Add(7,10, new Headquarter(Player.Blue));
        Add(8,10, new AirForce(Player.Blue));
        Add(6,11, new Commander(Player.Blue));
        Add(3,7, new Engineer(Player.Blue)); Add(9,7, new Engineer(Player.Blue));
    }

}
public enum GameStatus { Normal, Check, Checkmate, Stalemate }
public record MoveRecord(Position From, Position To, Piece Piece, Piece? Captured, bool OriginalHasMoved);
    

