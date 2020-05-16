﻿using GameMaster.Models.Fields;
using Shared.Enums;

namespace GameMaster.Models.Pieces
{
    public abstract class AbstractPiece
    {
        public abstract bool CheckForSham();

        public (PutEvent putEvent, bool removed) Put(AbstractField field)
        {
            return field.Put(this);
        }
    }
}
