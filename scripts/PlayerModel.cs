using System;

namespace Doublesidepoker.scripts;

public record PlayerModel
{
    public PokerCard? CardOnHand;
    public Guid PlayerId;
    public string PlayerName;
    public int? TokenOnBetBack;
    public int? TokenOnBetFront;
    public int TokenOnHand;
    public int TotalTokenOnBet;
}