using GameMaster.Models.Fields;

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
            return abstractField.PutFake(this);
        }
    }
}
