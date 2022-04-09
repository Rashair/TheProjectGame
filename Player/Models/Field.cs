using Shared.Enums;

namespace Player.Models;

public class Field
{
    public GoalInfo GoalInfo { get; set; }

    public bool PlayerInfo { get; set; }

    public int DistToPiece { get; set; }
}
