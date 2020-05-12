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

        public bool Verbose { get; set; }

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
            hash.Add(Verbose);
            return hash.ToHashCode();
        }

        public (bool, string) IsValid()
        {
            if (Width < 1)
            {
                return (false, "Width must be greater than or equal to 1");
            }

            if (Height < 3)
            {
                return (false, "Height must be greater than or equal to 3");
            }

            if (GoalAreaHeight < 1 || GoalAreaHeight > Height / 2)
            {
                return (false, "GoalAreaHeight must be in range [1, Height / 2]");
            }

            if (NumberOfGoals <= 0 || NumberOfGoals > GoalAreaHeight * Width)
            {
                return (false, "NumberOfGoals must be in range (0,  GoalAreaHeight * Width]");
            }

            if (NumberOfPlayersPerTeam <= 0 || NumberOfPlayersPerTeam > Height * Width)
            {
                return (false, "NumberOfPlayersPerTeam must be in range (0, Height * Width]");
            }

            if (NumberOfPiecesOnBoard <= 0 ||
                NumberOfPiecesOnBoard > (Height - (GoalAreaHeight * 2)) * Width)
            {
                return (false, "NumberOfPiecesOnBoard must be in range (0, (Height - (GoalAreaHeight * 2)) * Width]");
            }

            if (ShamPieceProbability <= 0 || ShamPieceProbability >= 1)
            {
                return (false, "ShamPieceProbability must be in range (0, 1)");
            }

            int[] penalties = new int[]
            {
                AskPenalty,
                CheckPenalty,
                DestroyPenalty,
                DiscoverPenalty,
                MovePenalty,
                PickPenalty,
                PutPenalty,
                ResponsePenalty
            };
            foreach (int penalty in penalties)
            {
                if (penalty <= 0)
                {
                    return (false, "Every penalty must be greater than 0");
                }
            }

            return (true, "");
        }
    }
}
