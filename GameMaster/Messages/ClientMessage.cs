namespace GameMaster.Messages
{
    public interface IClientMessage
    {
    }
    public class ClientMessage : IClientMessage
    {
        public long Id { get; set; }
    }
}
