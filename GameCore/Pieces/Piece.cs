using System;

namespace GameCore;
public class Piece
{
    public PieceType Type { get; }
    public Player Color { get; }
    public bool HasMoved { get; set; }
    public bool IsHero { get; set; }
    public Piece? Passenger { get; set; } // Quân đang nằm trong bụng

    public Piece(PieceType type, Player color)
    {
        Type = type;
        Color = color;
    }

    public Piece Copy()
    {
        return new Piece(Type, Color)
        {
            HasMoved = this.HasMoved,
            IsHero = this.IsHero,
            Passenger = this.Passenger?.Copy()
        };
    }

    // --- LOGIC KIỂM TRA CÕNG QUÂN / ẨN NẤP ---
    public bool CanCarry(Piece other)
    {
        // 1. Đã đầy thì không chứa thêm
        if (this.Passenger != null) return false;

        // 2. Chỉ chứa quân cùng màu
        if (this.Color != other.Color) return false;

        // 3. CÁC QUY TẮC CỤ THỂ:

        // A. Sở chỉ huy (Lô cốt) -> Chỉ chứa Tư lệnh (Ẩn nấp)
        if (this.Type == PieceType.Headquarter)
        {
            return other.Type == PieceType.Commander;
        }

        // B. Tàu chiến (Navy) -> Cõng được gần như tất cả (trừ hỏa lực nặng và tàu khác)
        if (this.Type == PieceType.Navy)
        {
            return other.Type == PieceType.AirForce ||
                   other.Type == PieceType.Tank ||
                   other.Type == PieceType.Infantry ||
                   other.Type == PieceType.Militia ||
                   other.Type == PieceType.Commander;
        }

        // C. Xe tăng (Tank) -> Cõng Bộ binh, Dân quân, Tư lệnh
        if (this.Type == PieceType.Tank)
        {
            return other.Type == PieceType.Infantry ||
                   other.Type == PieceType.Militia ||
                   other.Type == PieceType.Commander;
        }

        // D. Máy bay (AirForce) -> Cõng Bộ binh, Dân quân, Tư lệnh
        if (this.Type == PieceType.AirForce)
        {
            return other.Type == PieceType.Infantry ||
                   other.Type == PieceType.Militia ||
                   other.Type == PieceType.Commander;
        }

        // E. Công binh (Engineer) -> Cõng Hỏa lực hạng nặng (để qua sông)
        if (this.Type == PieceType.Engineer)
        {
            return other.Type == PieceType.AntiAircraftGun ||
                   other.Type == PieceType.Artillery ||
                   other.Type == PieceType.Rocket;
        }

        return false;
    }
}