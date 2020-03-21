using GameMaster.Models.Pieces;
using System;

namespace GameMaster.Models.Fields
{
    public class GoalField : AbstractField
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