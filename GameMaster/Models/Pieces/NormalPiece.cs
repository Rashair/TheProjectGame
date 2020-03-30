using GameMaster.Models.Fields;

namespace GameMaster.Models.Pieces
{
    public class NormalPiece : AbstractPiece
    {
        public override bool CheckForSham()
        {
            return false;
        }

        public override (bool, bool) PutOnField(AbstractField abstractField)
        {
            return abstractField.PutNormal(this);
        }
    }
}
