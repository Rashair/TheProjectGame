using TheProjectGame.Models.GM.Fields;

namespace TheProjectGame.Models.GM.Pieces
{
    public abstract class AbstractPiece
    {
        public abstract bool CheckForSham();

        public abstract bool Put(AbstractField abstractField);
    }
}