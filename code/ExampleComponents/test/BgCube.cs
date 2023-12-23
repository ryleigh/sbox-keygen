using Sandbox;
using Sandbox.Physics;
using System.Drawing;
using System.Reflection.Metadata;
using System.Runtime;

public class BgCube : Component
{
	private ModelRenderer _modelRenderer;

	public Vector3 Velocity { get; set; }
	public Vector3 RotSpeed { get; set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		_modelRenderer = Components.Get<ModelRenderer>();

		Restart();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		float dt = Time.Delta;

		Transform.Position += Velocity * dt;

		if(Transform.Position.y > 6600f || Transform.Position.y < -6600f || Transform.Position.z < -5000f || Transform.Position.z > 5000f)
		{
			Restart();
		}

		Transform.Rotation = Rotation.From( RotSpeed.x, RotSpeed.y, RotSpeed.z ) * Time.Now;
	}

	void Restart()
	{
		float x = Game.Random.Float( 1500f, 3000f );
		float y;
		float z;
		Vector3 targetPos;

		int rand = Game.Random.Int( 0, 3 );
		switch(rand)
		{
			case 0: default: // bottom
				y = Game.Random.Float(-6000f, 6000f);
				z = -3500f;
				targetPos = new Vector3( x, Game.Random.Float( -2000f, 2000f ), 0f );
				break;
			case 1: // top
				y = Game.Random.Float( -6000f, 6000f );
				z = 3700f;
				targetPos = new Vector3( x, Game.Random.Float( -2000f, 2000f ), 0f );
				break;
			case 2: // left
				y = 6000f;
				z = Game.Random.Float( -3500f, 3500f);
				targetPos = new Vector3( x, 0f, Game.Random.Float( -1100f, 1100f ) );
				break;
			case 3: // right
				y = -6000f;
				z = Game.Random.Float( -3500f, 3500f );
				targetPos = new Vector3( x, 0f, Game.Random.Float( -1100f, 1100f ) );
				break;
		}

		Transform.Position = new Vector3( x, y, z );
		Velocity = (targetPos - Transform.Position).Normal * Game.Random.Float( 150f, 2500f );

		Transform.LocalScale = new Vector3( Game.Random.Float( 0.5f, 2.5f ), Game.Random.Float( 0.5f, 2.5f ), Game.Random.Float( 0.5f, 2.5f ) ) * 10f;

		Transform.Rotation = Rotation.Random;
		RotSpeed = new Vector3( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) ) * 50f;

		//_modelRenderer.Tint = new Color( 0f, Game.Random.Float(0.002f, 0.004f), Game.Random.Float( 0.002f, 0.004f ), 0.5f );
		_modelRenderer.Tint = new Color( 0f, Game.Random.Float( 0.004f, 0.008f ), Game.Random.Float( 0.004f, 0.008f ), 0.5f );
	}
}
