using GameMaster.Models.Fields;
using Shared;
using System;
using System.Collections.Generic;
using GameMaster.Managers;
using System.Threading.Tasks.Dataflow;
using Shared.Models.Messages;
using Newtonsoft.Json;
using Shared.Models.Payloads;
using System.Threading.Tasks;

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

        private BufferBlock<AgentMessage> queue;
        private WebSocketManager<GMMessage> manager;

        public GM(BufferBlock<AgentMessage> _queue, WebSocketManager<GMMessage> _manager)
        {
            queue = _queue;
            manager = _manager;
        }

        public async void AcceptMessage()
        {
            AgentMessage message;
            if (queue.TryReceive(null, out message))
            {
                switch (message.messageID)
                {
                    case (int)MessageID.CheckPiece:
                        players[message.agentID].CheckHolding();
                        break;
                    case (int)MessageID.PieceDestruction:
                        players[message.agentID].DestroyHolding();
                        break;
                    case (int)MessageID.Discover:
                        players[message.agentID].Discover(this);
                        break;
                    case (int)MessageID.GiveInfo:
                        ForwardKnowledgeReply(message);
                        break;
                    case (int)MessageID.BegForInfo:
                        ForwardKnowledgeQuestion(message);
                        break;
                    case (int)MessageID.JoinTheGame:
                        JoinGamePayload payload1 = JsonConvert.DeserializeObject<JoinGamePayload>(message.payload);
                        int key = players.Count;
                        bool accepted = players.TryAdd(key, new GMPlayer(key, (Team)Enum.Parse(typeof(Team), payload1.teamID)));
                        GMMessage answer1 = new GMMessage();
                        answer1.id = 107;
                        JoinAnswerPayload answer1Payload = new JoinAnswerPayload();
                        answer1Payload.accepted = accepted;
                        answer1Payload.agentID = key;
                        answer1.payload = JsonConvert.SerializeObject(answer1Payload);
                        await manager.SendMessageAsync(players[key].SocketID, answer1);
                        break;
                    case (int)MessageID.Move:
                        MovePayload payload2 = JsonConvert.DeserializeObject<MovePayload>(message.payload);
                        AbstractField field = null;
                        int[] position1 = players[message.agentID].GetPosition();
                        switch ((Directions)Enum.Parse(typeof(Directions), payload2.direction))
                        {
                            case Directions.N:
                                if (position1[1] + 1 < map.GetLength(1)) field = map[position1[0]][position1[1] + 1];
                                break;
                            case Directions.S:
                                if (position1[1] - 1 >= 0) field = map[position1[0]][position1[1] - 1];
                                break;
                            case Directions.W:
                                if (position1[0] + 1 < map.GetLength(0)) field = map[position1[0] + 1][position1[1]];
                                break;
                            case Directions.E:
                                if (position1[0] - 1 >= 0) field = map[position1[0] - 1][position1[1]];
                                break;
                        }
                        players[message.agentID].Move(field);
                        break;
                    case (int)MessageID.Pick:
                        int[] position2 = players[message.agentID].GetPosition();
                        map[position2[0]][position2[1]].PickUp(players[message.agentID]);
                        GMMessage answer2 = new GMMessage();
                        answer2.id = 109;
                        EmptyAnswerPayload answer2Payload = new EmptyAnswerPayload();
                        answer2.payload = JsonConvert.SerializeObject(answer2Payload);
                        await manager.SendMessageAsync(players[message.agentID].SocketID, answer2);
                        break;
                    case (int)MessageID.Put:
                        bool point = players[message.agentID].Put();
                        if (point)
                        {
                            if (players[message.agentID].team == Team.Red) redTeamPoints++;
                            else blueTeamPoints++;
                        }
                        break;
                    default:
                        break;
                }
            }
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

        private async void ForwardKnowledgeQuestion(AgentMessage agentMessage)
        {
            throw new NotImplementedException();
        }

        private async void ForwardKnowledgeReply(AgentMessage agentMessage)
        {
            throw new NotImplementedException();
        }
    }
}