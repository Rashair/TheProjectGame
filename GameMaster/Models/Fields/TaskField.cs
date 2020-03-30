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
                player.Holding = Pieces.ElementAt(0);
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

        public override (bool, bool) PutNormal(AbstractPiece piece)
        {
            Put(piece);
            return (false, false);
        }

        public override (bool goal, bool removed) PutFake(AbstractPiece piece)
        {
            PutSham(piece);
            return (false, false);
        }
    }
}
