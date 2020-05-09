using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
            string message = $" PlayerId:{PlayerId}, TeamId:{TeamId}, LeaderId:{LeaderId}, ";
            message += $"BoardSize:{BoardSize.X}x{BoardSize.Y}, GoalAreaSize:{GoalAreaSize}";
            message += $"NumberOfAllies:{NumberOfPlayers.Allies},NumberOfEnemies:{NumberOfPlayers.Enemies},";
            message += $" NumberOfPieces{NumberOfPieces}, NumberOfGoals:{NumberOfGoals},\n";
            var penalty = Penalties;
            message += "Penalties{" + $" Ask:{penalty.Ask}, CheckPiece:{penalty.CheckPiece}, DestroyPiece {penalty.DestroyPiece}, ";
            message += $"Discover:{penalty.Discover}, Move:{penalty.Move}, PickPiece {penalty.PickPiece}, ";
            message += $"PutPiece:{penalty.PutPiece}, Response:{penalty.Response}" + "}";

            message += $"\nShamPieceProbability:{ShamPieceProbability}, Position:({Position.X},{Position.Y})\n";
            message += "AlliesIds:";
            if (AlliesIds != null)
            {
                for (int i = 0; i < AlliesIds.Length; i++)
                    message += $"{AlliesIds[i]}, ";
                message += "\n";
            }
            else
            {
                message += "null, ";
            }

            message += "EnemiesIds:";
            if (EnemiesIds != null)
            {
                for (int i = 0; i < EnemiesIds.Length; i++)
                    message += $"{EnemiesIds[i]}, ";
            }
            else
            {
                message += "null, ";
            }
            return message;
        }
    }
}
