using Godot;

// ABSTRACTION: Character describes "a body with health and movement"
// without knowing how it fights. It's abstract, so you can never create
// a plain Character, and every subclass must define how it dies.
public abstract partial class Character : CharacterBody2D
{
	[Signal] public delegate void HealthChangedEventHandler(int current, int max);
	[Signal] public delegate void DiedEventHandler(Character who);

	[Export] public float Speed = 300f;
	[Export] public float JumpVelocity = -700;
	[Export] public float Gravity = 1200f;
	[Export] public int MaxHealth = 100;
	[Export] public bool StartFacingRight = true;
	[Export] public bool ArtFacesRight = true;   // tick if the drawn sprite looks right, untick if it looks left

	// ENCAPSULATION: anyone can READ Health, but only this class can change it
	public int Health { get; private set; }
	public bool IsDead => Health <= 0;

	protected bool _facingRight = true;
	protected AnimatedSprite2D _sprite;

	// POLYMORPHISM: each subclass chooses its own prefix (p1_, p2_ ...)
	protected virtual string AnimPrefix => "";

	protected string IdleAnim  => AnimPrefix + "idle";
	protected string RunAnim   => AnimPrefix + "run";
	protected string JumpAnim  => AnimPrefix + "jump";
	protected string FallAnim  => AnimPrefix + "fall";
	protected string HitAnim   => AnimPrefix + "takeHit";
	protected string DeathAnim => AnimPrefix + "death";

	public override void _Ready()
	{
		Health = MaxHealth;
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_facingRight = StartFacingRight;
		ApplyFacingToSprite();
		PlayAnim(IdleAnim);
	}

	protected void Move(float moveInput, bool jumpPressed, ref Vector2 velocity)
	{
		velocity.X = moveInput * Speed;

		RefreshFacing(moveInput);

		if (jumpPressed && IsOnFloor())
			velocity.Y = JumpVelocity;

		UpdateAnimation(moveInput, velocity);
	}

	// Works out which way to face, then flips the sprite to match
	protected void RefreshFacing(float moveInput)
	{
		UpdateFacing(moveInput);
		ApplyFacingToSprite();
	}

	private void ApplyFacingToSprite()
	{
		// Flip only when the way we want to face differs from the way the art was drawn
		_sprite.FlipH = _facingRight != ArtFacesRight;
	}

	// POLYMORPHISM: default = face the way you walk; subclasses may change this
	protected virtual void UpdateFacing(float moveInput)
	{
		if (moveInput > 0) _facingRight = true;
		else if (moveInput < 0) _facingRight = false;
	}

	protected virtual void UpdateAnimation(float moveInput, Vector2 velocity)
	{
		string wanted;
		if (!IsOnFloor())
			wanted = velocity.Y < 0 ? JumpAnim : FallAnim;
		else
			wanted = moveInput != 0 ? RunAnim : IdleAnim;

		PlayAnim(wanted);
	}

	// Safe animation player: does nothing if the animation doesn't exist.
	// restart = true replays the animation from frame 1 (used for hit/attack/death).
	protected void PlayAnim(string name, bool restart = false)
	{
		var frames = _sprite.SpriteFrames;
		if (frames == null || !frames.HasAnimation(name)) return;
		if (!restart && _sprite.Animation == name && _sprite.IsPlaying()) return;

		_sprite.Play(name);
	}

	// How long an animation lasts in seconds (frames / FPS)
	protected float AnimDuration(string name, float fallback = 0.3f)
	{
		var frames = _sprite.SpriteFrames;
		if (frames == null || !frames.HasAnimation(name)) return fallback;

		float fps = (float)frames.GetAnimationSpeed(name);
		return fps > 0 ? frames.GetFrameCount(name) / fps : fallback;
	}

	public virtual void TakeDamage(int amount)
	{
		if (IsDead || amount <= 0) return;

		Health = Mathf.Max(Health - amount, 0);
		GD.Print($"{Name} health: {Health}");
		EmitSignal(SignalName.HealthChanged, Health, MaxHealth);

		if (IsDead)
		{
			Die();
			EmitSignal(SignalName.Died, this);
		}
	}

	// ABSTRACTION: every subclass MUST provide its own version
	protected abstract void Die();
}
