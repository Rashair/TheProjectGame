using System;
using GameMaster.Models.Pieces;

namespace GameMaster.Models.Fields
{
    internal class GoalField : AbstractField
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