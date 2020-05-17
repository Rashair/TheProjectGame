using Shared.Enums;

namespace Player.Models
{
    public class PlayerConfiguration
    {
        public string CsIP { get; set; }

        public int CsPort { get; set; }

        public Team TeamId { get; set; }

        public int Strategy { get; set; }

        public bool Verbose { get; set; }
    }
}
