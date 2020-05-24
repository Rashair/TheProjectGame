namespace GameMaster.Models.Payloads
{
    public class MoveClientPayload : ClientPayload
    {
        public int Id { get; set; }

        public int X { get; set; }
        
        public int Y { get; set; }
    }
}
