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

        public override (bool goal, bool removed) Put(AbstractPiece piece)
        {
            if (piece.CheckForSham() == false)
            {
                Pieces.Add(piece);
                return (false, true);
            }
            else
            {
                Pieces.Add(piece);
                return (false, true);
            }
        }

        public override bool CanPick()
        {
            return false;
        }
    }
}
