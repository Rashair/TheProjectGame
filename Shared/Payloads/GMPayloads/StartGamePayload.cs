using System.Text;

using Shared.Enums;
using Shared.Models;

namespace Shared.Payloads.GMPayloads
{
    public class StartGamePayload : Payload
    {
        public int AgentID { get; set; }

        public int[] AlliesIDs { get; set; }

        public int LeaderID { get; set; }

        public int[] EnemiesIDs { get; set; }

        public Team TeamID { get; set; }

        public BoardSize BoardSize { get; set; }

        public int GoalAreaSize { get; set; }

        public NumberOfPlayers NumberOfPlayers { get; set; }

        public int NumberOfPieces { get; set; }

        public int NumberOfGoals { get; set; }

        public Penalties Penalties { get; set; }

        public float ShamPieceProbability { get; set; }

        public Position Position { get; set; }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder($"\nAgentID:{AgentID}, TeamID:{TeamID}, LeaderId:{LeaderID},\n");
            message.Append($"BoardSize:{BoardSize.X}x{BoardSize.Y}, GoalAreaSize:{GoalAreaSize}\n");
            message.Append($"NumberOfAllies:{NumberOfPlayers.Allies}, NumberOfEnemies:{NumberOfPlayers.Enemies},\n");
            message.Append($"NumberOfPieces{NumberOfPieces}, NumberOfGoals:{NumberOfGoals},\n");
            var penalty = Penalties;

            message.Append("Penalties: {" + $"Move:{penalty.Move}, Ask:{penalty.Ask}, Response:{penalty.Response}, ");
            message.Append($"Discover:{penalty.Discover}, PrematureRequest{penalty.PrematureRequest}, PickupPiece {penalty.PickupPiece}, CheckPiece:{penalty.CheckPiece}, ");
            message.AppendLine($"PutPiece:{penalty.PutPiece}, DestroyPiece {penalty.DestroyPiece}}}");

            message.AppendLine($"ShamPieceProbability:{ShamPieceProbability}, Position:({Position.X},{Position.Y})");
            message.Append("AlliesIds:");
            if (AlliesIDs != null)
            {
                for (int i = 0; i < AlliesIDs.Length; i++)
                {
                    message.Append($"{AlliesIDs[i]}, ");
                }
            }
            else
            {
                message.Append("null, ");
            }

            message.Append("\nEnemiesIds:");
            if (EnemiesIDs != null)
            {
                for (int i = 0; i < EnemiesIDs.Length; i++)
                {
                    message.Append($"{EnemiesIDs[i]}, ");
                }
            }
            else
            {
                message.Append("null, ");
            }
            return message.ToString();
        }
    }
}
