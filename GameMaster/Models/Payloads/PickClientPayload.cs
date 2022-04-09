namespace GameMaster.Models.Payloads;

public class PickClientPayload : ClientPayload
{
    public int Id { get; set; }

    public bool ContainPieces { get; set; }
}
