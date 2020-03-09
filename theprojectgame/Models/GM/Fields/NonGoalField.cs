using System;
using TheProjectGame.Models.GM.Pieces;

namespace TheProjectGame.Models.GM.Fields
{
    public class NonGoalField : AbstractField
    {
        public override void PickUp(GMPlayer player)
        {
            throw new NotImplementedException();
        }

        public override bool Put(AbstractPiece piece)
        {
            throw new NotImplementedException();
        }
    }
}