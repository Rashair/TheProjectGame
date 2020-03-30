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

        public override (bool, bool) PutOnField(AbstractField abstractField)
        {
            Log.ForContext<ShamPiece>().Information($"Putting sham on {abstractField.GetPosition()}");
            return abstractField.PutFake(this);
        }
    }
}
