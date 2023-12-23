using Sandbox;
using System;

public enum HoldTypeMode { None, HoldNone, PunchShotgun, RiflePistol, RifleRpg, PistolNone, }
public enum HandIKMode { None, Enabled }
public enum FootIKMode { None, Crazy, Shuffle }
public enum LookDirMode { None, Random, Forward, LeftRight, UpDown, Wobble, Headbang }
public enum AnimMoveMode { None, Still, RandomToggle, Steady, }
public enum HeightMode { None, Enabled }
public enum GroundedMode { None, Enabled }
public enum AttackMode { None, Enabled }
public enum JumpMode { None, Enabled }
public enum VoiceMode { None, Alternate, Chomp, ChompReverse }
public enum NoclipMode { None, Enabled }
public enum SwimmingMode { None, Enabled }
public enum SittingMode { None, Enabled }
public enum DuckMode { None, Enabled }
public enum SpecialMovementMode { None, Climb, Roll, Flail }
public enum MoveMode { None, TargetPos, BackAndForth }
public enum SpinMode { None, Constant }

public sealed class KeygenCitizenAnimator : Component, Component.ExecuteInEditor
{
	[Property] public SkinnedModelRenderer Target { get; set; }

	//[Property] public GameObject EyeSource { get; set; }

	//[Property] public GameObject LookAtObject { get; set; }

	[Property, Range( 0.5f, 1.5f)] public float Height { get; set; } = 1.0f;

	[Property] public GameObject IkLeftHand { get; set; }
	[Property] public GameObject IkRightHand { get; set; }
	[Property] public GameObject IkLeftFoot { get; set; }
	[Property] public GameObject IkRightFoot { get; set; }

	public KeygenManager Manager { get; set; }

	public int NumBeats { get; private set; }

	private Vector3 _ikHandRightPos;
	private Vector3 _ikHandLeftPos;
	private Vector3 _ikHandRightPosTarget;
	private Vector3 _ikHandLeftPosTarget;
	private Rotation _ikHandRightRot;
	private Rotation _ikHandLeftRot;
	private Rotation _ikHandRightRotTarget;
	private Rotation _ikHandLeftRotTarget;

	private Vector3 _ikFootRightPos;
	private Vector3 _ikFootLeftPos;
	private Vector3 _ikFootRightPosTarget;
	private Vector3 _ikFootLeftPosTarget;
	private Rotation _ikFootRightRot;
	private Rotation _ikFootLeftRot;
	private Rotation _ikFootRightRotTarget;
	private Rotation _ikFootLeftRotTarget;

	public Vector3 LookDir { get; set; }
	public Vector3 AnimVelocity { get; set; }

	public Vector3 TargetPosition { get; set; }

	public float TargetSitMode { get; set; }

	public bool AllHoldTypes { get; set; }
	public int AllHoldTypesLength { get; set; } = 4;
	public HoldTypeMode HoldTypeMode { get; set; }
	public HandIKMode HandIKMode { get; set; }
	public FootIKMode FootIKMode { get; set; }
	public LookDirMode LookDirMode { get; set; }
	public AnimMoveMode AnimMoveMode { get; set; }
	public HeightMode HeightMode { get; set; }
	public GroundedMode GroundedMode { get; set; }
	public int GroundedModeModulus { get; set; } = 1;
	public bool GroundedModeModulusInverted { get; set; }
	public float GroundedModeLength { get; set; } = 0.2f;
	public AttackMode AttackMode { get; set; }
	public JumpMode JumpMode { get; set; }
	public VoiceMode VoiceMode { get; set; }
	public NoclipMode NoclipMode { get; set; }
	public SwimmingMode SwimmingMode { get; set; }
	public SittingMode SittingMode { get; set; }
	public DuckMode DuckMode { get; set; }
	public SpecialMovementMode SpecialMovementMode { get; set; }
	public MoveMode MoveMode { get; set; }
	public SpinMode SpinMode { get; set; }

	private bool _moveBackAndForthLeft;

	public float SpinSpeed { get; set; } = 80f;
	public float BaseYaw { get; set; } = -180f;

	public int HoldTypeInt { get; set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		CurrMainHoldType = HoldTypes.Punch;
	}

	public HoldTypes CurrMainHoldType { get; set; }
	public int HoldTypeBeatModulus { get; set; } = 2;

	public int AttackModeModulus { get; set; } = 2;
	public bool AttackModeModulusInverted { get; set; }
	public int LookDirBeatModulus { get; set; } = 2;
	public float MoveModeSpeed { get; set; } = 40f;
	public bool MoveModeSmooth { get; set; }
	public int DuckModeModulus { get; set; } = 4;
	public float DuckAmount { get; set; } = 1.5f;
	public int JumpModeModulus { get; set; } = 2;
	public int NoclipModeModulus { get; set; } = 2;
	public int SwimmingModeModulus { get; set; } = 2;
	public int FootIKModulus { get; set; } = 1;
	public int HandIKModulus { get; set; } = 1;

	public void OnBeat()
	{
		NumBeats++;

		if(AllHoldTypes)
		{
			if ( NumBeats % AllHoldTypesLength == 0 )
				CurrMainHoldType = GetRandomHoldType();

			SetHoldType( (NumBeats % HoldTypeBeatModulus == 0)
				? CurrMainHoldType
				: GetRandomHoldType( except: CurrMainHoldType ) );
		}
		else
		{
			if ( HoldTypeMode == HoldTypeMode.HoldNone )
			{
				SetHoldType( (NumBeats % HoldTypeBeatModulus == 0) ? HoldTypes.None : HoldTypes.HoldItem );
			}
			else if ( HoldTypeMode == HoldTypeMode.PunchShotgun )
			{
				SetHoldType( (NumBeats % HoldTypeBeatModulus == 0) ? HoldTypes.Punch : HoldTypes.Shotgun );
			}
			else if ( HoldTypeMode == HoldTypeMode.RiflePistol )
			{
				SetHoldType( (NumBeats % HoldTypeBeatModulus == 0) ? HoldTypes.Rifle : HoldTypes.Pistol );
			}
			else if ( HoldTypeMode == HoldTypeMode.RifleRpg )
			{
				SetHoldType( (NumBeats % HoldTypeBeatModulus == 0) ? HoldTypes.Rifle : HoldTypes.RPG );
			}
			else if ( HoldTypeMode == HoldTypeMode.PistolNone )
			{
				SetHoldType( (NumBeats % HoldTypeBeatModulus == 0) ? HoldTypes.Pistol : HoldTypes.None );
			}
		}

		if (NoclipMode == NoclipMode.Enabled)
		{
			IsNoclipping = NumBeats % NoclipModeModulus == 0;
		}

		if ( SwimmingMode == SwimmingMode.Enabled )
		{
			IsSwimming = NumBeats % SwimmingModeModulus == 0;
		}

		if (VoiceMode == VoiceMode.Alternate)
		{
			VoiceLevel = (VoiceLevel > 0.5f) ? 0f : 1f;
		}

		if ( HandIKMode == HandIKMode.Enabled)
		{
			if ( NumBeats % HandIKModulus == 0 )
			{
				_ikHandLeftPosTarget = new Vector3( 0f, 50f, Game.Random.Float( -100f, 100f ) );
				_ikHandRightPosTarget = new Vector3( 0f, -50f, Game.Random.Float( -100f, 100f ) );
				_ikHandLeftRotTarget = Rotation.From( Game.Random.Float( -50f, 50f ), Game.Random.Float( -90f, 60f ), Game.Random.Float( -40f, 40f ) );
				_ikHandRightRotTarget = Rotation.From( Game.Random.Float( -50f, 50f ), Game.Random.Float( -90f, 60f ), Game.Random.Float( -40f, 40f ) );
			}
		}

		if( FootIKMode == FootIKMode.Crazy )
		{
			if(NumBeats % FootIKModulus == 0)
			{
				_ikFootLeftPosTarget = new Vector3( Game.Random.Float( -25f, 25f ), Game.Random.Float( -20f, 100f ), Game.Random.Float( -25f, 25f ) );
				_ikFootRightPosTarget = new Vector3( Game.Random.Float( -25f, 25f ), Game.Random.Float( -100f, 20f ), Game.Random.Float( -25f, 25f ) );
				_ikFootLeftRotTarget = Rotation.From( Game.Random.Float( -50f, 50f ), Game.Random.Float( -90f, 60f ), Game.Random.Float( -40f, 40f ) );
				_ikFootRightRotTarget = Rotation.From( Game.Random.Float( -50f, 50f ), Game.Random.Float( -90f, 60f ), Game.Random.Float( -40f, 40f ) );
			}
		}
		else if ( FootIKMode == FootIKMode.Shuffle )
		{
			if ( NumBeats % FootIKModulus == 0 )
			{
				_ikFootLeftPosTarget = new Vector3( Game.Random.Float( -5f, 5f ), Game.Random.Float( -2f, 10f ), Game.Random.Float( -25f, 25f ) );
				_ikFootRightPosTarget = new Vector3( Game.Random.Float( -5f, 5f ), Game.Random.Float( -10f, 2f ), Game.Random.Float( -25f, 25f ) );
				_ikFootLeftRotTarget = Rotation.From( Game.Random.Float( -5f, 5f ), Game.Random.Float( -9f, 6f ), 90f + Game.Random.Float( -4f, 4f ) );
				_ikFootRightRotTarget = Rotation.From( Game.Random.Float( -5f, 5f ), Game.Random.Float( -9f, 6f ), 90f + Game.Random.Float( -4f, 4f ) );
			}
		}

		if ( SittingMode == SittingMode.Enabled )
		{
			Target.Set( "sit", Game.Random.Int( 0, 2 ) );
			TargetSitMode = Game.Random.Float( 0f, 3f );
		}

		if(SpecialMovementMode == SpecialMovementMode.None )
		{
			Target.Set( "special_movement_states", 0 );
		}
		else if(SpecialMovementMode == SpecialMovementMode.Climb)
		{
			Target.Set( "special_movement_states", NumBeats % 4 == 0 ? 1 : 0 );
		}
		else if ( SpecialMovementMode == SpecialMovementMode.Climb )
		{
			Target.Set( "special_movement_states", NumBeats % 4 == 0 ? 2 : 0 );
		}
		else if ( SpecialMovementMode == SpecialMovementMode.Climb )
		{
			Target.Set( "special_movement_states", NumBeats % 4 == 0 ? 3 : 0 );
		}

		if ( LookDirMode == LookDirMode.Random)
		{
			LookDir = new Vector3( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) );
		}
		else if (LookDirMode == LookDirMode.LeftRight)
		{
			LookDir = new Vector3( -100f, NumBeats % LookDirBeatModulus == 0 ? -1000f : 1000f, Transform.Position.y + 20f );  
		}
		else if ( LookDirMode == LookDirMode.UpDown )
		{
			LookDir = new Vector3( -100f, Transform.Position.y, NumBeats % LookDirBeatModulus == 0 ? -2000f : 2000f);
		}

		if (AnimMoveMode == AnimMoveMode.RandomToggle)
		{
			AnimVelocity = NumBeats % 2 == 0 ? Rotation.Random.Angles().AsVector3() * Game.Random.Float( -1.5f, 1.5f ) : Vector3.Zero;

			//Target.Set( "skid", Game.Random.Float(0f, 1f));
			//MoveStyle = MoveStyles.Walk;
		}

		if (DuckMode == DuckMode.Enabled)
		{
			DuckLevel = NumBeats % DuckModeModulus != 0 ? 0f : DuckAmount;
		}
		else
		{
			DuckLevel = 0f;
		}

		if(JumpMode == JumpMode.Enabled)
		{
			if(NumBeats % JumpModeModulus == 0)
				TriggerJump();
		}

		if(HoldTypeInt != 7) // no RPG
		{
			if ( AttackMode == AttackMode.Enabled )
			{
				if(AttackModeModulusInverted)
					Target.Set( "b_attack", NumBeats % AttackModeModulus != 0 );
				else
					Target.Set( "b_attack", NumBeats % AttackModeModulus == 0 );
			}
		}
	}

	public void OnSection()
	{

	}

	protected override void OnUpdate()
	{
		float dt = Time.Delta;

		if ( Manager == null )
			return;

		Target.Set( "b_attack", false );

		if (MoveMode == MoveMode.TargetPos )
		{
			Transform.Position = Utils.DynamicEaseTo( Transform.Position, TargetPosition, 0.05f, dt );
		}
		else if(MoveMode == MoveMode.BackAndForth)
		{
			float speed = MoveModeSpeed * (MoveModeSmooth ? 1f : Utils.Map( Manager.TimeSinceBeat, 0f, 0.25f, 2f, 0f, EasingType.QuadOut ));
			Transform.Position += new Vector3( 0f, 1f, 0f ) * speed * (_moveBackAndForthLeft ? 1f : -1f) * dt;

			if ( Transform.Position.y > 40f )
				_moveBackAndForthLeft = false;
			else if( Transform.Position.y < -40f )
				_moveBackAndForthLeft = true;
		}

		if(SpinMode == SpinMode.Constant)
		{
			Transform.Rotation = Rotation.From( Transform.Rotation.Pitch(), Transform.Rotation.Yaw() + SpinSpeed * dt, Transform.Rotation.Roll() );
		}
		else if(SpinMode == SpinMode.None)
		{
			Transform.Rotation = Rotation.Lerp( Transform.Rotation, Rotation.FromYaw( BaseYaw ), dt * 5f );
		}

		if(HeightMode == HeightMode.Enabled )
		{
			Height = Utils.DynamicEaseTo( Height, Utils.Map( Manager.TimeSinceBeat, 0f, 0.3f, MathF.Max(1.1f, Manager.SegmentAverages[3]), 0.8f, EasingType.QuadOut ), 0.1f, dt );
		}

		if(GroundedMode == GroundedMode.Enabled)
		{
			if ( GroundedModeModulusInverted )
				IsGrounded = NumBeats % GroundedModeModulus != 0 && Manager.TimeSinceBeat > GroundedModeLength;
			else
				IsGrounded = NumBeats % GroundedModeModulus == 0 && Manager.TimeSinceBeat > GroundedModeLength;
		}

		if ( VoiceMode == VoiceMode.Chomp )
		{
			VoiceLevel = Utils.Map( Manager.TimeSinceBeat, 0f, 0.33f, 0f, 1f, EasingType.QuadOut );
		}
		else if ( VoiceMode == VoiceMode.ChompReverse )
		{
			VoiceLevel = Utils.Map( Manager.TimeSinceBeat, 0f, 0.33f, 1f, 0f, EasingType.QuadOut );
		}
		
		if(LookDirMode == LookDirMode.Forward)
		{
			WithLook( Vector3.Zero, eyesWeight: 1, headWeight: 0.5f, bodyWeight: 0.1f );
		}
		else if ( LookDirMode == LookDirMode.Wobble )
		{
			WithLook( new Vector3(-100f, Utils.FastSin(Time.Now * 24f) * 2000f, Utils.FastSin( Time.Now * 25f ) * 3000f), eyesWeight: 1, headWeight: 0.5f, bodyWeight: 0.1f );
		}
		else if ( LookDirMode == LookDirMode.Headbang )
		{
			WithLook( new Vector3( -300f, Transform.Position.y, Utils.FastSin( Time.Now * 16f ) * 13000f ), eyesWeight: 1, headWeight: 0.5f, bodyWeight: 0.1f );
		}
		else if ( LookDirMode != LookDirMode.None )
		{
			WithLook( LookDir, eyesWeight: 1, headWeight: 0.5f, bodyWeight: 0.1f );
		}

		if ( AnimMoveMode == AnimMoveMode.Still )
		{
			WithVelocity( Vector3.Zero );
		}
		else if (AnimMoveMode != AnimMoveMode.None)
		{
			WithVelocity( AnimVelocity );
		}

		if(SittingMode == SittingMode.Enabled )
		{
			Target.Set( "sit_pose", Utils.DynamicEaseTo( Target.GetFloat( "sit_pose" ), TargetSitMode, 0.2f, dt ) );
		}

		if ( HandIKMode != HandIKMode.None)
		{
			Target.Set( "ik.hand_left.enabled", true );
			Target.Set( "ik.hand_right.enabled", true );
			float HAND_LERP_SPEED = 0.25f;
			_ikHandLeftPos = Utils.DynamicEaseTo( _ikHandLeftPos, _ikHandLeftPosTarget, HAND_LERP_SPEED, dt );
			_ikHandRightPos = Utils.DynamicEaseTo( _ikHandRightPos, _ikHandRightPosTarget, HAND_LERP_SPEED, dt );
			Target.Set( "ik.hand_left.position", _ikHandLeftPos );
			Target.Set( "ik.hand_right.position", _ikHandRightPos );
			_ikHandLeftRot = Utils.DynamicEaseTo( _ikHandLeftRot, _ikHandLeftRotTarget, HAND_LERP_SPEED, dt );
			_ikHandRightRot = Utils.DynamicEaseTo( _ikHandRightRot, _ikHandRightRotTarget, HAND_LERP_SPEED, dt );
			Target.Set( "ik.hand_left.rotation", _ikHandLeftRot );
			Target.Set( "ik.hand_right.rotation", _ikHandRightRot );
		}
		else
		{
			Target.Set( "ik.hand_left.enabled", false );
			Target.Set( "ik.hand_right.enabled", false );
		}

		if ( FootIKMode != FootIKMode.None)
		{
			Target.Set( "ik.foot_left.enabled", true );
			Target.Set( "ik.foot_right.enabled", true );
			float FOOT_LERP_SPEED = 0.25f;
			_ikFootLeftPos = Utils.DynamicEaseTo( _ikFootLeftPos, _ikFootLeftPosTarget, FOOT_LERP_SPEED, dt );
			_ikFootRightPos = Utils.DynamicEaseTo( _ikFootRightPos, _ikFootRightPosTarget, FOOT_LERP_SPEED, dt );
			Target.Set( "ik.foot_left.position", _ikFootLeftPos );
			Target.Set( "ik.foot_right.position", _ikFootRightPos );
			_ikFootLeftRot = Utils.DynamicEaseTo( _ikFootLeftRot, _ikFootLeftRotTarget, FOOT_LERP_SPEED, dt );
			_ikFootRightRot = Utils.DynamicEaseTo( _ikFootRightRot, _ikFootRightRotTarget, FOOT_LERP_SPEED, dt );
			Target.Set( "ik.foot_left.rotation", _ikFootLeftRot );
			Target.Set( "ik.foot_right.rotation", _ikFootRightRot );
		}
		else
		{
			Target.Set( "ik.foot_left.enabled", false );
			Target.Set( "ik.foot_right.enabled", false );
		}

		Target.Set( "scale_height", Height );
	}

	void SetHoldType( HoldTypes holdType )
	{
		HoldType = holdType;
		HoldTypeInt = (int)holdType;
	}

	public void SetIk( string name, Transform tx )
	{
		// convert local to model
		tx = Target.Transform.World.ToLocal( tx );

		Target.Set( $"ik.{name}.enabled", true );
		Target.Set( $"ik.{name}.position", tx.Position );
		Target.Set( $"ik.{name}.rotation", tx.Rotation );
	}

	public void ClearIk( string name )
	{
		Target.Set( $"ik.{name}.enabled", false );
	}

	/// <summary>
	/// Have the player look at this point in the world
	/// </summary>
	public void WithLook( Vector3 lookDirection, float eyesWeight = 1.0f, float headWeight = 1.0f, float bodyWeight = 1.0f )
	{
		Target.SetLookDirection( "aim_eyes", lookDirection );
		Target.SetLookDirection( "aim_head", lookDirection );
		Target.SetLookDirection( "aim_body", lookDirection );

		AimEyesWeight = eyesWeight;
		AimHeadWeight = headWeight;
		AimBodyWeight = bodyWeight;
	}

	public void WithVelocity( Vector3 Velocity )
	{
		var dir = Velocity;
		var forward = Target.Transform.Rotation.Forward.Dot( dir );
		var sideward = Target.Transform.Rotation.Right.Dot( dir );

		var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

		Target.Set( "move_direction", angle );
		Target.Set( "move_speed", Velocity.Length );
		Target.Set( "move_groundspeed", Velocity.WithZ( 0 ).Length );
		Target.Set( "move_y", sideward );
		Target.Set( "move_x", forward );
		Target.Set( "move_z", Velocity.z );
	}

	public void WithWishVelocity( Vector3 Velocity )
	{
		var dir = Velocity;
		var forward = Target.Transform.Rotation.Forward.Dot( dir );
		var sideward = Target.Transform.Rotation.Right.Dot( dir );

		var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

		Target.Set( "wish_direction", angle );
		Target.Set( "wish_speed", Velocity.Length );
		Target.Set( "wish_groundspeed", Velocity.WithZ( 0 ).Length );
		Target.Set( "wish_y", sideward );
		Target.Set( "wish_x", forward );
		Target.Set( "wish_z", Velocity.z );
	}

	//public Rotation AimAngle
	//{
	//	set
	//	{
	//		value = Target.Transform.Rotation.Inverse * value;
	//		var ang = value.Angles();

	//		Target.Set( "aim_body_pitch", ang.pitch );
	//		Target.Set( "aim_body_yaw", ang.yaw );
	//	}
	//}

	public float AimEyesWeight
	{
		get => Target.GetFloat( "aim_eyes_weight" );
		set => Target.Set( "aim_eyes_weight", value );
	}

	public float AimHeadWeight
	{
		get => Target.GetFloat( "aim_head_weight" );
		set => Target.Set( "aim_head_weight", value );
	}

	public float AimBodyWeight
	{
		get => Target.GetFloat( "aim_body_weight" );
		set => Target.Set( "aim_body_weight", value );
	}


	public float FootShuffle
	{
		get => Target.GetFloat( "move_shuffle" );
		set => Target.Set( "move_shuffle", value );
	}

	public float DuckLevel
	{
		get => Target.GetFloat( "duck" );
		set => Target.Set( "duck", value );
	}

	public float VoiceLevel
	{
		get => Target.GetFloat( "voice" );
		set => Target.Set( "voice", value );
	}

	//public bool IsSitting
	//{
	//	get => Target.GetBool( "b_sit" );
	//	set => Target.Set( "b_sit", value );
	//}

	public bool IsGrounded
	{
		get => Target.GetBool( "b_grounded" );
		set => Target.Set( "b_grounded", value );
	}

	public bool IsSwimming
	{
		get => Target.GetBool( "b_swim" );
		set => Target.Set( "b_swim", value );
	}

	public bool IsClimbing
	{
		get => Target.GetBool( "b_climbing" );
		set => Target.Set( "b_climbing", value );
	}

	public bool IsNoclipping
	{
		get => Target.GetBool( "b_noclip" );
		set => Target.Set( "b_noclip", value );
	}

	public bool IsWeaponLowered
	{
		get => Target.GetBool( "b_weapon_lower" );
		set => Target.Set( "b_weapon_lower", value );
	}

	public enum HoldTypes
	{
		None,
		Pistol,
		Rifle,
		Shotgun,
		HoldItem,
		Punch,
		Swing,
		RPG
	}

	public HoldTypes HoldType
	{
		get => (HoldTypes)Target.GetInt( "holdtype" );
		set => Target.Set( "holdtype", (int)value );
	}

	HoldTypes GetRandomHoldType( HoldTypes except )
	{
		var holdType = GetRandomHoldType();
		while ( holdType == except )
			holdType = GetRandomHoldType();

		return holdType;
	}


	HoldTypes GetRandomHoldType()
	{
		var rand = Game.Random.Int( 0, 4 );
		switch ( rand )
		{
			case 0: default: return HoldTypes.Pistol;
			case 1: return HoldTypes.Shotgun;
			case 2: return HoldTypes.HoldItem;
			case 3: return HoldTypes.Punch;
			case 4: return HoldTypes.RPG;
		}
	}

	public enum Hand
	{
		Both,
		Right,
		Left
	}

	public Hand Handedness
	{
		get => (Hand)Target.GetInt( "holdtype_handedness" );
		set => Target.Set( "holdtype_handedness", (int)value );
	}

	public void TriggerJump()
	{
		Target.Set( "b_jump", true );
	}

	public void TriggerDeploy()
	{
		Target.Set( "b_deploy", true );
	}

	public enum MoveStyles
	{
		Auto,
		Walk,
		Run
	}

	/// <summary>
	/// We can force the model to walk or run, or let it decide based on the speed.
	/// </summary>
	public MoveStyles MoveStyle
	{
		get => (MoveStyles)Target.GetInt( "move_style" );
		set => Target.Set( "move_style", (int)value );
	}
}
