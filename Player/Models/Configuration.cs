using Player.Models.Strategies;
using Shared.Enums;

namespace Player.Models
{
    public class Configuration
    {
        public Team Team { get; set; }

        public IStrategy Strategy { get; set; }

        public void Update(Configuration conf)
        {
            Team = conf.Team;
            Strategy = conf.Strategy;
        }
    }
}
