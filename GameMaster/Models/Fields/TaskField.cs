using System.Linq;

using GameMaster.Models.Pieces;
using Shared.Enums;

namespace GameMaster.Models.Fields
{
    public class TaskField : AbstractField
    {
        public TaskField(int y, int x)
            : base(y, x)
        {
        }

        public override bool PickUp(GMPlayer player)
        {
            if (this.ContainsPieces())
            {
                var piece = Pieces.First();
                player.Holding = piece;
                Pieces.Remove(piece);
                return true;
            }

            return false;
        }

        public override (PutEvent putEvent, bool removed) Put(AbstractPiece piece)
        {
            Pieces.Add(piece);
            return (PutEvent.TaskField, false);
        }

        public override bool CanPick()
        {
            return true;
        }
    }
}
