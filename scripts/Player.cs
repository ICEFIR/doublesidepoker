using Doublesidepoker.scripts;
using Godot;

public partial class Player : Node2D
{
#nullable enable
	PlayerModel? _player;

	bool _showBackValue;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_player is null) return;
		(GetNode("PlayerIndicator") as Label)!.Text = _player.PlayerName;
		var cardFrontValueLabel = (GetNode("CardFrontLabel/CardFrontValue") as Label)!;
		var cardBackValueLabel = (GetNode("CardBackLabel/CardBackValue") as Label)!;
		if (_player.CardOnHand is null)
		{
			cardFrontValueLabel.Text = "-";
			cardBackValueLabel.Text = "-";
		}
		else
		{
			cardFrontValueLabel.Text = _player.CardOnHand.FrontValue.ToString();
			cardBackValueLabel.Text = _showBackValue ? _player.CardOnHand.BackValue.ToString() : "#";
		}

		var tokenValueLabel = (GetNode("TokenLabel/TokenValue") as Label)!;
		tokenValueLabel.Text = _player.TokenOnHand.ToString();
		var betFrontLabel = (GetNode("TokenBetFront/TokenValue") as Label)!;
		var betBackLabel = (GetNode("TokenBetBack/TokenValue") as Label)!;
		var totalBetLabel = (GetNode("TokenTotalBet/Value") as Label)!;

		betFrontLabel.Text = _player.TokenOnBetFront is null ? "-" : _player.TokenOnBetFront.Value.ToString();
		betBackLabel.Text = _player.TokenOnBetBack is null ? "-" : _player.TokenOnBetBack.Value.ToString();
		totalBetLabel.Text = (_player.TokenOnBetBack + _player.TokenOnBetFront ?? 0).ToString();
	}

	public void SetPlayer(PlayerModel playerModel)
	{
		_player = playerModel;
	}

	void _on_show_card_back_toggled(bool button_pressed)
	{
		_showBackValue = button_pressed;
		GD.Print(button_pressed);
	}

	void _on_bet_both_button_pressed()
	{
		var value = (GetNode("BetBothLabel/Value") as SpinBox)!.Value;
		GameManager.GetGameManager().Bet(_player, (int)value, BetMethod.Both);
	}

	void _on_bet_back_button_pressed()
	{
		var value = (GetNode("BetBackLabel/Value") as SpinBox)!.Value;
		GameManager.GetGameManager().Bet(_player, (int)value, BetMethod.Back);
	}

	void _on_bet_front_button_pressed()
	{
		var value = (GetNode("BetFrontLabel/Value") as SpinBox)!.Value;
		GameManager.GetGameManager().Bet(_player, (int)value, BetMethod.Front);
	}

	void _on_give_up_pressed()
	{
		GameManager.GetGameManager().GiveUpCurrentRound(_player);
	}
}
