using GameMaster.Models.Fields;
using Shared;
using System;
using System.Collections.Generic;
using GameMaster.Managers;
using System.Threading.Tasks.Dataflow;
using Shared.Models.Messages;

namespace GameMaster.Models
{
    public class GM
    {
        private readonly Dictionary<int, GMPlayer> players;
        private readonly AbstractField[][] map;
        private static int[] legalKnowledgeReplies = new int[2]; // unique from documentation considered as static
        private Configuration conf;
        internal int redTeamPoints;
        internal int blueTeamPoints;

        private BufferBlock<GMMessage> queue;
        private WebSocketManager<GMMessage> manager;

        public GM(BufferBlock<GMMessage> _queue, WebSocketManager<GMMessage> _manager)
        {
            queue = _queue;
            manager = _manager;
        }

        public void AcceptMessage()
        {
            throw new NotImplementedException();
        }

        public void GenerateGUI()
        {
            throw new NotImplementedException();
        }

        internal Dictionary<Direction, int> Discover(AbstractField field)
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