using GameMaster.Models.Pieces;

namespace GameMaster.Models.Fields
{
    public class NonGoalField : AbstractField
    {
        public NonGoalField(int y, int x)
            : base(y, x)
        {
        }

        public override bool PickUp(GMPlayer player)
        {
            return false;
        }

        public override bool Put(AbstractPiece piece)
        {
            Pieces.Add(piece);
            return false;
        }

        public override bool PutSham(AbstractPiece piece)
        {
            Pieces.Add(piece);
            return true;
        }

        public override (bool, bool) PutNormal(AbstractPiece piece)
        {
            Put(piece);
            return (false, true);
        }

        public override (bool goal, bool removed) PutFake(AbstractPiece piece)
        {
            PutSham(piece);
            return (false, true);
        }

        public override bool CanPick()
        {
            return false;
        }
    }
}
