namespace Doublesidepoker.scripts;

public enum PokerGameState
{
    RoundStart,
    PlayersMandatoryBets,
    PlayersDrawCard,
    Player1Bet,
    Player1BetComplete,
    Player1GiveUp,
    Player2Bet,
    Player2BetComplete,
    Player2GiveUp,
    PlayersBetComplete,
    Player1RoundVictory,
    Player2RoundVictory,
    PlayerRoundDraw,
    GameEndVictoryCheck,
    Player1GameVictory,
    Player2GameVictory,
    GameError
}