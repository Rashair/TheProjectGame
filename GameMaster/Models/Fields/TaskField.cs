using System.Linq;

using GameMaster.Models.Pieces;

namespace GameMaster.Models.Fields
{
    public class TaskField : AbstractField
    {
        public TaskField(int x, int y)
            : base(x, y)
        {
        }

        public override bool PickUp(GMPlayer player)
        {
            if (this.ContainsPieces())
            {
                player.SetHolding(Pieces.ElementAt(0));
                Pieces.Remove(Pieces.ElementAt(0));
                return true;
            }

            return false;
        }

        public override bool Put(AbstractPiece piece)
        {
            return Pieces.Add(piece);
        }

        public override bool PutSham(AbstractPiece piece)
        {
            Pieces.Add(piece);
            return false;
        }
    }
}
