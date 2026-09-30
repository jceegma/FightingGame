using Godot;

// A health bar that listens to ANY Character.
// ABSTRACTION: it only knows about Character, not Fighter1 or Fighter2.
// ENCAPSULATION: all the styling and updating is hidden inside this class.
public partial class HealthBar : ProgressBar
{
	private Character _character;
	private StyleBoxFlat _fill;

	public void Bind(Character character, bool drainToOutside)
	{
		_character = character;

		MinValue = 0;
		MaxValue = character.MaxHealth;
		Value = character.Health;
		ShowPercentage = false;

		// Player 2's bar fills from the right so it drains toward the screen edge
		FillMode = (int)(drainToOutside ? FillModeEnum.EndToBegin : FillModeEnum.BeginToEnd);

		_fill = new StyleBoxFlat { BgColor = Colors.LimeGreen };
		AddThemeStyleboxOverride("fill", _fill);
		AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.15f, 0.15f, 0.15f) });

		character.HealthChanged += OnHealthChanged;
	}

	private void OnHealthChanged(int current, int max)
	{
		Value = current;

		float ratio = (float)current / max;
		_fill.BgColor = ratio > 0.5f ? Colors.LimeGreen
					  : ratio > 0.25f ? Colors.Yellow
					  : Colors.Red;
	}

	public override void _ExitTree()
	{
		// Stop listening when this bar is removed (for example on restart)
		if (_character != null)
			_character.HealthChanged -= OnHealthChanged;
	}
}