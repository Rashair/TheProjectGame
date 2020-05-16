using System.Text;

using Newtonsoft.Json;
using Shared.Enums;
using Shared.Models;

namespace Shared.Payloads.GMPayloads
{
    public class StartGamePayload : Payload
    {
        public int AgentID { get; set; }

        public int[] AlliesIds { get; set; }

        public int LeaderId { get; set; }

        public int[] EnemiesIds { get; set; }

        [JsonProperty("teamId")]
        public Team TeamId { get; set; }

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
            StringBuilder message = new StringBuilder($"\nAgentID:{AgentID}, TeamId:{TeamId}, LeaderId:{LeaderId},\n");
            message.Append($"BoardSize:{BoardSize.X}x{BoardSize.Y}, GoalAreaSize:{GoalAreaSize}\n");
            message.Append($"NumberOfAllies:{NumberOfPlayers.Allies}, NumberOfEnemies:{NumberOfPlayers.Enemies},\n");
            message.Append($"NumberOfPieces{NumberOfPieces}, NumberOfGoals:{NumberOfGoals},\n");
            var penalty = Penalties;

            message.Append("Penalties: {" + $"Move:{penalty.Move}, Ask:{penalty.Ask}, Response:{penalty.Response}, ");
            message.Append($"Discover:{penalty.Discover}, PrematureRequest{penalty.PrematureRequest}, PickupPiece {penalty.PickUpPiece}, CheckPiece:{penalty.CheckPiece}, ");
            message.AppendLine($"PutPiece:{penalty.PutPiece}, DestroyPiece {penalty.DestroyPiece}}}");

            message.AppendLine($"ShamPieceProbability:{ShamPieceProbability}, Position:({Position.X},{Position.Y})");
            message.Append("AlliesIds:");
            if (AlliesIds != null)
            {
                for (int i = 0; i < AlliesIds.Length; i++)
                    message.Append($"{AlliesIds[i]}, ");
            }
            else
            {
                message.Append("null, ");
            }

            message.Append("\nEnemiesIds:");
            if (EnemiesIds != null)
            {
                for (int i = 0; i < EnemiesIds.Length; i++)
                    message.Append($"{EnemiesIds[i]}, ");
            }
            else
            {
                message.Append("null, ");
            }
            return message.ToString();
        }
    }
}
