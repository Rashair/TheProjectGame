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

        InvalidMoveError = 901,
        PickError, // TODO Issue 111
        PutError,
        NotWaitedError,
        UnknownError,
    }
}
