using System.Collections.Generic;

namespace Doublesidepoker.scripts;

public record GameState
{
    public PlayerModel CurrentInitialActionPlayer;
    public PlayerModel Player1;
    public PlayerModel Player2;
    public List<PokerCard> PokerPool;
    public PokerGameState State;
    public int UnresolvedTokenPool;
}