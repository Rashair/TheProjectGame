using System;
using System.IO;

using Newtonsoft.Json;

namespace GameMaster.Models
{
    public class GameConfiguration
    {
        public string CsIP { get; set; }

        public int CsPort { get; set; }

        public int MovePenalty { get; set; }

        public int AskPenalty { get; set; }

        public int ResponsePenalty { get; set; }

        public int DiscoverPenalty { get; set; }

        public int PickUpPenalty { get; set; }

        public int CheckPenalty { get; set; }

        public int PutPenalty { get; set; }

        public int DestroyPenalty { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int NumberOfPlayersPerTeam { get; set; }

        public int GoalAreaHeight { get; set; }

        public int NumberOfGoals { get; set; }

        public int NumberOfPiecesOnBoard { get; set; }

        public bool Verbose { get; set; }

        public int PrematureRequestPenalty { get; set; }

        /// <summary>
        /// Percentage, between 0 and 1.
        /// </summary>
        public float ShamPieceProbability { get; set; }

        public GameConfiguration()
        {
        }

        public GameConfiguration(string path)
        {
            using (var file = new StreamReader(path))
            {
                var json = file.ReadToEnd();
                var conf = JsonConvert.DeserializeObject<GameConfiguration>(json);
                this.Update(conf);
            }
        }

        public void Update(GameConfiguration conf)
        {
            conf.CopyProperties(this);
        }

        public override bool Equals(object obj)
        {
            return obj is GameConfiguration configuration &&
                this.AreAllPropertiesTheSame(configuration);
        }

        public override int GetHashCode()
        {
            HashCode hash = default;
            hash.Add(CsIP);
            hash.Add(CsPort);
            hash.Add(MovePenalty);
            hash.Add(AskPenalty);
            hash.Add(ResponsePenalty);
            hash.Add(DiscoverPenalty);
            hash.Add(PickUpPenalty);
            hash.Add(CheckPenalty);
            hash.Add(PutPenalty);
            hash.Add(DestroyPenalty);
            hash.Add(Width);
            hash.Add(Height);
            hash.Add(GoalAreaHeight);
            hash.Add(NumberOfGoals);
            hash.Add(NumberOfPiecesOnBoard);
            hash.Add(ShamPieceProbability);
            hash.Add(NumberOfPlayersPerTeam);
            hash.Add(Verbose);
            return hash.ToHashCode();
        }
    }
}
