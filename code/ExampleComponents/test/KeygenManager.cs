using Sandbox;
using Sandbox.Physics;
using System.Drawing;
using System.Runtime;
using static KeygenCitizenAnimator;

public class KeygenManager : Component
{
	public float MusicVolume { get; set; }
	public MusicPlayer MusicPlayer { get; private set; }
	public float ElapsedTime { get; private set; }

	[Property] public GameObject CubePrefab { get; set; }
	[Property] public GameObject CitizenPrefab { get; set; }
	[Property] public GameObject BgCubePrefab { get; set; }
	[Property] public GameObject EmojiWorldPrefab { get; set; }

	private List<GameObject> _cubes = new List<GameObject>();
	private GameObject _cubeContainer;

	private List<GameObject> _citizens = new List<GameObject>();

	private const int SPEC_MAX = 512;

	private const int SEGMENT_SIZE = 64;
	public float[] SegmentAverages = new float[SPEC_MAX / SEGMENT_SIZE];

	[Property] public CameraComponent Camera { get; set; }

	public TimeSince TimeSinceBeat { get; private set; }
	private float _lastBeatTime;
	public const float BEAT_DELAY = 0.395f;
	private const float STARTING_BEAT_TIME_OFFSET = -0.3f;
	public int NumBeats { get; private set; }
	private float _lastPlaybackTime;

	public int CurrBeatsInSection { get; private set; }
	public int CurrSectionLength { get; private set; }
	public int SectionNum { get; private set; }

	public GameObject MainCitizen { get; private set; }

	public List<GameObject> BgCitizens = new List<GameObject>();
	private const float BG_CITIZEN_Y_LIMIT = 600f;

	public Color FrameColor { get; private set; }
	private Color _oldFrameColor;
	public Color _targetFrameColor;
	private TimeSince _timeSinceFrameColor;
	private float _colorChangeTime = 16f;

	public Color BgFlashColor { get; private set; }
	private float _bgFlashTime;
	private EasingType _bgFlashEasingType = EasingType.QuadIn;

	public Color BarColorHigh { get; private set; }
	public Color BarColorLow { get; private set; }
	public Color TargetBarColorHigh { get; private set; }
	public Color TargetBarColorLow { get; private set; }

	private EasingType _bgCitizenEasingType = EasingType.Linear;

	private TimeSince _timeSinceFlying;
	private float _flyingDelay;
	private int _flyingNum;

	private KeygenHud _keygenHud;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		var time = Time.Now;
		var realTime = RealTime.Now;

		ElapsedTime = 0f;

		_cubeContainer = Scene.GetAllObjects( true ).Where( x => x.Name == "CubeContainer" ).FirstOrDefault();

		Camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		_keygenHud = Scene.GetAllComponents<KeygenHud>().FirstOrDefault();

		if ( _cubeContainer != null )
			_cubeContainer.Destroy();

		_cubeContainer = Scene.CreateObject( true );
		_cubeContainer.Name = "CubeContainer";

		MusicPlayer = MusicPlayer.Play( FileSystem.Mounted, $"test/music/varg_stars.mp3" );
		//MusicPlayer = MusicPlayer.Play( FileSystem.Mounted, $"test/music/varg_stars_mini.wav" );
		MusicPlayer.Volume = 0f;
		MusicPlayer.Repeat = true;
		MusicPlayer.ListenLocal = true;

		TimeSinceBeat = 0f;
		_lastBeatTime = STARTING_BEAT_TIME_OFFSET;
		_lastPlaybackTime = 0f;
		CurrSectionLength = Game.Random.Int( 2, 4 ) * 4;

		int numCubes = SPEC_MAX;
		for (int i = 0; i < numCubes; i += 2)
		{
			var pos = new Vector3( 0f, 380f - i * 1.5f, 0f );
			var cube = SceneUtility.Instantiate( CubePrefab, pos);
			cube.Parent = _cubeContainer;
			cube.Transform.Scale = new Vector3( Utils.Map(i, 0, numCubes, 0.05f, 0.2f), 0.05f, 0.01f );
			cube.Name = $"cube_{i}";
			//cube.Components.Get<ModelRenderer>().ShouldCastShadows = false;
			_cubes.Add( cube );
		}

		BarColorHigh = TargetBarColorHigh = Color.Red;
		BarColorLow = TargetBarColorLow = Color.Blue;

		MainCitizen = SpawnCitizenObj( new Vector3(-245f, -24f, 150f), Rotation.FromYaw( 180f ) );
		//MainCitizen.Components.Get<KeygenPlayer>().Eye.Transform.LocalPosition = new Vector3( 0f, -1000f, 0f );
		MainCitizen.Components.Get<KeygenCitizenAnimator>().TargetPosition = MainCitizen.Transform.Position;
		InitializeMainCitizen();

		for (int i = 0; i < 24; i++)
		{
			var bgCitizenObj = SpawnCitizenObj( new Vector3( 30f, BG_CITIZEN_Y_LIMIT - 50f * i, 100f ), Rotation.FromYaw( 180f ) );
			var bgAnim = bgCitizenObj.Components.Get<KeygenCitizenAnimator>();
			bgAnim.NoclipMode = NoclipMode.Enabled;
			bgAnim.NoclipModeModulus = 1;
			bgAnim.SwimmingModeModulus = 1;
			BgCitizens.Add( bgCitizenObj );
		}

		FrameColor = _oldFrameColor = new Color( 0.1f, 0.1f, 0.1f );
		_targetFrameColor = Utils.GetRandomColor();
		_timeSinceFrameColor = 0f;

		BgFlashColor = new Color( Game.Random.Float( 0f, 0.02f ), Game.Random.Float( 0f, 0.02f ), Game.Random.Float( 0f, 0.02f ) );
		_bgFlashTime = Game.Random.Float( 0.1f, 0.35f );

		for ( int i = 0; i < 8; i++ )
			SpawnBgCube();

		_flyingDelay = Game.Random.Float( 2f, 3f );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Log.Info( $"{MusicPlayer.Amplitude}" );
		//Log.Info( $"{MusicPlayer.PlaybackTime}" );

		if( MusicPlayer.PlaybackTime < _lastPlaybackTime)
		{
			_lastBeatTime = MusicPlayer.PlaybackTime - BEAT_DELAY + STARTING_BEAT_TIME_OFFSET;
			//Log.Info( $"PlaybackTime: {MusicPlayer.PlaybackTime}, _lastBeatTime: {_lastBeatTime}" );
		}

		_lastPlaybackTime = MusicPlayer.PlaybackTime;

		if( MusicPlayer.PlaybackTime > _lastBeatTime + BEAT_DELAY)
		{
			_lastBeatTime = MusicPlayer.PlaybackTime;
			TimeSinceBeat = 0f;
			NumBeats++;

			//Sound.Play( "beep" );

			OnBeat();
		}

		float dt = Time.Delta;

		float MAX_VOLUME = 0.5f;
		MusicVolume = Utils.Map( ElapsedTime, 0f, 5f, 0f, MAX_VOLUME, EasingType.Linear );
		MusicPlayer.Volume = MusicVolume;

		ElapsedTime += dt;

		float SCALE_FACTOR = 1f * (MusicVolume / MAX_VOLUME);
		float lerpSpeed = Utils.Map( Utils.FastSin( Time.Now * 0.5f ), -1f, 1f, 0.03f, 0.1f );

		float[] _segmentSums = new float[SPEC_MAX / SEGMENT_SIZE];

		BarColorHigh = Color.Lerp( BarColorHigh, TargetBarColorHigh, dt * 0.9f );
		BarColorLow = Color.Lerp( BarColorLow, TargetBarColorLow, dt * 0.9f );

		int i = 0;
		foreach(var sample in MusicPlayer.Spectrum)
		{
			if ( i % 2 != 0 )
			{
				i++;
				continue;
			}

			if ( i % 2 != 0 )
			{
				i++;
				continue;
			}

			int index = i / 2;

			if ( index >= _cubes.Count )
				break;

			var cube = _cubes[index];
			float target = sample * SCALE_FACTOR;
			float current = cube.Transform.Scale.z;

			float lerped = current + (target - current) * lerpSpeed * dt * 100f;

			cube.Transform.Scale = new Vector3( cube.Transform.Scale.x, cube.Transform.Scale.y, lerped );
			cube.Components.Get<ModelRenderer>().Tint = Color.Lerp( BarColorLow, BarColorHigh, Utils.Map( lerped, 0f, 5f, 0f, 1f, EasingType.QuadIn) );
			cube.Name = $"cube_{index} ({lerped})";

			int segment = MathX.FloorToInt( i / SEGMENT_SIZE );
			_segmentSums[segment] += lerped;

			i++;
		}

		for(int seg = 0; seg < SPEC_MAX / SEGMENT_SIZE; seg++ )
		{
			var val = _segmentSums[seg] / SEGMENT_SIZE;
			SegmentAverages[seg] = float.IsNaN( val ) ? 0f : val;
		}

		// main citizen
		var mainCitizenAnim = MainCitizen.Components.Get<KeygenCitizenAnimator>();
		//mainCitizenAnim.HeightMode = HeightMode.Enabled;
		//mainCitizenAnim.AttackMode = AttackMode.Enabled;
		//mainCitizenAnim.HoldTypeMode = HoldTypeMode.HoldNone;

		//mainCitizenAnim.HandIKMode = NumBeats % 4 <= 1 ? HandIKMode.Enabled : HandIKMode.None;
		//mainCitizenAnim.FootIKMode = NumBeats % 5 <= 3 ? FootIKMode.Enabled : FootIKMode.None;
		//mainCitizenAnim.LookDirMode = NumBeats % 4 <= 1 ? LookDirMode.Enabled : LookDirMode.None;
		//mainCitizenAnim.AnimMoveMode = NumBeats % 5 <= 3 ? AnimMoveMode.Enabled : AnimMoveMode.None;
		//mainCitizenAnim.NoclipMode = NoclipMode.Enabled;

		//mainCitizenAnim.MoveMode = MoveMode.TargetPos;
		//MainCitizen.Transform.Position = MainCitizen.Transform.Position.WithX( Utils.Map(TimeSinceBeat, 0f, BEAT_DELAY, -260f, -245f, EasingType.QuadOut) );

		// background citizens
		int citizenNum = 0;
		float scrollSpeed = (100f + (NumBeats % 12f) * 20f) * (NumBeats % 16 < 8 ? -1f : 1f);
		foreach(var citizenObj in BgCitizens)
		{
			citizenObj.Transform.Position += new Vector3( 0f, -1f, 0f ) * scrollSpeed * dt;
			var posZ = 100f + Utils.FastSin( citizenNum + Time.Now * 4f ) * (40f + Utils.FastSin(Time.Now * 0.5f) * 40f);
			citizenObj.Transform.Position = citizenObj.Transform.Position.WithZ( posZ );

			var posY = citizenObj.Transform.Position.y;

			if ( posY < -BG_CITIZEN_Y_LIMIT )
				citizenObj.Transform.Position = new Vector3( citizenObj.Transform.Position.x, posY + BG_CITIZEN_Y_LIMIT * 2f, citizenObj.Transform.Position.z );
			else if ( posY > BG_CITIZEN_Y_LIMIT )
				citizenObj.Transform.Position = new Vector3( citizenObj.Transform.Position.x, posY - BG_CITIZEN_Y_LIMIT * 2f, citizenObj.Transform.Position.z );

			float average = SegmentAverages[citizenNum % SegmentAverages.Count()];
			var anim = citizenObj.Components.Get<KeygenCitizenAnimator>();
			anim.Height = Utils.DynamicEaseTo( anim.Height, Utils.Map( average, 0f, 11f, 0f, 2f, _bgCitizenEasingType ), 0.2f, dt);

			var colorA = Color.Lerp( BarColorLow, BarColorHigh, Utils.Map( average, 0f, 5f, 0f, 1f, EasingType.SineIn ) ).WithAlpha( Utils.Map( average, 0f, 1f, 0f, 1f, EasingType.Linear ) );
			var colorB = colorA * 0.9f;
			var color = Color.Lerp( colorA, colorB, Utils.FastSin(Time.Now * 64f));
			citizenObj.Children.Find( x => x.Name == "Body" ).Components.Get<ModelRenderer>().Tint = color;

			citizenObj.Transform.Scale = Utils.Map( SegmentAverages[citizenNum % SegmentAverages.Count()], 0f, 3f, 0.9f, 1.4f );


			citizenNum++;
		}

		if( _timeSinceFrameColor > _colorChangeTime)
		{
			FrameColor = _oldFrameColor = _targetFrameColor;
			_timeSinceFrameColor = 0f;
			_targetFrameColor = Utils.GetRandomColor();
		}
		else
		{
			FrameColor = Color.Lerp( _oldFrameColor, _targetFrameColor, Utils.Map( _timeSinceFrameColor, 0f, _colorChangeTime, 0f, 1f ) );
		}

		Camera.BackgroundColor = Color.Lerp( BgFlashColor, Color.Black, Utils.Map(TimeSinceBeat, 0f, _bgFlashTime, 0f, 1f, _bgFlashEasingType ) );

		if(_timeSinceFlying > _flyingDelay)
		{
			SpawnEmojiWorldPanel( _flying[_flyingNum % _flying.Length], goingRight: true );
			_flyingDelay = Game.Random.Float( 3f, 5f );
			_timeSinceFlying = 0f;
			_flyingNum++;
		}
	}

	void InitializeMainCitizen()
	{
		var anim = MainCitizen.Components.Get<KeygenCitizenAnimator>();
		anim.HeightMode = HeightMode.Enabled;

		//switch( Game.Random.Int( 0, 5 ) )
		//{
		//	case 0: anim.HoldTypeMode = HoldTypeMode.None; break;
		//	case 1: anim.HoldTypeMode = HoldTypeMode.HoldNone; break;
		//	case 2: anim.HoldTypeMode = HoldTypeMode.PunchShotgun; break;
		//	case 3: anim.HoldTypeMode = HoldTypeMode.RiflePistol; break;
		//	case 4: anim.HoldTypeMode = HoldTypeMode.RifleRpg; break;
		//	case 5: anim.HoldTypeMode = HoldTypeMode.PistolNone; break;
		//}

		//anim.FootIKMode = FootIKMode.Shuffle;
	}

	void OnBeat()
	{
		foreach ( var citizenObj in _citizens )
		{
			citizenObj.Components.Get<KeygenPlayer>().OnBeat();
			citizenObj.Components.Get<KeygenCitizenAnimator>().OnBeat();
		}

		_keygenHud.OnBeat();

		var anim = MainCitizen.Components.Get<KeygenCitizenAnimator>();

		//anim.AttackMode = AttackMode.Enabled;
		//anim.AttackModeModulus = 4;
		//anim.AttackModeModulusInverted = true;
		//anim.AnimMoveMode = AnimMoveMode.None;
		//anim.GroundedMode = GroundedMode.Enabled;

		CurrBeatsInSection++;

		if(CurrBeatsInSection >= CurrSectionLength )
		{
			CurrBeatsInSection = 0;
			CurrSectionLength = Game.Random.Int( 1, 9 ) * 4;
			SectionNum++;
			OnSection();
		}
	}

	void OnSection()
	{
		foreach ( var citizenObj in _citizens )
		{
			citizenObj.Components.Get<KeygenPlayer>().OnSection();
			citizenObj.Components.Get<KeygenCitizenAnimator>().OnSection();
		}

		int bgNum = 0;
		float spinFactor = Game.Random.Float( 0f, 1f ) < 0.5f ? 1f : -1f;
		bool spinSameDir = Game.Random.Float( 0f, 1f ) < 0.5f;
		float bgSpinSpeed = Game.Random.Float( 30f, 650f );
		bool altHoldTypes = Game.Random.Float( 0f, 1f ) < 0.4f;
		Vector3 animVelocity = Rotation.Random.Angles().AsVector3() * Game.Random.Float( -1.5f, 1.5f );

		HoldTypeMode bgHoldType1 = HoldTypeMode.None;
		switch ( Game.Random.Int( 0, 5 ) )
		{
			case 1: bgHoldType1 = HoldTypeMode.HoldNone; break;
			case 2: bgHoldType1 = HoldTypeMode.PunchShotgun; break;
			case 3: bgHoldType1 = HoldTypeMode.RiflePistol; break;
			case 4: bgHoldType1 = HoldTypeMode.RifleRpg; break;
			case 5: bgHoldType1 = HoldTypeMode.PistolNone; break;
		}

		HoldTypeMode bgHoldType2 = HoldTypeMode.None;
		switch ( Game.Random.Int( 0, 5 ) )
		{
			case 1: bgHoldType2 = HoldTypeMode.HoldNone; break;
			case 2: bgHoldType2 = HoldTypeMode.PunchShotgun; break;
			case 3: bgHoldType2 = HoldTypeMode.RiflePistol; break;
			case 4: bgHoldType2 = HoldTypeMode.RifleRpg; break;
			case 5: bgHoldType2 = HoldTypeMode.PistolNone; break;
		}

		VoiceMode bgVoiceMode = VoiceMode.None;
		switch ( Game.Random.Int( 0, 3 ) )
		{
			case 1: bgVoiceMode = VoiceMode.Alternate; break;
			case 2: bgVoiceMode = VoiceMode.Chomp; break;
			case 3: bgVoiceMode = VoiceMode.ChompReverse; break;
		}

		LookDirMode lookDirMode = LookDirMode.None;
		switch ( Game.Random.Int( 0, 5 ) )
		{
			case 1: lookDirMode = LookDirMode.Forward; break;
			case 2: lookDirMode = LookDirMode.Random; break;
			case 3: lookDirMode = LookDirMode.UpDown; break;
			case 4: lookDirMode = LookDirMode.Wobble; break;
			case 5: lookDirMode = LookDirMode.Headbang; break;
		}

		FootIKMode footIKMode = FootIKMode.None;
		int footIKModulus = 1;
		switch ( Game.Random.Int( 0, 1 ) )
		{
			case 0:
				footIKMode = FootIKMode.Crazy;
				footIKModulus = Game.Random.Int( 0, 1 ) == 0 ? 1 : Game.Random.Int( 1, 4 );
				break;
			case 1:
				footIKMode = FootIKMode.Shuffle;
				footIKModulus = Game.Random.Int( 0, 1 ) == 0 ? 1 : Game.Random.Int( 1, 4 );
				break;
		}
		
		int handIKModulus = Game.Random.Int( 0, 1 ) == 0 ? 1 : Game.Random.Int( 1, 4 );

		foreach ( var bgCitizen in BgCitizens )
		{
			var bgAnim = bgCitizen.Components.Get<KeygenCitizenAnimator>();

			if(Game.Random.Float(0f, 1f) < 0.1f)
			{
				bgAnim.SpinMode = SpinMode.Constant;
				bgAnim.SpinSpeed = bgSpinSpeed * spinFactor * ((spinSameDir || bgNum % 2 == 0) ? 1f : -1f);
			}
			else if (Game.Random.Float(0f, 1f) < 0.2f)
			{
				bgAnim.SpinMode = SpinMode.None;
			}

			if ( Game.Random.Float( 0f, 1f ) < 0.2f )
			{
				bgAnim.AnimMoveMode = AnimMoveMode.Steady;
				bgAnim.AnimVelocity = animVelocity;
			}
			else if ( Game.Random.Float( 0f, 1f ) < 0.2f )
			{
				bgAnim.AnimMoveMode = AnimMoveMode.Still;
			}

			if ( Game.Random.Float( 0f, 1f ) < 0.2f )
			{
				bgAnim.FootIKMode = footIKMode;
				bgAnim.FootIKModulus = footIKModulus;
			}
			else if ( Game.Random.Float( 0f, 1f ) < 0.2f )
			{
				bgAnim.FootIKMode = FootIKMode.None;
			}

			if ( Game.Random.Float( 0f, 1f ) < 0.2f )
			{
				bgAnim.HandIKMode = HandIKMode.Enabled;
				bgAnim.HandIKModulus = handIKModulus;
			}
			else if ( Game.Random.Float( 0f, 1f ) < 0.2f )
			{
				bgAnim.HandIKMode = HandIKMode.None;
			}

			if ( Game.Random.Float( 0f, 1f ) < 0.15f )
			{
				bgAnim.HoldTypeMode = altHoldTypes
					? (bgNum % 2 == 0 ? bgHoldType1 : bgHoldType2)
					: bgHoldType1;
			}
			else if ( Game.Random.Float( 0f, 1f ) < 0.25f )
			{
				bgAnim.HoldTypeMode = HoldTypeMode.None;
			}

			if ( Game.Random.Float( 0f, 1f ) < 0.2f ) bgAnim.VoiceMode = bgVoiceMode;
			else if ( Game.Random.Float( 0f, 1f ) < 0.1f ) bgAnim.VoiceMode = VoiceMode.None;

			if ( Game.Random.Float( 0f, 1f ) < 0.2f ) bgAnim.SwimmingMode = SwimmingMode.Enabled;
			else if ( Game.Random.Float( 0f, 1f ) < 0.1f ) bgAnim.SwimmingMode = SwimmingMode.None;

			if ( Game.Random.Float( 0f, 1f ) < 0.2f ) bgAnim.NoclipMode = NoclipMode.Enabled;
			else if ( Game.Random.Float( 0f, 1f ) < 0.1f ) bgAnim.NoclipMode = NoclipMode.None;

			if ( Game.Random.Float( 0f, 1f ) < 0.2f ) bgAnim.AttackMode = AttackMode.Enabled;
			else if ( Game.Random.Float( 0f, 1f ) < 0.1f ) bgAnim.AttackMode = AttackMode.None;

			if ( Game.Random.Float( 0f, 1f ) < 0.2f ) bgAnim.LookDirMode = lookDirMode;
			else if ( Game.Random.Float( 0f, 1f ) < 0.1f ) bgAnim.LookDirMode = LookDirMode.None;

			bgNum++;
		}

		switch ( Game.Random.Int( 0, 10 ) )
		{
			case 0:
			case 1:
			case 3:
			case 4: break;
			case 5:
			case 6: _bgCitizenEasingType = EasingType.Linear; break;
			case 7: _bgCitizenEasingType = EasingType.QuadIn; break;
			case 8: _bgCitizenEasingType = EasingType.ExtremeOut; break;
			case 9: _bgCitizenEasingType = EasingType.QuadOut; break;
			case 10: _bgCitizenEasingType = EasingType.ExpoOut; break;
		}

		if ( Game.Random.Float(0f, 1f) < Utils.Map( SectionNum, 3, 0f, 16f, 0.6f))
		{
			switch ( Game.Random.Int( 0, 16 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 6: break;
				case 7:
				case 8:
				case 9:
				case 10:
				case 11:
					TargetBarColorHigh = Color.Red;
					TargetBarColorLow = Color.Blue;
					break;
				case 12:
					TargetBarColorHigh = new Color( 0f, 0.9f, 0f );
					TargetBarColorLow = new Color( 0f, 0.02f, 0f );
					break;
				case 13:
					TargetBarColorHigh = new Color( 0.9f, 0f, 0.9f );
					TargetBarColorLow = new Color( 0f, 0f, 0.5f );
					break;
				case 14:
					TargetBarColorHigh = new Color( 0.4f, 0.4f, 0f );
					TargetBarColorLow = new Color( 0.01f, 0.01f, 0.01f );
					break;
				case 15:
					TargetBarColorHigh = new Color( 0.3f, 0.3f, 0.3f );
					TargetBarColorLow = new Color( 0.005f, 0.005f, 0.005f );
					break;
				case 16:
					TargetBarColorHigh = new Color( 0f, 0f, 1f );
					TargetBarColorLow = new Color( 0f, 0.2f, 0.2f );
					break;
				case 17:
					TargetBarColorHigh = new Color( 0f, 0f, 1f );
					TargetBarColorLow = new Color( 0f, 1f, 0f );
					break;
				case 18:
					TargetBarColorHigh = new Color( 1f, 0f, 0f );
					TargetBarColorLow = new Color( 1f, 1f, 0f );
					break;
			}
		}

		BgFlashColor = new Color( Game.Random.Float( 0f, 0.02f ), Game.Random.Float( 0f, 0.02f ), Game.Random.Float( 0f, 0.02f ) );
		_bgFlashTime = Game.Random.Float( 0.1f, 0.35f );

		var anim = MainCitizen.Components.Get<KeygenCitizenAnimator>();

		if ( SectionNum >= Game.Random.Int( 4, 6 ) )
		{
			switch ( Game.Random.Int( 0, 7 ) )
			{
				case 0:
				case 1:
				case 3: break;
				case 4:
				case 6: anim.HeightMode = HeightMode.Enabled; break;
				case 7: anim.HeightMode = HeightMode.None; break;
			}
		}

		switch ( Game.Random.Int( 0, 11 ) )
		{
			case 1:
			case 2:
			case 3:
			case 4: break;
			case 5:
			case 6: anim.HoldTypeMode = HoldTypeMode.None; break;
			case 7: anim.HoldTypeMode = HoldTypeMode.HoldNone; break;
			case 8: anim.HoldTypeMode = HoldTypeMode.PunchShotgun; break;
			case 9: anim.HoldTypeMode = HoldTypeMode.RiflePistol; break;
			case 10: anim.HoldTypeMode = HoldTypeMode.RifleRpg; break;
			case 11: anim.HoldTypeMode = HoldTypeMode.PistolNone; break;
		}

		if(SectionNum >= 4 && Game.Random.Int(0, 9) == 0)
		{
			anim.AllHoldTypes = true;
			anim.AllHoldTypesLength = 4 * Game.Random.Int( 1, 3 );
		}
		else
		{
			anim.AllHoldTypes = false;
		}

		switch ( Game.Random.Int( 0, 6 ) )
		{
			case 0:
			case 1:
			case 2: break;
			case 3: anim.VoiceMode = VoiceMode.None; break;
			case 4: anim.VoiceMode = VoiceMode.Alternate; break;
			case 5: anim.VoiceMode = VoiceMode.Chomp; break;
			case 6: anim.VoiceMode = VoiceMode.ChompReverse; break;
		}

		switch ( Game.Random.Int( 0, 8 ) )
		{
			case 0:
			case 1:
			case 2:
			case 3: break;
			case 4:
			case 5: anim.AttackMode = AttackMode.None; break;
			case 6:
			case 7:
			case 8:
				anim.AttackMode = AttackMode.Enabled;
				anim.AttackModeModulusInverted = Game.Random.Int( 0, 1 ) == 0;
				anim.AttackModeModulus = anim.AttackModeModulusInverted
					? (Game.Random.Int( 0, 1 ) == 0 ? 3 : Game.Random.Int( 2, 4 ))
					: (Game.Random.Int( 0, 1 ) == 0 ? 2 : Game.Random.Int( 1, 4 ));
				break;
		}

		switch ( Game.Random.Int( 0, 8 ) )
		{
			case 0:
			case 1:
			case 2:
			case 3: 
			case 4: break;
			case 5: 
			case 6:
			case 7: anim.GroundedMode = GroundedMode.None; break;
			case 8:
				anim.GroundedMode = GroundedMode.Enabled;
				anim.GroundedModeModulusInverted = Game.Random.Int( 0, 1 ) == 0;
				anim.GroundedModeModulus = anim.GroundedModeModulusInverted
					? (Game.Random.Int( 0, 1 ) == 0 ? 8 : Game.Random.Int( 1, 5 ))
					: (Game.Random.Int( 0, 2 ) < 2 ? 1 : Game.Random.Int( 1, 4 ));
				anim.GroundedModeLength = Game.Random.Float(0.1f, BEAT_DELAY);
				break;
		}

		if (SectionNum >= 6)
		{
			switch ( Game.Random.Int( 0, 7 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3: break;
				case 4:
				case 5:
				case 6: anim.HoldTypeBeatModulus = 2; break;
				case 7: anim.HoldTypeBeatModulus = 3; break;
			}
		}

		if(SectionNum >= Game.Random.Int(2, 5))
		{
			switch ( Game.Random.Int( 0, 9 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3: anim.LookDirMode = LookDirMode.None; break; // continue looking current direction
				case 4: anim.LookDirMode = LookDirMode.Forward; break;
				case 5: anim.LookDirMode = LookDirMode.Random; break;
				case 6: anim.LookDirMode = LookDirMode.UpDown; break;
				case 7: anim.LookDirMode = LookDirMode.Wobble; break;
				case 8:
				case 9: anim.LookDirMode = LookDirMode.Headbang; break;
			}

			switch ( Game.Random.Int( 0, 6 ) )
			{
				case 0: default: anim.LookDirBeatModulus = 2; break;
				case 1: anim.LookDirBeatModulus = 3; break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 2, 5 ) )
		{
			switch ( Game.Random.Int( 0, 6 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3: break; // keep doing same thing
				case 4:
				case 5: anim.MoveMode = MoveMode.TargetPos; break;
				case 6: 
					anim.MoveMode = MoveMode.BackAndForth;
					anim.MoveModeSpeed = 20f * Game.Random.Int( 1, 5 );
					anim.MoveModeSmooth = Game.Random.Int( 0, 2 ) == 0;
					break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 2, 5 ) )
		{
			switch ( Game.Random.Int( 0, 10 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3: break; // keep doing same thing
				case 4:
				case 6:
				case 7:
				case 8: anim.AnimMoveMode = AnimMoveMode.Still; break;
				case 9: anim.AnimMoveMode = AnimMoveMode.RandomToggle; break;
				case 10:
					anim.AnimMoveMode = AnimMoveMode.Steady;
					anim.AnimVelocity = Rotation.Random.Angles().AsVector3() * Game.Random.Float( -1.5f, 1.5f );
					break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 2, 4 ) )
		{
			switch ( Game.Random.Int( 0, 7 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3: break;
				case 4:
				case 5:
				case 6: anim.SpinMode = SpinMode.None; break;
				case 7:
					anim.SpinMode = SpinMode.Constant;
					anim.SpinSpeed = Game.Random.Float( 15f, 55f ) * (Game.Random.Int( 0, 1 ) == 0 ? -1f : 1f);
					break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 4, 6 ) )
		{
			switch ( Game.Random.Int( 0, 7 ) )
			{
				case 0:
				case 1:
				case 2: break; // keep doing same thing
				case 3:
				case 4:
				case 5:
				case 6: anim.DuckMode = DuckMode.None; break;
				case 7:
					anim.DuckMode = DuckMode.Enabled;
					anim.DuckModeModulus = Game.Random.Int( 0, 5 ) == 0 ? 8 : Game.Random.Int( 1, 4 );
					break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 4, 7 ) )
		{
			switch ( Game.Random.Int( 0, 7 ) )
			{
				case 0:
				case 1:
				case 2: break; // keep doing same thing
				case 3:
				case 4:
				case 5:
				case 6: anim.JumpMode = JumpMode.None; break;
				case 7:
					anim.JumpMode = JumpMode.Enabled;
					anim.JumpModeModulus = Game.Random.Int( 0, 5 ) == 0 ? 8 : Game.Random.Int( 1, 4 );
					break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 3, 6 ) )
		{
			switch ( Game.Random.Int( 0, 9 ) )
			{
				case 0:
				case 1:
				case 2: break; // keep doing same thing
				case 3:
				case 4:
				case 5:
				case 6: anim.SpecialMovementMode = SpecialMovementMode.None; break;
				case 7: anim.SpecialMovementMode = SpecialMovementMode.Flail; break;
				case 8: anim.SpecialMovementMode = SpecialMovementMode.Roll; break;
				case 9: anim.SpecialMovementMode = SpecialMovementMode.Climb; break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 3, 8 ) )
		{
			switch ( Game.Random.Int( 0, 7 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3: break;
				case 4: 
				case 5:
				case 6: anim.SittingMode = SittingMode.None; break;
				case 7: anim.SittingMode = SittingMode.Enabled; break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 2, 8 ) )
		{
			switch ( Game.Random.Int( 0, 8 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3: break;
				case 4:
				case 5:
				case 6:
				case 7: anim.NoclipMode = NoclipMode.None; break;
				case 8:
					anim.NoclipMode = NoclipMode.Enabled;
					anim.NoclipModeModulus = Game.Random.Int( 0, 1 ) == 0 ? 2 : Game.Random.Int( 1, 4 );
					break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 4, 12 ) )
		{
			switch ( Game.Random.Int( 0, 8 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3: break;
				case 4:
				case 5:
				case 6:
				case 7: anim.SwimmingMode = SwimmingMode.None; break;
				case 8:
					anim.SwimmingMode = SwimmingMode.Enabled;
					anim.SwimmingModeModulus = Game.Random.Int( 0, 1 ) == 0 ? 2 : Game.Random.Int( 1, 4 );
					break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 2, 8 ) )
		{
			switch ( Game.Random.Int( 0, 9 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3: 
				case 4: break;
				case 5:
				case 6:
				case 7: anim.FootIKMode = FootIKMode.None; break;
				case 8: 
					anim.FootIKMode = FootIKMode.Crazy;
					anim.FootIKModulus = Game.Random.Int( 0, 1 ) == 0 ? 1 : Game.Random.Int( 1, 4 );
					break;
				case 9: 
					anim.FootIKMode = FootIKMode.Shuffle;
					anim.FootIKModulus = Game.Random.Int( 0, 1 ) == 0 ? 1 : Game.Random.Int( 1, 4 );
					break;
			}
		}

		if ( SectionNum >= Game.Random.Int( 1, 4 ) )
		{
			switch ( Game.Random.Int( 0, 6 ) )
			{
				case 0:
				case 1:
				case 2:
				case 3: break;
				case 4:
				case 5: anim.HandIKMode = HandIKMode.None; break;
				case 6:
					anim.HandIKMode = HandIKMode.Enabled;
					anim.HandIKModulus = Game.Random.Int( 0, 1 ) == 0 ? 1 : Game.Random.Int( 1, 4 );
					break;
			}
		}
	}

	public float GetMusicSample(int index)
	{
		if ( index >= MusicPlayer.Spectrum.Length )
			return 0f;

		return MusicPlayer.Spectrum[index];
	}

	public GameObject SpawnCitizenObj(Vector3 pos, Rotation rot)
	{
		var citizenObj = SceneUtility.Instantiate( CitizenPrefab, pos, rot );
		citizenObj.Components.Get<KeygenPlayer>().Manager = this;
		citizenObj.Components.Get<KeygenCitizenAnimator>().Manager = this;
		_citizens.Add(citizenObj );

		return citizenObj;
	}

	public GameObject SpawnBgCube()
	{
		var bgCubeObj = SceneUtility.Instantiate( BgCubePrefab, new Vector3(-6000f, -6000f, -6000f) );
		return bgCubeObj;
	}

	public GameObject SpawnEmojiWorldPanel(string text, bool goingRight)
	{
		var emojiObj = SceneUtility.Instantiate( EmojiWorldPrefab, new Vector3( 90f, goingRight ? 590f : -590f, Game.Random.Float(80f, 150f) ) );
		var emoji = emojiObj.Components.Get<EmojiWorldPanel>();
		emoji.Emoji = text;
		emoji.HorizontalSpeed = 120f * (goingRight ? 1f : -1f);

		return emojiObj;
	}

	//void ApplyMainCitizenModes(CitizenAnimation otherAnim)
	//{
	//	var anim = MainCitizen.Components.Get<CitizenAnimation>();

	//	otherAnim.HoldTypeMode = anim.HoldTypeMode;
	//	otherAnim.HandIKMode = anim.HandIKMode;
	//	otherAnim.FootIKMode = anim.FootIKMode;
	//	otherAnim.LookDirMode = anim.LookDirMode;
	//	otherAnim.AnimMoveMode = anim.AnimMoveMode;
	//	otherAnim.HeightMode = anim.HeightMode;
	//	otherAnim.GroundedMode = anim.GroundedMode;
	//	otherAnim.AttackMode = anim.AttackMode;
	//	otherAnim.JumpMode = anim.JumpMode;
	//	otherAnim.VoiceMode = anim.VoiceMode;
	//	otherAnim.NoclipMode = anim.NoclipMode;
	//	otherAnim.SwimmingMode = anim.SwimmingMode;
	//	otherAnim.DuckMode = anim.DuckMode;
	//	otherAnim.SpecialMovementMode = anim.SpecialMovementMode;
	//	otherAnim.MoveMode = anim.MoveMode;
	//	otherAnim.SpinMode = anim.SpinMode;
	//	otherAnim.AllHoldTypes = anim.AllHoldTypes;
	//	otherAnim.AllHoldTypesLength = anim.AllHoldTypesLength;
	//	otherAnim.GroundedModeModulus = anim.GroundedModeModulus;
	//	otherAnim.GroundedModeModulusInverted = anim.GroundedModeModulusInverted;
	//	otherAnim.GroundedModeLength = anim.GroundedModeLength;
	//	otherAnim.SpinSpeed = anim.SpinSpeed;
	//	otherAnim.BaseYaw = anim.BaseYaw;
	//	otherAnim.CurrMainHoldType = anim.CurrMainHoldType;
	//	otherAnim.HoldTypeBeatModulus = anim.HoldTypeBeatModulus;
	//	otherAnim.AttackModeModulus = anim.AttackModeModulus;
	//	otherAnim.AttackModeModulusInverted = anim.AttackModeModulusInverted;
	//	otherAnim.LookDirBeatModulus = anim.LookDirBeatModulus;
	//	otherAnim.DuckModeModulus = anim.DuckModeModulus;
	//	otherAnim.DuckAmount = anim.DuckAmount;
	//	otherAnim.JumpModeModulus = anim.JumpModeModulus;
	//	otherAnim.NoclipModeModulus = anim.NoclipModeModulus;
	//	otherAnim.SwimmingModeModulus = anim.SwimmingModeModulus;
	//	otherAnim.FootIKModulus = anim.FootIKModulus;
	//	otherAnim.HandIKModulus = anim.HandIKModulus;
	//	otherAnim.LookDir = anim.LookDir;
	//	otherAnim.AnimVelocity = anim.AnimVelocity;
	//	otherAnim.TargetPosition = anim.TargetPosition;
	//	otherAnim.TargetSitMode = anim.TargetSitMode;
	//}

	private string[] _flying = new string[] { "🦊", "🥕", "✌️", "🎱", "🦆", "🧀", "🐈", "🎱", "🐬", "🐘", "🖐️", "🍩", "👨‍👩‍👧‍👦", "👨‍👩‍👦", "✡️", "🚗", "🍎", "🖐️", "👨‍👩‍👦", "🐝", "👨‍👩‍👦", "🐜", "🎱", "🔥", "♋", "🍌", "🚪", "🕘", "🥑", "🍰", "⭕" };
}
