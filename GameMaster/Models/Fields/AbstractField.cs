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

        public AbstractField(int _x, int _y)
        {
            x = _x;
            y = _y;
            whosHere = null;
            pieces = new HashSet<AbstractPiece>();
        }

        public void Leave(GMPlayer player)
        {
            if (whosHere != null) whosHere = null;
        }
        // oryginalnie zwracało void co jest niezgodne
        public abstract bool PickUp(GMPlayer player);

        public abstract bool Put(AbstractPiece piece);

        public abstract bool PutSham(AbstractPiece piece);

        public bool MoveHere(GMPlayer player)
        {
            if (whosHere == null && player != null)
            {
                player.Move(this); whosHere = player; return true;
            }
            else
            {
                return false;
            }
        }

        public bool ContainsPieces()
        {
            return (pieces.Count > 0);
        }

        public int[] GetPosition()
        {
            return new int[2] { x, y };
        }
    }
}