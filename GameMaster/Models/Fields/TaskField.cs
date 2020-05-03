using System.Linq;

using GameMaster.Models.Pieces;

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

        public override (bool? goal, bool removed) Put(AbstractPiece piece)
        {
            if (piece.CheckForSham() == false)
            {
                Pieces.Add(piece);
                return (false, false);
            }
            else
            {
                Pieces.Add(piece);
                return (null, false);
            }
        }

        public override bool CanPick()
        {
            return true;
        }
    }
}
