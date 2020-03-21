using GameMaster.Models.Pieces;
using System;
using System.Collections.Generic;

namespace GameMaster.Models.Fields
{
    public abstract class AbstractField
    {
        private readonly int x;
        private readonly int y;
        private GMPlayer whosHere;
        protected HashSet<AbstractPiece> pieces;

        public AbstractField()
        {
            pieces = new HashSet<AbstractPiece>();
        }

        public void Leave(GMPlayer player)
        {
            throw new NotImplementedException();
        }

        public abstract void PickUp(GMPlayer player);

        public abstract bool Put(AbstractPiece piece);

        public abstract bool PutSham(AbstractPiece piece);

        public bool MoveHere(GMPlayer player)
        {
            throw new NotImplementedException();
        }

        public bool ContainsPieces()
        {
            return pieces.Count > 0;
        }

        public int[] GetPosition()
        {
            throw new NotImplementedException();
        }
    }
}