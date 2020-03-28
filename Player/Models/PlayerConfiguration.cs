using Player.Models.Strategies;
using Shared.Enums;

namespace Player.Models
{
    public class PlayerConfiguration
    {
        public Team Team { get; set; }

        public IStrategy Strategy { get; set; }

        public void Update(PlayerConfiguration conf)
        {
            Team = conf.Team;
            Strategy = conf.Strategy;
        }
    }
}
