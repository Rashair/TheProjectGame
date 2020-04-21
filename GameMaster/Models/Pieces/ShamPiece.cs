using GameMaster.Models.Fields;
using Serilog;

namespace GameMaster.Models.Pieces
{
    public class ShamPiece : AbstractPiece
    {
        public override bool CheckForSham()
        {
            return true;
        }
    }
}
