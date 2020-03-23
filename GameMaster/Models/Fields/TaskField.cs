using System.Linq;

using GameMaster.Models.Pieces;

namespace GameMaster.Models.Fields
{
    public class TaskField : AbstractField
    {
        public TaskField(int x, int y) : base(x, y)
        {
        }

        public override bool PickUp(GMPlayer player)
        {
            if (this.ContainsPieces())
            {
                player.SetHolding(pieces.ElementAt(0));
                pieces.Remove(pieces.ElementAt(0));
                return true;
            }
            return false;
        }

        public override bool Put(AbstractPiece piece)
        {
            return pieces.Add(piece);
        }

        public override bool PutSham(AbstractPiece piece)
        {
            pieces.Add(piece);
            return false;
        }
    }
}
