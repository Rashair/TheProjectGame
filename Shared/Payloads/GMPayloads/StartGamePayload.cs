using System.Text;

using Newtonsoft.Json;
using Shared.Enums;
using Shared.Models;

namespace Shared.Payloads.GMPayloads
{
    public class StartGamePayload : Payload
    {
        public int PlayerId { get; set; }

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
            StringBuilder message = new StringBuilder($" PlayerId:{PlayerId}, TeamId:{TeamId}, LeaderId:{LeaderId}, ");
            message.Append($"BoardSize:{BoardSize.X}x{BoardSize.Y}, GoalAreaSize:{GoalAreaSize}");
            message.Append($"NumberOfAllies:{NumberOfPlayers.Allies},NumberOfEnemies:{NumberOfPlayers.Enemies},");
            message.Append($" NumberOfPieces{NumberOfPieces}, NumberOfGoals:{NumberOfGoals},");
            var penalty = Penalties;
            message.AppendLine();
            message.Append("Penalties{" + $" Ask:{penalty.Ask}, CheckPiece:{penalty.CheckPiece}, DestroyPiece {penalty.DestroyPiece}, ");
            message.Append($"Discover:{penalty.Discover}, Move:{penalty.Move}, PickPiece {penalty.PickPiece}, ");
            message.AppendLine($"PutPiece:{penalty.PutPiece}, Response:{penalty.Response}" + "}");

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
            message.AppendLine();
            message.Append("EnemiesIds:");
            if (EnemiesIds != null)
            {
                for (int i = 0; i < EnemiesIds.Length; i++)
                    message.Append($"{EnemiesIds[i]}, ");
            }
            else
            {
                message.Append("null, ");
            }
            message.AppendLine();
            return message.ToString();
        }
    }
}
