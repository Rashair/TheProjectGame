using GameMaster.Models.Pieces;
using System;
using System.Linq;

namespace GameMaster.Models.Fields
{
    public class TaskField : AbstractField
    {
        public override void PickUp(GMPlayer player)
        {
            var piece = pieces.First();
            player.SetHolding(piece);
            pieces.Remove(piece);
        }

        public override bool Put(AbstractPiece piece)
        {
            this.pieces.Add(piece);
            return false;
        }

        public override bool PutSham(AbstractPiece piece)
        {
            throw new NotImplementedException();
        }
    }
}