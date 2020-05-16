namespace Shared.Enums
{
    public enum GMMessageId
    {
        Unknown,

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

        CSDisconnected = 1001, // inside type
    }
}
