using System.Collections.Generic;

using GameMaster.Models.Pieces;

namespace GameMaster.Models.Fields
{
    public abstract class AbstractField
    {
        private readonly int x;
        private readonly int y;
        private GMPlayer whosHere;
        protected HashSet<AbstractPiece> pieces;

        public AbstractField(int x, int y)
        {
            this.x = x;
            this.y = y;
            pieces = new HashSet<AbstractPiece>();
        }

        public void Leave(GMPlayer player)
        {
            whosHere = null;
        }

        // originally returned void
        public abstract bool PickUp(GMPlayer player);

        public abstract bool Put(AbstractPiece piece);

        public abstract bool PutSham(AbstractPiece piece);

        public bool MoveHere(GMPlayer player)
        {
            if (whosHere == null && player != null)
            {
                player.Move(this);
                whosHere = player;
                return true;
            }
            return false;
        }

        public bool ContainsPieces()
        {
            return pieces.Count > 0;
        }

        public int[] GetPosition()
        {
            return new int[2] { x, y };
        }
    }
}
