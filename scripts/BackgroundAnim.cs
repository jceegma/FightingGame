using Godot;

public partial class BackgroundAnim : AnimatedSprite2D
{
	public override void _Ready()
	{
		Play("bg");
	}
}
