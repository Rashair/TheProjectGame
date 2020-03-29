using System.Collections.Generic;

using GameMaster.Models.Pieces;
using Shared.Models;

namespace GameMaster.Models.Fields
{
    public abstract class AbstractField
    {
        private readonly int x;
        private readonly int y;
        private GMPlayer whosHere;

        protected HashSet<AbstractPiece> Pieces { get; set; }

        public AbstractField(int x, int y)
        {
            this.x = x;
            this.y = y;
            Pieces = new HashSet<AbstractPiece>();
        }

        public void Leave(GMPlayer player)
        {
            whosHere = null;
        }

        // TODO Temporary fix Put()
        public abstract (bool goal, bool removed) PutNormal(AbstractPiece piece);

        // TODO Temporary fix Put()
        public abstract (bool goal, bool removed) PutFake(AbstractPiece piece);

        // originally returned void
        public abstract bool PickUp(GMPlayer player);

        public abstract bool Put(AbstractPiece piece);

        public abstract bool PutSham(AbstractPiece piece);

        public bool MoveHere(GMPlayer player)
        {
            if (whosHere == null && player != null)
            {
                player.Position = this;
                whosHere = player;
                return true;
            }

            return false;
        }

        public bool ContainsPieces()
        {
            return Pieces.Count > 0;
        }

        public int[] GetPosition()
        {
            return new int[2] { x, y };
        }

        public Position GetPositionObject()
        {
            return new Position()
            {
                X = x,
                Y = y,
            };
        }
    }
}
