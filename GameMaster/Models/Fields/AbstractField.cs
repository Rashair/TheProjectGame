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
        private HashSet<AbstractPiece> pieces;

        public void Leave(GMPlayer player)
        {
            throw new NotImplementedException();
        }

        public abstract void PickUp(GMPlayer player);

        public abstract bool Put(AbstractPiece piece);

        public bool MoveHere(GMPlayer player)
        {
            throw new NotImplementedException();
        }

        public bool ContainsPieces()
        {
            throw new NotImplementedException();
        }

        public int[] GetPosition()
        {
            throw new NotImplementedException();
        }
    }
}