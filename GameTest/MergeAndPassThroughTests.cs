using GameCore;
using Xunit;

namespace GameTest;

/// <summary>
/// Test suite để verify chức năng nhập quân (merge/stack) và đi xuyên
/// </summary>
public class MergeAndPassThroughTests
{
    [Fact]
    public void Commander_CanMergeIntoTank()
    {
        // Arrange
        var board = new Board();
        var tank = new Tank(Player.Red);
        var commander = new Commander(Player.Red);
        var blueCmd = new Commander(Player.Blue);
        
        var tankPos = new Position(5, 5);
        var commanderPos = new Position(6, 5); // Adjacent
        
        board.PlacePiece(tankPos, tank);
        board.PlacePiece(commanderPos, commander);
        board.PlacePiece(new Position(11, 7), blueCmd);
        board.ActivePlayer = Player.Red;
        
        // Act
        var legalMoves = board.GetLegalMoves(commanderPos).ToList();
        
        // Assert: Commander can move into tank position for merge
        Assert.Contains(tankPos, legalMoves);
        
        bool merged = board.TryMove(commanderPos, tankPos);
        Assert.True(merged);
        
        // Verify merge successful
        var piece = board.GetPieceAt(tankPos);
        Assert.Equal(PieceType.Tank, piece.Type);
        Assert.Single(piece.CarriedUnits);
        Assert.Equal(PieceType.Commander, piece.CarriedUnits[0].Type);
    }
    
    [Fact]
    public void Tank_CanMergeIntoAirForce()
    {
        // Arrange
        var board = new Board();
        var airforce = new AirForce(Player.Red);
        var tank = new Tank(Player.Red);
        var red = new Commander(Player.Red);
        var blue = new Commander(Player.Blue);
        
        var airforcePos = new Position(5, 5);
        var tankPos = new Position(6, 5);
        
        board.PlacePiece(airforcePos, airforce);
        board.PlacePiece(tankPos, tank);
        board.PlacePiece(new Position(0, 3), red);
        board.PlacePiece(new Position(11, 7), blue);
        board.ActivePlayer = Player.Red;
        
        // Act
        var legalMoves = board.GetLegalMoves(tankPos).ToList();
        
        // Assert
        Assert.Contains(airforcePos, legalMoves);
        
        bool merged = board.TryMove(tankPos, airforcePos);
        Assert.True(merged);
        
        var piece = board.GetPieceAt(airforcePos);
        Assert.Equal(PieceType.AirForce, piece.Type);
        Assert.Single(piece.CarriedUnits);
        Assert.Equal(PieceType.Tank, piece.CarriedUnits[0].Type);
    }
    
    [Fact]
    public void Militia_CanMergeIntoTank()
    {
        // Arrange
        var board = new Board();
        var tank = new Tank(Player.Red);
        var militia = new Militia(Player.Red);
        var red = new Commander(Player.Red);
        var blue = new Commander(Player.Blue);
        
        var tankPos = new Position(5, 5);
        var militiaPos = new Position(6, 5);
        
        board.PlacePiece(tankPos, tank);
        board.PlacePiece(militiaPos, militia);
        board.PlacePiece(new Position(0, 3), red);
        board.PlacePiece(new Position(11, 7), blue);
        board.ActivePlayer = Player.Red;
        
        // Act
        bool merged = board.TryMove(militiaPos, tankPos);
        
        // Assert
        Assert.True(merged);
        Assert.Single(tank.CarriedUnits);
        Assert.Equal(PieceType.Militia, tank.CarriedUnits[0].Type);
    }
    
    [Fact]
    public void AirForce_CanFlyOverFriendlyPieces()
    {
        // Arrange
        var board = new Board();
        var airforce = new AirForce(Player.Red);
        var infantry = new Infantry(Player.Red);
        var red = new Commander(Player.Red);
        var blue = new Commander(Player.Blue);
        
        var airforcePos = new Position(5, 5);
        var infantryPos = new Position(6, 5); // Blocking position
        var targetPos = new Position(7, 5); // Beyond infantry
        
        board.PlacePiece(airforcePos, airforce);
        board.PlacePiece(infantryPos, infantry);
        board.PlacePiece(new Position(0, 3), red);
        board.PlacePiece(new Position(11, 7), blue);
        board.ActivePlayer = Player.Red;
        
        // Act
        var legalMoves = board.GetLegalMoves(airforcePos).ToList();
        
        // Assert: AirForce can fly over infantry to reach target
        Assert.Contains(targetPos, legalMoves);
    }
    
    [Fact]
    public void Navy_CanPassThroughFriendlyNavy()
    {
        // Arrange
        var board = new Board();
        var navy1 = new Navy(Player.Red);
        var navy2 = new Navy(Player.Red);
        
        var navy1Pos = new Position(5, 0); // Sea
        var navy2Pos = new Position(6, 0); // Blocking sea position
        var targetPos = new Position(7, 0); // Beyond navy2
        
        board.PlacePiece(navy1Pos, navy1);
        board.PlacePiece(navy2Pos, navy2);
        board.ActivePlayer = Player.Red;
        
        // Act
        var legalMoves = board.GetLegalMoves(navy1Pos).ToList();
        
        // Assert: Navy can pass through friendly navy
        Assert.Contains(targetPos, legalMoves);
    }
    
    [Fact]
    public void MultipleUnits_CanStackOnCarrier()
    {
        // Arrange: Test that Tank can carry infantry, then move to AirForce
        var board = new Board();
        var airforce = new AirForce(Player.Red);
        var tank = new Tank(Player.Red);
        var infantry = new Infantry(Player.Red);
        var red = new Commander(Player.Red);
        var blue = new Commander(Player.Blue);
        
        var infantryPos = new Position(6, 5);
        var tankPos = new Position(5, 5);
        var airforcePos = new Position(4, 5);
        
        board.PlacePiece(infantryPos, infantry);
        board.PlacePiece(tankPos, tank);
        board.PlacePiece(airforcePos, airforce);
        board.PlacePiece(new Position(0, 3), red);
        board.PlacePiece(new Position(11, 7), blue);
        board.ActivePlayer = Player.Red;
        
        // Act: First merge infantry into tank
        bool merged1 = board.TryMove(infantryPos, tankPos);
        Assert.True(merged1);
        
        // Switch turn back to Red (for testing)
        board.ActivePlayer = Player.Red;
        
        // Act: Then merge tank+infantry into airforce
        bool merged2 = board.TryMove(tankPos, airforcePos);
        Assert.True(merged2);
        
        // Assert: AirForce now carries tank, tank carries infantry
        var piece = board.GetPieceAt(airforcePos);
        Assert.Equal(PieceType.AirForce, piece.Type);
        Assert.Single(piece.CarriedUnits);
        Assert.Equal(PieceType.Tank, piece.CarriedUnits[0].Type);
        Assert.Single(piece.CarriedUnits[0].CarriedUnits);
        Assert.Equal(PieceType.Infantry, piece.CarriedUnits[0].CarriedUnits[0].Type);
    }
    
    [Fact]
    public void StackedUnits_ScoreIncludesAll()
    {
        // Arrange
        var airforce = new AirForce(Player.Red); // 40
        var tank = new Tank(Player.Red); // 20
        var infantry = new Infantry(Player.Red); // 10
        
        // Act: Stack them
        infantry.IsBeingCarried = true;
        tank.CarriedUnits.Add(infantry);
        tank.IsBeingCarried = true;
        airforce.CarriedUnits.Add(tank);
        
        // Assert: Total score = 40 + 20 + 10 = 70
        Assert.Equal(70, airforce.TotalScore());
    }
    
    [Fact]
    public void CannotMerge_IncompatibleTypes()
    {
        // Arrange: Artillery cannot be carried by tank
        var board = new Board();
        var tank = new Tank(Player.Red);
        var artillery = new Artillery(Player.Red);
        var red = new Commander(Player.Red);
        var blue = new Commander(Player.Blue);
        
        var tankPos = new Position(5, 5);
        var artilleryPos = new Position(6, 5);
        
        board.PlacePiece(tankPos, tank);
        board.PlacePiece(artilleryPos, artillery);
        board.PlacePiece(new Position(0, 3), red);
        board.PlacePiece(new Position(11, 7), blue);
        board.ActivePlayer = Player.Red;
        
        // Act
        var legalMoves = board.GetLegalMoves(artilleryPos).ToList();
        
        // Assert: Artillery cannot move into tank (no merge capability)
        Assert.DoesNotContain(tankPos, legalMoves);
    }
}

