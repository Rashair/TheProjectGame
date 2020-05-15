namespace Shared.Models
{
    public class Penalties
    {
        public int Move { get; set; }

        public int Ask { get; set; }

        public int Response { get; set; }

        public int Discover { get; set; }

        public int PickPiece { get; set; }

        public int CheckPiece { get; set; }

        public int DestroyPiece { get; set; }

        public int PutPiece { get; set; }

        public override string ToString()
        {
            return $"Move: {Move}, Ask: {Ask}, Response: {Response}, Discover: {Discover}," +
                $" PickPiece: {PickPiece}, CheckPiece: {CheckPiece}, DestroyPiece: {DestroyPiece}, PutPiece: {PutPiece}";
        }
    }
}
