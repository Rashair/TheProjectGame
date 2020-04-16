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

        public int PickPenalty { get; set; }

        public int CheckPenalty { get; set; }

        public int PutPenalty { get; set; }

        public int DestroyPenalty { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int NumberOfPlayersPerTeam { get; set; }

        public int GoalAreaHeight { get; set; }

        public int NumberOfGoals { get; set; }

        public int NumberOfPiecesOnBoard { get; set; }

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
                   CsIP == configuration.CsIP &&
                   CsPort == configuration.CsPort &&
                   MovePenalty == configuration.MovePenalty &&
                   AskPenalty == configuration.AskPenalty &&
                   ResponsePenalty == configuration.ResponsePenalty &&
                   DiscoverPenalty == configuration.DiscoverPenalty &&
                   PickPenalty == configuration.PickPenalty &&
                   CheckPenalty == configuration.CheckPenalty &&
                   PutPenalty == configuration.PutPenalty &&
                   DestroyPenalty == configuration.DestroyPenalty &&
                   Width == configuration.Width &&
                   Height == configuration.Height &&
                   GoalAreaHeight == configuration.GoalAreaHeight &&
                   NumberOfGoals == configuration.NumberOfGoals &&
                   NumberOfPiecesOnBoard == configuration.NumberOfPiecesOnBoard &&
                   ShamPieceProbability == configuration.ShamPieceProbability &&
                   NumberOfPlayersPerTeam == configuration.NumberOfPlayersPerTeam;
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
            hash.Add(PickPenalty);
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
            return hash.ToHashCode();
        }
    }
}
