using System;
using TheProjectGame.Models.GM.Fields;
using TheProjectGame.Models.GM.Pieces;
using TheProjectGame.Models.Shared;
using TheProjectGame.Models.Shared.Senders;

namespace TheProjectGame.Models.GM
{
    public class GMPlayer
    {
        private int id;
        private int messageCorrelationId;
        private Team team;
        private bool isLeader;
        private AbstractPiece holding;
        private AbstractField position;
        private DateTime LockedTill;
        private ISender messageService;

        public bool TryLock(TimeSpan timeSpan)
        {
            throw new NotImplementedException();
        }

        public void Move(AbstractField field)
        {
            throw new NotImplementedException();
        }

        public void DestroyHolding()
        {
            throw new NotImplementedException();
        }

        public void CheckHolding()
        {
            throw new NotImplementedException();
        }

        public void Discover(GM gm)
        {
            throw new NotImplementedException();
        }

        public bool Put()
        {
            throw new NotImplementedException();
        }

        internal void SetHolding(AbstractPiece piece)
        {
            throw new NotImplementedException();
        }

        internal int[] GetPosition()
        {
            throw new NotImplementedException();
        }
    }
}