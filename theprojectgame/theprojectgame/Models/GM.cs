using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace theprojectgame.Models.GM
{
    public class ShamPiece: AbstractPiece
    {
        public override bool CheckForSham()
        {
            throw new NotImplementedException();
        }
        public override bool Put(AbstractField abstractField)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class AbstractPiece
    {
        public abstract bool CheckForSham();
        public abstract bool Put(AbstractField abstractField);
    }

    public class TaskField : AbstractField
    {
        public override void PickUp(Player player)
        {
            throw new NotImplementedException();
        }
        public override bool Put(AbstractPiece piece)
        {
            throw new NotImplementedException();
        }
    }

    public class NonGoalField : AbstractField
    {
        public override void PickUp(Player player)
        {
            throw new NotImplementedException();
        }
        public override bool Put(AbstractPiece piece)
        {
            throw new NotImplementedException();
        }
    }

    class GoalField : AbstractField
    {
        public override void PickUp(Player player)
        {
            throw new NotImplementedException();
        }
        public override bool Put(AbstractPiece piece)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class AbstractField
    {
        private readonly int x;
        private readonly int y;
        private Player whosHere;
        private HashSet<AbstractPiece> pieces;

        public void Leave(Player player)
        {
            throw new NotImplementedException();
        }
        public abstract void PickUp(Player player);
        public abstract bool Put(AbstractPiece piece);
        public bool MoveHere(Player player)
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

    public enum Team { Red, Blue };

    public interface MessageSenderService
    {
        void SendMessage();
    }

    public class Player
    {
        private int id;
        private int messageCorrelationId;
        private Team team;
        private bool isLeader;
        private AbstractPiece holding;
        private AbstractField position;
        private DateTime LockedTill;
        private MessageSenderService messageService;

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

    public class Configuration
    {
        public readonly TimeSpan movePenalty;
        public readonly TimeSpan askPenalty;
        public readonly TimeSpan discoverPenalty;
        public readonly TimeSpan putPenalty;
        public readonly TimeSpan checkPenalty;
        public readonly TimeSpan responsePenalty;
        public readonly int x;
        public readonly int y;
        public readonly int numberOfGoals;
    }
    public class GM
    {
        private readonly Dictionary<int, Player> players;
        private readonly AbstractField[][] map;
        private static int[] legalKnowledgeReplies = new int[2]; //uznane ze oznaczenie unique jest rowne static
        private Configuration conf;
        internal int redTeamPoints;
        internal int blueTeamPoints;

        public void AcceptMessage()
        {
            throw new NotImplementedException();
        }
        public void GenerateGUI()
        {
            throw new NotImplementedException();
        }
        internal void Discover(AbstractField field)
        {
            throw new NotImplementedException();
        }
        internal void EndGame()
        {
            throw new NotImplementedException();
        }
        private void GeneratePiece()
        {
            throw new NotImplementedException();
        }
        private void ForwardKnowledgeQuestion()
        {
            throw new NotImplementedException();
        }
        private void ForwardKnowledgeReply()
        {
            throw new NotImplementedException();
        }
    }
}