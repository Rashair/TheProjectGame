using GameMaster.Models.Pieces;
using Shared.Enums;

namespace GameMaster.Models.Fields
{
    public class GoalField : AbstractField
    {
        public GoalField(int y, int x)
            : base(y, x)
        {
        }

        public override bool PickUp(GMPlayer player)
        {
            return false;
        }

        public override (PutEvent putEvent, bool removed) Put(AbstractPiece piece)
        {
            if (piece.CheckForSham() == false)
            {
                if (this.ContainsPieces() == false)
                {
                    this.Pieces.Add(piece);
                    return (PutEvent.NormalOnGoalField, true);
                }
                else
                {
                    return (PutEvent.NormalOnNonGoalField, true);
                }
            }
            else
            {
                return (PutEvent.ShamOnGoalArea, true);
            }
        }

        public override bool CanPick()
        {
            return false;
        }
    }
}
