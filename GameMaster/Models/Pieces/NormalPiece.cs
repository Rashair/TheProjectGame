using GameMaster.Models.Fields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameMaster.Models.Pieces
{
    public class NormalPiece : AbstractPiece
    {
        public override bool CheckForSham()
        {
            throw new NotImplementedException();
        }

        public override bool Put(AbstractField abstractField)
        {
            throw new NotImplementedException();
        }
    }
}
