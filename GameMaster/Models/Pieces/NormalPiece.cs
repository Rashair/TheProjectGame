using GameMaster.Models.Fields;
using System;

namespace GameMaster.Models.Pieces
{
    public class NormalPiece : AbstractPiece
    {
        public override bool CheckForSham()
        {
            return false;
        }

        public override bool Put(AbstractField abstractField)
        {
            return abstractField.Put(this);
        }
    }
}