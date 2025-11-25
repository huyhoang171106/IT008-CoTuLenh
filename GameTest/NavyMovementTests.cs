using Xunit;
using GameCore;

namespace GameTest;

public class NavyMovementTests
{
    [Fact]
    public void Navy_CanMoveInAllEightDirections()
    {
        var board = new Board();
        
        // Place Navy in sea at position (5, 1)
        var navy = new Navy(Player.Red);
        board.PlacePiece(new Position(5, 1), navy);
        
        var moves = board.GetLegalMoves(new Position(5, 1)).ToList();
        
        // Navy should be able to move in all 8 directions
        // North
        Assert.Contains(new Position(4, 1), moves);
        Assert.Contains(new Position(3, 1), moves);
        
        // South
        Assert.Contains(new Position(6, 1), moves);
        Assert.Contains(new Position(7, 1), moves);
        
        // East (but column 2 is not sea, so should stop at column 1)
        // West (column 0 is sea)
        Assert.Contains(new Position(5, 0), moves);
        
        // Diagonal directions in sea
        // NorthEast - (4, 2) not sea
        // NorthWest
        Assert.Contains(new Position(4, 0), moves);
        
        // SouthEast - (6, 2) not sea
        // SouthWest
        Assert.Contains(new Position(6, 0), moves);
        
        // Should have multiple moves available
        Assert.NotEmpty(moves);
    }
    
    [Fact]
    public void Navy_CanMoveDiagonallyInSea()
    {
        var board = new Board();
        
        // Place Navy in middle of sea
        var navy = new Navy(Player.Red);
        board.PlacePiece(new Position(3, 1), navy);
        
        var moves = board.GetLegalMoves(new Position(3, 1)).ToList();
        
        // Check diagonal moves
        // NorthEast (2, 2) - not sea, should stop
        // NorthWest (2, 0) - sea
        Assert.Contains(new Position(2, 0), moves);
        
        // SouthEast (4, 2) - not sea, should stop  
        // SouthWest (4, 0) - sea
        Assert.Contains(new Position(4, 0), moves);
        
        // Check it can move multiple steps diagonally
        Assert.Contains(new Position(1, 0), moves); // 2 steps NorthWest
        Assert.Contains(new Position(0, 0), moves); // 3 steps NorthWest (if in range)
    }
    
    [Fact]
    public void Navy_CanMove_UpTo4Segments_InAnyDirection()
    {
        var board = new Board();
        
        // Place Navy at position with room to move
        var navy = new Navy(Player.Red);
        board.PlacePiece(new Position(6, 1), navy);
        
        var moves = board.GetLegalMoves(new Position(6, 1)).ToList();
        
        // Navy should be able to move up to 4 segments
        // North: (5,1), (4,1), (3,1), (2,1)
        Assert.Contains(new Position(5, 1), moves);
        Assert.Contains(new Position(4, 1), moves);
        Assert.Contains(new Position(3, 1), moves);
        Assert.Contains(new Position(2, 1), moves);
        
        // South: up to 4 segments (if board allows)
        Assert.Contains(new Position(7, 1), moves);
        Assert.Contains(new Position(8, 1), moves);
        Assert.Contains(new Position(9, 1), moves);
        Assert.Contains(new Position(10, 1), moves);
        
        // Should have many moves
        Assert.True(moves.Count >= 8);
    }
}

