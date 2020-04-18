using GameMaster.Models.Fields;

namespace GameMaster.Models.Pieces
{
    public abstract class AbstractPiece
    {
        public abstract bool CheckForSham();

        public (bool goal, bool removed) Put(AbstractField abstractField)
        {
            return abstractField.Put(this);
        }
    }
}
