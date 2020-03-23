using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Shared;
using System;
using System.Collections.Generic;
using GameMaster.Managers;
using Shared.Models.Messages;
using System.Threading.Tasks;
using Shared.Models.Payloads;
using Newtonsoft.Json;

namespace GameMaster.Models
{
    public class GM
    {
        private readonly Dictionary<int, GMPlayer> players;
        private readonly AbstractField[][] board;
        private static HashSet<(int, int)> legalKnowledgeReplies; // unique from documentation considered as static
        private Configuration conf;
        internal int redTeamPoints;
        internal int blueTeamPoints;

        private WebSocketManager<GMMessage> manager;

        public GM(Configuration conf, WebSocketManager<GMMessage> _manager)
        {
            this.conf = conf;
            board = new AbstractField[conf.Height][];
            for(int i = 0; i < board.Length; ++i)
            {
                board[i] = new AbstractField[conf.Width];
            }

            Func<AbstractField> nonGoalFieldGenerator = () => new NonGoalField();
            for (int rowIt = 0; rowIt < conf.GoalAreaHeight; ++rowIt)
            {
                FillBoardRow(rowIt, nonGoalFieldGenerator);
            }

            Func<AbstractField> taskFieldGenerator = () => new TaskField();
            int secondGoalAreaStart = conf.Height - conf.GoalAreaHeight;
            for (int rowIt = conf.GoalAreaHeight; rowIt < secondGoalAreaStart; ++rowIt)
            {
                FillBoardRow(rowIt, taskFieldGenerator);
            }

            for (int rowIt = secondGoalAreaStart; rowIt < conf.Height; ++rowIt)
            {
                FillBoardRow(rowIt, nonGoalFieldGenerator);
            }

            manager = _manager;

            // TODO : initialize rest
        }

        private void FillBoardRow(int row, Func<AbstractField> getField)
        {
            for(int col = 0; col < board[row].Length; ++col)
            {
                board[row][col] = getField();
            }
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
            var rand = new Random();
            bool isSham = rand.Next(0, 100) <= conf.ShamPieceProbability;
            AbstractPiece piece;
            if (isSham)
            {
                piece = new ShamPiece();
            }
            else
            {
                piece = new NormalPiece();
            }

            int taskAreaStart = conf.GoalAreaHeight;
            int taskAreaEnd = conf.Height - conf.GoalAreaHeight;
            int xCoord = rand.Next(taskAreaStart, taskAreaEnd);
            int yCoord = rand.Next(0, conf.Width);

            board[xCoord][yCoord].Put(piece);
        }

        private void ForwardKnowledgeQuestion()
        {
            throw new NotImplementedException();
        }

        private async void ForwardKnowledgeReply(AgentMessage message)
        {
            GiveInfoPayload payload = JsonConvert.DeserializeObject<GiveInfoPayload>(message.payload);
            if (legalKnowledgeReplies.Contains((message.agentID, payload.respondToID)))
            {
                legalKnowledgeReplies.Remove((message.agentID, payload.respondToID));
                GMMessage answer = new GMMessage();
                GiveInfoForwardedPayload answerPayload = new GiveInfoForwardedPayload();
                answerPayload.answeringID = message.agentID;
                answerPayload.distances = payload.distances;
                answerPayload.redTeamGoalAreaInformations = payload.redTeamGoalAreaInformations;
                answerPayload.blueTeamGoalAreaInformations = payload.blueTeamGoalAreaInformations;
                await manager.SendMessageAsync(payload.respondToID.ToString(), answer);
            }
        }
    }
}