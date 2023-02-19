using System;
using System.Collections.Generic;
using System.Linq;
using Doublesidepoker.scripts;
using Godot;

public partial class GameManager : Node2D
{
	static GameManager _gameManager;
	string _errorMessage;

	Player _player1Script;
	Player _player2Script;

	Random _random;

	GameState _state;

	public override void _Ready()
	{
		_gameManager = this;
	}

	public static GameManager GetGameManager()
	{
		return _gameManager;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_gameManager = this;
		if (_state is null)
		{
			Init();
		}
		else
		{
			_player1Script?.SetPlayer(_state.Player1);
			_player2Script?.SetPlayer(_state.Player2);
			var gameStateText = (GetNode("GameStateLabel/GameStateText") as Label)!;
			gameStateText.Text = _state.State.ToString();
			var tokenPoolText = (GetNode("TokenPoolLabel/Value") as Label)!;
			tokenPoolText.Text = _state.UnresolvedTokenPool.ToString();
		}

		StateTransition();
	}

	void Init()
	{
		_random = new Random();
		_errorMessage = "";
		_state = NewGame();
		_player1Script = GetNode("Player1") as Player;
		_player2Script = GetNode("Player2") as Player;
	}

	void StateTransition()
	{
		var startState = _state.State;
		_state.State = _state.State switch
		{
			PokerGameState.RoundStart => RoundStartCheck(),
			PokerGameState.PlayersMandatoryBets => MandatoryBets(),
			PokerGameState.PlayersDrawCard => DrawCard(),
			PokerGameState.Player1Bet => PokerGameState.Player1Bet,
			// wait till game transit to Player 1 Bet Complete
			PokerGameState.Player1BetComplete => _state.Player1.TotalTokenOnBet == _state.Player2.TotalTokenOnBet
				? PokerGameState.PlayersBetComplete
				: PokerGameState.Player2Bet,
			PokerGameState.Player1GiveUp => PokerGameState.Player2RoundVictory,
			PokerGameState.Player2Bet => PokerGameState.Player2Bet, // wait till game transit to Player 2 Bet Complete
			PokerGameState.Player2BetComplete => _state.Player1.TotalTokenOnBet == _state.Player2.TotalTokenOnBet
				? PokerGameState.PlayersBetComplete
				: PokerGameState.Player1Bet,
			PokerGameState.Player2GiveUp => PokerGameState.Player1RoundVictory,
			PokerGameState.PlayersBetComplete => CheckRoundResult(),
			PokerGameState.Player1RoundVictory => PlayerRoundVictory(_state.Player1, _state.Player2,
				PokerGameState.RoundStart),
			PokerGameState.Player2RoundVictory => PlayerRoundVictory(_state.Player2, _state.Player1,
				PokerGameState.RoundStart),
			PokerGameState.PlayerRoundDraw => RoundDraw(),
			PokerGameState.GameEndVictoryCheck => GameEndVictoryCheck(),
			PokerGameState.Player1GameVictory => PokerGameState.Player1GameVictory,
			PokerGameState.Player2GameVictory => PokerGameState.Player2GameVictory,
			PokerGameState.GameError => PokerGameState.GameError,
			_ => PokerGameState.GameError
		};
		var endState = _state.State;
		if (startState != endState)
		{
			var str = $"{startState} => {endState}";
			AppendEventLog(str);
		}
	}

	PokerGameState MandatoryBets()
	{
		_state.Player1.TokenOnHand -= 1;
		_state.Player2.TokenOnHand -= 1;
		_state.UnresolvedTokenPool += 2;
		// At each round start we switch current initial action player and check if the game has ended
		if (_state.Player1.TokenOnHand <= 0 || _state.Player2.TokenOnHand <= 0)
			return PokerGameState.GameEndVictoryCheck;
		return PokerGameState.PlayersDrawCard;
	}

	PokerGameState GameEndVictoryCheck()
	{
		if (_state.Player1.TokenOnHand == 0) return PokerGameState.Player2GameVictory;
		return PokerGameState.Player1GameVictory;
	}

	PokerGameState RoundDraw()
	{
		_state.UnresolvedTokenPool += _state.Player1.TotalTokenOnBet + _state.Player2.TotalTokenOnBet;
		ClearPlayerBetState(_state.Player1);
		ClearPlayerBetState(_state.Player2);
		return PokerGameState.RoundStart;
	}

	PokerGameState RoundStartCheck()
	{
		_state.CurrentInitialActionPlayer =
			_state.CurrentInitialActionPlayer.PlayerId == _state.Player1.PlayerId ? _state.Player2 : _state.Player1;
		return PokerGameState.PlayersMandatoryBets;
	}

	PokerGameState CheckRoundResult()
	{
		try
		{
			var player1BetNumber = SelectPlayerBetNumber(_state.Player1);
			var player2BetNumber = SelectPlayerBetNumber(_state.Player2);
			if (player1BetNumber > player2BetNumber) return PokerGameState.Player1RoundVictory;
			if (player2BetNumber > player1BetNumber) return PokerGameState.Player2RoundVictory;
			return PokerGameState.PlayerRoundDraw;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			_errorMessage = e.Message;
			return PokerGameState.GameError;
		}
	}

	int SelectPlayerBetNumber(PlayerModel player)
	{
		if (player.CardOnHand is null)
			throw new InvalidOperationException($"Error - Player {player.PlayerName} card is empty " +
												"while it should not. Please restart game");

		if (player.TokenOnBetBack is not null && player.TokenOnBetFront is not null)
			return Math.Min(player.CardOnHand.FrontValue, player.CardOnHand.BackValue);
		if (player.TokenOnBetFront is not null) return player.CardOnHand.FrontValue;
		if (player.TokenOnBetBack is not null) return player.CardOnHand.BackValue;
		throw new InvalidOperationException($"Error - Player {player.PlayerName} did not bet on " +
											"Either the front or the back of the card. Please restart the game");
	}

	PokerGameState PlayerRoundVictory(PlayerModel player, PlayerModel opponent, PokerGameState nextState)
	{
		player.TokenOnHand += player.TotalTokenOnBet + opponent.TotalTokenOnBet + _state.UnresolvedTokenPool;
		if (player.TokenOnBetBack != null && player.TokenOnBetFront != null)
		{
			// Bonus for bet on both
			player.TokenOnHand += 10;
			opponent.TokenOnHand -= 10;
		}

		ClearPlayerBetState(player);
		ClearPlayerBetState(opponent);
		_state.UnresolvedTokenPool = 0;
		return nextState;
	}

	void ClearPlayerBetState(PlayerModel player)
	{
		player.TotalTokenOnBet = 0;
		player.TokenOnBetBack = null;
		player.TokenOnBetFront = null;
		player.CardOnHand = null;
	}

	PokerGameState DrawCard()
	{
		if (_state.PokerPool.Count < 2) return PokerGameState.GameEndVictoryCheck;
		PlayerDrawCard(_state.Player1);
		PlayerDrawCard(_state.Player2);
		return _state.CurrentInitialActionPlayer.PlayerId == _state.Player1.PlayerId
			? PokerGameState.Player1Bet
			: PokerGameState.Player2Bet;
	}

	void PlayerDrawCard(PlayerModel player)
	{
		var nextIndex = _random.Next(_state.PokerPool.Count);
		var nextCard = _state.PokerPool[nextIndex];
		_state.PokerPool.Remove(nextCard);
		player.CardOnHand = nextCard;
	}

	GameState NewGame()
	{
		var player1 = NewPlayer("Player 1");
		var newState = new GameState
		{
			State = PokerGameState.PlayersMandatoryBets,
			Player1 = player1,
			Player2 = NewPlayer("Player 2"),
			CurrentInitialActionPlayer = player1,
			PokerPool = GenerateDeck()
		};
		return newState;
	}

	List<PokerCard> GenerateDeck()
	{
		var front = GenerateCardNumbers();
		var back = GenerateCardNumbers();
		return Enumerable.Range(0, front.Count).Select(i => new PokerCard
		{
			FrontValue = front[i],
			BackValue = back[i]
		}).ToList();
	}

	List<int> GenerateCardNumbers()
	{
		return Enumerable.Range(1, 9).Select(num => Enumerable.Range(0, 10).Select(_ => num)).SelectMany(n => n)
			.OrderBy(
				_ => _random.Next()
			).ToList();
	}

	PlayerModel NewPlayer(string name)
	{
		return new PlayerModel
		{
			PlayerName = name,
			TotalTokenOnBet = 0,
			TokenOnHand = 30,
			PlayerId = Guid.NewGuid()
		};
	}

	public void Bet(PlayerModel player, int amount, BetMethod method)
	{
		if (amount <= 0)
		{
			AppendEventLog("Error - Bets must be greater than zero");
			return;
		}

		try
		{
			if (player.PlayerId == _state.Player1.PlayerId && _state.State == PokerGameState.Player1Bet)
			{
				ProcessBet(_state.Player1, _state.Player2, amount, method);
				_state.State = PokerGameState.Player1BetComplete;
			}

			if (player.PlayerId == _state.Player2.PlayerId && _state.State == PokerGameState.Player2Bet)
			{
				ProcessBet(_state.Player2, _state.Player1, amount, method);
				_state.State = PokerGameState.Player2BetComplete;
			}
		}
		catch (Exception e)
		{
			AppendEventLog($"Error - {e.Message}");
		}
	}

	void ProcessBet(PlayerModel player, PlayerModel opponent, int amount, BetMethod method)
	{
		if (amount > player.TokenOnHand)
			throw new InvalidOperationException(
				$" ${player.PlayerName} bet more than they have");
		switch (method)
		{
			case BetMethod.Front:
				if (player.TokenOnBetBack != null)
					throw new InvalidOperationException(
						$"${player.PlayerName} cannot bet on front as you already bet on back");
				player.TokenOnBetFront ??= 0;
				if (player.TokenOnBetFront + amount > opponent.TokenOnHand + opponent.TotalTokenOnBet)
					throw new InvalidOperationException(
						$"${player.PlayerName} bet more than opponent's tokens");
				player.TotalTokenOnBet += amount;
				player.TokenOnBetFront += amount;
				player.TokenOnHand -= amount;
				break;
			case BetMethod.Back:
				if (player.TokenOnBetFront != null)
					throw new InvalidOperationException(
						$"${player.PlayerName} cannot bet on back as you already bet on front");
				player.TokenOnBetBack ??= 0;
				if (player.TokenOnBetBack + amount > opponent.TokenOnHand + opponent.TotalTokenOnBet)
					throw new InvalidOperationException(
						$"${player.PlayerName} bet more than opponent's tokens");
				player.TotalTokenOnBet += amount;
				player.TokenOnBetBack += amount;
				player.TokenOnHand -= amount;
				break;
			case BetMethod.Both:
				if (amount * 2 > opponent.TokenOnHand + opponent.TotalTokenOnBet)
					throw new InvalidOperationException(
						$" ${player.PlayerName} bet more than they have");
				player.TokenOnBetFront ??= 0;
				player.TokenOnBetBack ??= 0;
				if (amount <= player.TokenOnBetBack || amount <= player.TokenOnBetFront)
					throw new InvalidOperationException(
						$" ${player.PlayerName} must bet higher than current");
				var requiredAmount = 2 * amount - player.TokenOnBetBack - player.TokenOnBetFront ?? 0;
				if (requiredAmount > player.TokenOnHand)
					throw new InvalidOperationException(
						$" ${player.PlayerName} bet more than they have");

				player.TotalTokenOnBet = amount * 2;
				player.TokenOnBetFront = amount;
				player.TokenOnBetBack = amount;
				player.TokenOnHand -= requiredAmount;
				break;
		}
	}

	public void GiveUpCurrentRound(PlayerModel player)
	{
		if (player.PlayerId == _state.Player1.PlayerId && _state.State == PokerGameState.Player1Bet)
			_state.State = PokerGameState.Player1GiveUp;
		if (player.PlayerId == _state.Player2.PlayerId && _state.State == PokerGameState.Player2Bet)
			_state.State = PokerGameState.Player2GiveUp;
	}

	void AppendEventLog(string str)
	{
		var eventTexts = (GetNode("Events") as RichTextLabel)!;
		eventTexts.Text += str + "\n";
		GD.Print(str);
	}
}
