namespace Shared.Enums
{
    public enum PlayerMessageId
    {
        Unknown,
        CheckPiece,
        PieceDestruction,
        Discover,
        GiveInfo,
        BegForInfo,
        JoinTheGame,
        Move,
        Pick,
        Put,
        Disconnected = 906,

        CSDisconnected = 1002, // inside type
    }
}
