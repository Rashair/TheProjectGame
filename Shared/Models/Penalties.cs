namespace Shared.Models;

public class Penalties
{
    public int Move { get; set; }

    public int Ask { get; set; }

    public int Response { get; set; }

    public int Discovery { get; set; }

    public int Pickup { get; set; }

    public int CheckForSham { get; set; }

    public int DestroyPiece { get; set; }

    public int PutPiece { get; set; }

    public int PrematureRequest { get; set; }

    public override string ToString()
    {
        return $"Move: {Move}, Ask: {Ask}, Response: {Response}, Discover: {Discovery}," +
            $" PickPiece: {Pickup}, CheckPiece: {CheckForSham}, DestroyPiece: {DestroyPiece}, PutPiece: {PutPiece}, PrematureRequest:{PrematureRequest} ";
    }
}
