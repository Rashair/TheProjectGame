using GameMaster.Models.Pieces;
using System;

namespace GameMaster.Models.Fields
{
    public class NonGoalField : AbstractField
    {
        public NonGoalField(int _x, int _y) : base(_x, _y) { }
        public override bool PickUp(GMPlayer player)
        {
            return false;
        }

        public override bool Put(AbstractPiece piece)
        {
            pieces.Add(piece); return false;
        }

        public override bool PutSham(AbstractPiece piece)
        {
            pieces.Add(piece);
            return true;
        }
    }
}