﻿using GameMaster.Models.Pieces;

namespace GameMaster.Models.Fields
{
    public class GoalField : AbstractField
    {
        public GoalField(int y, int x)
            : base(y, x)
        {
        }

        public override bool PickUp(GMPlayer player)
        {
            return false;
        }

        public override bool Put(AbstractPiece piece)
        {
            if (this.ContainsPieces() == false)
            {
                this.Pieces.Add(piece);
                return true;
            }

            return false;
        }

        public override bool PutSham(AbstractPiece piece)
        {
            Pieces.Add(piece);
            return true;
        }

        public override (bool?, bool) PutNormal(AbstractPiece piece)
        {
            bool goal = Put(piece);
            return (goal, true);
        }

        public override (bool? goal, bool removed) PutFake(AbstractPiece piece)
        {
            PutSham(piece);
            return (null, true);
        }

        public override bool CanPick()
        {
            return false;
        }
    }
}
