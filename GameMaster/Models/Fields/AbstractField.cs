using System.Collections.Generic;

using GameMaster.Models.Pieces;
using Shared.Enums;

namespace GameMaster.Models.Fields;

public abstract class AbstractField
{
    private readonly int y;
    private readonly int x;
    private GMPlayer whosHere;

    protected HashSet<AbstractPiece> Pieces { get; set; }

    public int PiecesCount => Pieces.Count;

    public AbstractField(int y, int x)
    {
        this.y = y;
        this.x = x;
        Pieces = new HashSet<AbstractPiece>();
    }

    public void Leave(GMPlayer player)
    {
        whosHere = null;
    }

    public abstract bool PickUp(GMPlayer player);

    public abstract (PutEvent putEvent, bool wasPieceRemoved) Put(AbstractPiece piece);

    public abstract bool CanPick();

    public bool MoveHere(GMPlayer player)
    {
        if (whosHere == null && player != null)
        {
            player.Position = this;
            whosHere = player;
            return true;
        }

        return false;
    }

    public bool ContainsPieces()
    {
        return Pieces.Count > 0;
    }

    public int[] GetPosition()
    {
        return new int[2] { y, x };
    }
}
