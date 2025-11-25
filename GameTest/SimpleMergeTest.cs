using GameCore;
using Xunit;

namespace GameTest;

public class SimpleMergeTest
{
    [Fact]
    public void TestMoveGeneration()
    {
        // Arrange
        var board = new Board();
        var infantry = new Infantry(Player.Red);
        
        // Position: Row 6, Column 5
        var infantryPos = new Position(6, 5);
        board.PlacePiece(infantryPos, infantry);
        board.ActivePlayer = Player.Red;
        
        // Act: Get all possible moves
        var moves = MovementRules.GenerateMoves(board, infantryPos, infantry).ToList();
        
        // Assert: Print moves for debug
        foreach (var move in moves)
        {
            Console.WriteLine($"Move: Row={move.Row}, Column={move.Column}");
        }
        
        // Infantry should be able to move North (row-1), South (row+1), East (col+1), West (col-1)
        // Expected moves from (6,5):
        // North: (5,5)
        // South: (7,5)
        // East: (6,6)
        // West: (6,4)
        
        Assert.Contains(moves, m => m.Row == 5 && m.Column == 5); // North
        Assert.Contains(moves, m => m.Row == 7 && m.Column == 5); // South
        Assert.Contains(moves, m => m.Row == 6 && m.Column == 6); // East
        Assert.Contains(moves, m => m.Row == 6 && m.Column == 4); // West
    }
}

