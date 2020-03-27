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

        public int DiscoverPenalty { get; set; }

        public int PutPenalty { get; set; }

        public int CheckPenalty { get; set; }

        public int ResponsePenalty { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int GoalAreaHeight { get; set; }

        public int NumberOfGoals { get; set; }

        public int MaximumNumberOfPiecesOnBoard { get; set; }

        public int NumberOfPlayersPerTeam { get; set; }

        /// <summary>
        /// Percentage, between 0 and 100.
        /// </summary>
        public int ShamPieceProbability { get; set; }

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
            CsIP = conf.CsIP;
            CsPort = conf.CsPort;
            MovePenalty = conf.MovePenalty;
            AskPenalty = conf.AskPenalty;
            DiscoverPenalty = conf.DiscoverPenalty;
            PutPenalty = conf.PutPenalty;
            CheckPenalty = conf.CheckPenalty;
            ResponsePenalty = conf.ResponsePenalty;
            Width = conf.Width;
            Height = conf.Height;
            GoalAreaHeight = conf.GoalAreaHeight;
            NumberOfGoals = conf.NumberOfGoals;
            MaximumNumberOfPiecesOnBoard = conf.MaximumNumberOfPiecesOnBoard;
            NumberOfPlayersPerTeam = conf.NumberOfPlayersPerTeam;
            ShamPieceProbability = conf.ShamPieceProbability;
        }
    }
}
