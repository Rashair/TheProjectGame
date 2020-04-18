﻿using GameMaster.Models.Fields;

namespace GameMaster.Models.Pieces
{
    public abstract class AbstractPiece
    {
        public abstract bool CheckForSham();

        public bool Put(AbstractField abstractField)
        {
            return abstractField.Put(this);
        }

        // TODO Temporary fix Put()
        public abstract (bool? goal, bool removed) PutOnField(AbstractField abstractField);
    }
}
