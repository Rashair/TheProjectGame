using GameMaster.Models.Fields;
using System;

namespace GameMaster.Models.Pieces
{
    public class ShamPiece : AbstractPiece
    {
        public override bool CheckForSham()
        {
            return true;
        }

        public override bool Put(AbstractField abstractField)
        {
            return abstractField.PutSham(this);
        }
    }
}