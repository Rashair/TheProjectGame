using GameMaster.Models.Pieces;
using System;
using System.Linq;

namespace GameMaster.Models.Fields
{
    public class TaskField : AbstractField
    {
        public TaskField(int _x, int _y) : base(_x, _y) { }
        public override bool PickUp(GMPlayer player)
        {
            if (this.ContainsPieces())
            {

                player.SetHolding(pieces.ElementAt(0));
                pieces.Remove(pieces.ElementAt(0));
                return true;
            }
            else return false;

        }

        public override bool Put(AbstractPiece piece)
        {
            return pieces.Add(piece);
        }

        public override bool PutSham(AbstractPiece piece)
        {
            pieces.Add(piece);
            return false;
        }
    }
}