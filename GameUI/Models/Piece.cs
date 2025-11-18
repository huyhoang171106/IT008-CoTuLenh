// GameUI/Models/Piece.cs
namespace GameUI
{
    public class Piece
    {
        public int Row { get; set; } // 0-based
        public int Col { get; set; } // 0-based
        public string ImagePath { get; set; } = string.Empty; // e.g., "Assets/Images/Pieces/Tank.png"
    }
}