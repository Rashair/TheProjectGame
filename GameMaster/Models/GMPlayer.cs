using System;

using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Shared.Enums;
using Shared.Senders;

namespace GameMaster.Models
{
    public class GMPlayer
    {
        private int id;
        private int messageCorrelationId;
        private AbstractPiece holding;
        private AbstractField position;
        private DateTime lockedTill;
        private ISender messageService;

        public bool HasPiece
        {
            get => holding != null;
        }

        public bool IsLeader { get; private set; }

        public Team Team { get; private set; }

        public string SocketID { get; set; }

        public GMPlayer(int id, Team team)
        {
            this.id = id;
            this.Team = team;
        }

        public bool TryLock(TimeSpan timeSpan)
        {
            throw new NotImplementedException();
        }

        public void Move(AbstractField field)
        {
            position = field;
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

        /// <summary>
        /// Sends message to player through ISender interface
        /// </summary>
        /// <returns></returns>
        public bool Put()
        {
            throw new NotImplementedException();
        }

        internal void SetHolding(AbstractPiece piece)
        {
            holding = piece;
        }

        internal int[] GetPosition()
        {
            throw new NotImplementedException();
        }    
    }
}
