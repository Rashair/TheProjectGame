namespace GameMaster.Messages
{
    public interface IPlayerMessage
    {
    }
    public class PlayerMessage : IPlayerMessage
    {
        public long Id { get; set; }
    }
}
