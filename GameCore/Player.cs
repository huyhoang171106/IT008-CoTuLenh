namespace GameCore;
// Người chơi
public enum Player
{
    None,
    Red,
    Blue
}
// Phương thức bổ sung để lấy người chơi đối thủ
public static class PlayerExtensions
{
    public static Player Opponent(this Player player)
    {
        return player switch
        {
            Player.Red => Player.Blue,
            Player.Blue => Player.Red,
            _ => Player.None
        };
    }
}
    

