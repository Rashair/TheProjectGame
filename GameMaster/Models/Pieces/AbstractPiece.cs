using GameMaster.Models.Fields;

namespace GameMaster.Models.Pieces
{
    public abstract class AbstractPiece
    {
        public abstract bool CheckForSham();

        public abstract bool Put(AbstractField abstractField);
    }
}