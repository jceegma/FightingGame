using Godot;

// Main is the "referee": it builds the HUD and arena walls, runs the countdown
// and timer, listens to the fighters' signals, and decides who wins.
public partial class Main : Node2D
{
	[Export] public float RoundTime = 60f;

	private const float CountdownTime = 3f;

	private enum RoundState { Countdown, Fighting, Over }

	private Fighter1 _p1, _p2;
	private Label _timerLabel, _messageLabel;
	private RoundState _state = RoundState.Countdown;
	private float _countdown = CountdownTime;
	private float _timeLeft;
	private float _messageTimer;

	public override void _Ready()
	{
		_p1 = GetNode<Fighter1>("Fighter1");
		_p2 = GetNode<Fighter1>("Fighter2");
		_timeLeft = RoundTime;

		BuildWalls();
		BuildHud();

		// Nobody moves until the countdown ends
		_p1.SetControlsLocked(true);
		_p2.SetControlsLocked(true);

		// Loose coupling: fighters announce, Main reacts
		_p1.Died += OnFighterDied;
		_p2.Died += OnFighterDied;
	}

	// ---------- Building things ----------

	private void BuildWalls()
	{
		Vector2 screen = GetViewportRect().Size;
		AddWall(new Vector2(-25, screen.Y / 2));
		AddWall(new Vector2(screen.X + 25, screen.Y / 2));
	}

	private void AddWall(Vector2 position)
	{
		var wall = new StaticBody2D { Position = position };
		wall.AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(50, 2000) }
		});
		AddChild(wall);
	}

	private void BuildHud()
	{
		var hud = new CanvasLayer();   // draws on top of the game, fixed on screen
		AddChild(hud);

		Vector2 screen = GetViewportRect().Size;
		float margin = screen.X * 0.02f;
		float barWidth = screen.X * 0.38f;
		float barHeight = screen.Y * 0.05f;
		float nameHeight = screen.Y * 0.05f;
		int smallFont = Mathf.RoundToInt(screen.Y * 0.04f);
		int timerFont = Mathf.RoundToInt(screen.Y * 0.08f);
		int bigFont = Mathf.RoundToInt(screen.Y * 0.09f);

		// Player names
		CreateLabel(hud, "PLAYER 1", new Vector2(margin, margin),
			new Vector2(barWidth, nameHeight), smallFont, HorizontalAlignment.Left);
		CreateLabel(hud, "PLAYER 2", new Vector2(screen.X - margin - barWidth, margin),
			new Vector2(barWidth, nameHeight), smallFont, HorizontalAlignment.Right);

		// Health bars
		float barY = margin + nameHeight;
		CreateBar(hud, _p1, new Vector2(margin, barY),
			new Vector2(barWidth, barHeight), false);
		CreateBar(hud, _p2, new Vector2(screen.X - margin - barWidth, barY),
			new Vector2(barWidth, barHeight), true);

		// Timer in the middle
		float timerWidth = screen.X * 0.14f;
		_timerLabel = CreateLabel(hud, Mathf.CeilToInt(RoundTime).ToString(),
			new Vector2((screen.X - timerWidth) / 2, margin),
			new Vector2(timerWidth, nameHeight + barHeight), timerFont, HorizontalAlignment.Center);

		// Big message in the center (countdown, KO, winner)
		_messageLabel = CreateLabel(hud, "", Vector2.Zero, screen, bigFont, HorizontalAlignment.Center);
		_messageLabel.Visible = false;
	}

	private static void CreateBar(CanvasLayer hud, Character character, Vector2 position, Vector2 size, bool drainToOutside)
	{
		var bar = new HealthBar { Position = position, Size = size, CustomMinimumSize = size };
		hud.AddChild(bar);
		bar.Bind(character, drainToOutside);
	}

	private static Label CreateLabel(CanvasLayer hud, string text, Vector2 position, Vector2 size, int fontSize, HorizontalAlignment align)
	{
		var label = new Label
		{
			Text = text,
			Position = position,
			Size = size,
			HorizontalAlignment = align,
			VerticalAlignment = VerticalAlignment.Center
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeConstantOverride("outline_size", 6);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		hud.AddChild(label);
		return label;
	}

	// ---------- Round flow ----------

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// Hide short messages like "FIGHT!" after a moment
		if (_messageTimer > 0)
		{
			_messageTimer -= dt;
			if (_messageTimer <= 0 && _state == RoundState.Fighting)
				_messageLabel.Visible = false;
		}

		switch (_state)
		{
			case RoundState.Countdown:
				UpdateCountdown(dt);
				break;
			case RoundState.Fighting:
				UpdateFight(dt);
				break;
			case RoundState.Over:
				// ui_accept is built into Godot (Enter/Space), so it needs no Input Map entry
				if (Input.IsActionJustPressed("ui_accept"))
					GetTree().ReloadCurrentScene();
				break;
		}
	}

	private void UpdateCountdown(float dt)
	{
		_countdown -= dt;
		if (_countdown > 0)
		{
			ShowMessage(Mathf.CeilToInt(_countdown).ToString());
			return;
		}

		_state = RoundState.Fighting;
		_p1.SetControlsLocked(false);
		_p2.SetControlsLocked(false);
		ShowMessage("FIGHT!", 0.8f);
	}

	private void UpdateFight(float dt)
	{
		_timeLeft -= dt;
		_timerLabel.Text = Mathf.CeilToInt(Mathf.Max(_timeLeft, 0f)).ToString();

		if (_timeLeft <= 0)
			TimeUp();
	}

	private void OnFighterDied(Character loser)
	{
		if (_state == RoundState.Over) return;

		Fighter1 winner = loser == _p1 ? _p2 : _p1;
		winner.SetControlsLocked(true);   // the winner stands idle

		EndRound($"KO!\n{NameOf(winner)} wins!");
	}

	private void TimeUp()
	{
		_p1.SetControlsLocked(true);
		_p2.SetControlsLocked(true);

		string result;
		if (_p1.Health > _p2.Health) result = "Player 1 wins!";
		else if (_p2.Health > _p1.Health) result = "Player 2 wins!";
		else result = "Draw!";

		EndRound($"TIME UP!\n{result}");
	}

	private void EndRound(string text)
	{
		_state = RoundState.Over;
		ShowMessage(text + "\nPress Enter to restart");
	}

	private string NameOf(Fighter1 fighter) => fighter == _p1 ? "Player 1" : "Player 2";

	private void ShowMessage(string text, float duration = 0f)
	{
		_messageLabel.Text = text;
		_messageLabel.Visible = true;
		_messageTimer = duration;
	}
}