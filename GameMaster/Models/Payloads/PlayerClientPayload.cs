namespace GameMaster.Models.Payloads;

public class PlayerClientPayload : ClientPayload
{
    public int Id { get; set; }

    public int Team { get; set; }
    
    public int X { get; set; }
    
    public int Y { get; set; }

    public bool IsLeader { get; set; }
}
