using Shared.Models;

namespace Shared.Payloads.GMPayloads
{
    public class MoveAnswerPayload : Payload
    {
        public bool MadeMove { get; set; }

        public Position CurrentPosition { get; set; }

        public int ClosestPiece { get; set; }

        public override string ToString()
        {
            string message = $" Accepted:{ClosestPiece}, PlayerId:({CurrentPosition.X},{CurrentPosition.Y})";
            message += $"MadeMove:{MadeMove}";
            return message;
        }
    }
}
