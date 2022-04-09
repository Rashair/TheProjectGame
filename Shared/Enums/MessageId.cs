namespace Shared.Enums;

public enum MessageID
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

    CheckAnswer = 101,
    DestructionAnswer,
    DiscoverAnswer,
    EndGame,
    StartGame,
    BegForInfoForwarded,
    JoinTheGameAnswer,
    MoveAnswer,
    PickAnswer,
    PutAnswer,
    GiveInfoForwarded,
    InformationExchangeResponse,
    InformationExchangeRequest,

    InvalidMoveError = 901,
    PickError, // TODO Issue 111
    PutError,
    NotWaitedError,
    UnknownError,
    PlayerDisconnected,

    CSDisconnected = 1001, // inside type
}
