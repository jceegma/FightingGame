using Godot;

// INHERITANCE: Fighter2 is a Fighter1, so it keeps movement, attacks, health and animations.
// POLYMORPHISM: it only overrides which controls (and animation prefix) it uses.
public partial class Fighter2 : Fighter1
{
	protected override string InputPrefix => "p2";
}
