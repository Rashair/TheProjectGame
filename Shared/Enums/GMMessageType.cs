namespace Shared.Enums
{
    public enum GMMessageType
    {
        Unknown,
        CheckPieceResponse = 101,
        PieceDestructionResponse,
        DiscoverResponse,
        EndOfGame,
        StartOfGame,
        ForwardKnowledgeReply,
        JoinTheGameResponse,
        MoveResponse,
        PickResponse,
        PutResponse,
        ForwardKnowledgeQuestion,
    }
}
