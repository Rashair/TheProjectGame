using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Shared;
using Shared.Senders;
using System;

namespace GameMaster.Models
{
    public class GMPlayer
    {
        private int id;
        private int messageCorrelationId;
        public Team team;
        private bool isLeader;
        private AbstractPiece holding;
        private AbstractField position;
        private DateTime LockedTill;
        private ISender messageService;

        public string SocketID;

        public GMPlayer(int _id, Team _team)
        {
            id = _id;
            team = _team;
        }

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