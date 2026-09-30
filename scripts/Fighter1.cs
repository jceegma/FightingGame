using Godot;

// INHERITANCE: Fighter1 gets Speed, Gravity, Health, Move() and TakeDamage()
// for free from Character, and adds controls, attacking and hit reactions.
public partial class Fighter1 : Character
{
	[Export] public int PlayerNumber = 1;
	[Export] public int Attack1Damage = 10;
	[Export] public int Attack2Damage = 15;
	[Export] public float HitboxOffset = 40f;      // how far in front of the body the punch box sits
	[Export] public float KnockbackSpeed = 300f;   // how hard a hit pushes the target back

	// POLYMORPHISM: subclasses can override these for different controls/animations
	protected virtual string InputPrefix => PlayerNumber == 1 ? "p1" : "p2";
	protected override string AnimPrefix => InputPrefix + "_";
	protected string Attack1Anim => AnimPrefix + "attack1";
	protected string Attack2Anim => AnimPrefix + "attack2";

	private const float KnockbackDecay = 900f;

	// While _actionTimer is above 0 the fighter is busy (attacking or hurt)
	private float _actionTimer;
	private bool IsBusy => _actionTimer > 0;

	private bool _isAttacking;
	private bool _hasHit;             // makes sure one punch hurts only once
	private bool _controlsLocked;     // set by Main (countdown, round over)
	private int _currentDamage;
	private float _knockbackX;

	private Area2D _attackArea;
	private CollisionShape2D _attackShape;

	public override void _Ready()
	{
		base._Ready();
		AddToGroup("fighters");   // lets a fighter find its opponent

		// The hitbox: always watching, but its shape is only ON during a punch
		_attackArea = GetNode<Area2D>("AttackMove");
		_attackShape = _attackArea.GetNode<CollisionShape2D>("CollisionShape2D");
		_attackArea.Monitoring = true;
		_attackArea.Monitorable = false;
		_attackShape.Disabled = true;
	}

	// ENCAPSULATION: other classes ask for a lock; they can't touch the field itself
	public void SetControlsLocked(bool locked) => _controlsLocked = locked;

	// Called by the attacker. direction is +1 (push right) or -1 (push left)
	public void ApplyKnockback(int direction) => _knockbackX = direction * KnockbackSpeed;

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Vector2 velocity = Velocity;

		if (!IsOnFloor())
			velocity.Y += Gravity * dt;

		if (IsDead)
		{
			velocity.X = _knockbackX;          // dead: slides a bit, then stays down
			DecayKnockback(dt);
		}
		else if (IsBusy)
		{
			velocity.X = _knockbackX;          // attacking or hurt: can't walk
			DecayKnockback(dt);
			_actionTimer -= dt;

			if (_isAttacking)
			{
				if (!_hasHit && !_controlsLocked)
					_hasHit = TryHit();        // look for a target every frame
				if (_actionTimer <= 0)
					EndAttack();
			}
		}
		else if (_controlsLocked)
		{
			velocity.X = 0;                    // countdown or round over: just stand
			RefreshFacing(0);
			UpdateAnimation(0, velocity);
		}
		else
		{
			HandleControls(ref velocity);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void DecayKnockback(float dt)
		=> _knockbackX = Mathf.MoveToward(_knockbackX, 0f, KnockbackDecay * dt);

	private void HandleControls(ref Vector2 velocity)
	{
		float moveInput = Input.GetAxis(InputPrefix + "_left", InputPrefix + "_right");
		bool jumpPressed = Input.IsActionJustPressed(InputPrefix + "_jump");

		Move(moveInput, jumpPressed, ref velocity);

		if (Input.IsActionJustPressed(InputPrefix + "_attack1"))
			StartAttack(Attack1Anim, Attack1Damage);
		else if (Input.IsActionJustPressed(InputPrefix + "_attack2"))
			StartAttack(Attack2Anim, Attack2Damage);
	}

	// POLYMORPHISM: a Fighter1 turns to face its opponent instead of its walk direction
	protected override void UpdateFacing(float moveInput)
	{
		Fighter1 opponent = FindOpponent();
		if (opponent != null)
			_facingRight = opponent.GlobalPosition.X > GlobalPosition.X;
		else
			base.UpdateFacing(moveInput);
	}

	private Fighter1 FindOpponent()
	{
		foreach (Node node in GetTree().GetNodesInGroup("fighters"))
			if (node is Fighter1 other && other != this)
				return other;
		return null;
	}

	private void StartAttack(string animationName, int damage)
	{
		_isAttacking = true;
		_hasHit = false;
		_currentDamage = damage;
		_knockbackX = 0;
		_actionTimer = AnimDuration(animationName);

		// Put the hitbox in front of the fighter, then switch it on
		float side = _facingRight ? HitboxOffset : -HitboxOffset;
		_attackShape.Position = new Vector2(side, _attackShape.Position.Y);
		_attackShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);

		PlayAnim(animationName, true);
	}

	private void EndAttack()
	{
		_isAttacking = false;
		_attackShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
	}

	// Asks the hitbox: "who is inside you right now?"
	private bool TryHit()
	{
		foreach (Node2D body in _attackArea.GetOverlappingBodies())
		{
			if (body is Fighter1 target && target != this && !target.IsDead)
			{
				// POLYMORPHISM: runs the TARGET's own TakeDamage
				target.TakeDamage(_currentDamage);
				target.ApplyKnockback(_facingRight ? 1 : -1);
				return true;
			}
		}
		return false;
	}

	// POLYMORPHISM: same method as Character, extended with a hit reaction
	public override void TakeDamage(int amount)
	{
		base.TakeDamage(amount);      // subtract health, emit signals, maybe call Die()
		if (IsDead) return;

		if (_isAttacking) EndAttack();   // getting hit interrupts your own punch

		_actionTimer = AnimDuration(HitAnim);
		PlayAnim(HitAnim, true);
	}

	// Required by Character (it's abstract): what a Fighter1 does when it dies
	protected override void Die()
	{
		if (_isAttacking) EndAttack();
		_actionTimer = 0;
		PlayAnim(DeathAnim, true);
		GD.Print($"{Name} died");
	}
}