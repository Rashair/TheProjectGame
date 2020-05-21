using GameMaster.Models.Pieces;
using Shared.Enums;

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

        public override (PutEvent putEvent, bool wasPieceRemoved) Put(AbstractPiece piece)
        {
            if (piece.CheckForSham() == false)
            {
                Pieces.Add(piece);
                return (PutEvent.NormalOnNonGoalField, true);
            }
            else
            {
                Pieces.Add(piece);
                return (PutEvent.ShamOnGoalArea, true);
            }
        }

        public override bool CanPick()
        {
            return false;
        }
    }
}
