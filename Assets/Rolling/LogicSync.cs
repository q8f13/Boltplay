using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LogicSync
{
	private static LogicSync _instance = new LogicSync();
	public static LogicSync Instance{
		get{
			if(_instance == null)
				_instance = new LogicSync();
			return _instance;
		}
	}

	private LogicSync()
	{
	}

	public void Tick()
	{

	}

	public void UpdateRigidbody(Rigidbody rig, InputMsg input)
	{

	}
}

public class InputMsg
{
	public long TickNumber;
	public Vector3 Position;
	public Quaternion Rotation;
	public Vector3 Velocity;
	public Vector3 AngularVelocity;
}