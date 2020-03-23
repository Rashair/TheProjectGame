using GameMaster.Models.Pieces;

namespace GameMaster.Models.Fields
{
    public class NonGoalField : AbstractField
    {
        public NonGoalField(int x, int y) : base(x, y)
        {
        }

        public override bool PickUp(GMPlayer player)
        {
            return false;
        }

        public override bool Put(AbstractPiece piece)
        {
            pieces.Add(piece);
            return false;
        }

        public override bool PutSham(AbstractPiece piece)
        {
            pieces.Add(piece);
            return true;
        }
    }
}
