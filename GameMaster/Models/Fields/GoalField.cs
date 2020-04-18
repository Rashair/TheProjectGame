using GameMaster.Models.Pieces;

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

        public override (bool, bool) Put(AbstractPiece piece)
        {
            if (piece.CheckForSham() == false)
            {
                {
                    if (this.ContainsPieces() == false)
                    {
                        this.Pieces.Add(piece);

                        return (true, true);
                    }
                    else
                    {
                        return (false, true);
                    }
                }
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
