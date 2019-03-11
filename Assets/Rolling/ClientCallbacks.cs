using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour(BoltNetworkModes.Client, "rolling")]
public class ClientCallbacks : Bolt.GlobalEventListener {
    private Dictionary<string, BallFighter> _players = new Dictionary<string, BallFighter>();
    // private Dictionary<string, Queue<StateMsg>> _stateMsgReceived = new Dictionary<string, Queue<StateMsg>>();

    // private Queue<StateMsg> _stateMsgReceived = new Queue<StateMsg>();

	// private Dictionary<string, StateMsg[]> _stateReceived = new Dictionary<string, StateMsg[]>();

	private Dictionary<string, int> _stateMsgCount = new Dictionary<string, int>();

	private string _thisClientId;

	public override void OnEvent(PlayerCreated evnt)
	{
		_players[evnt.EntityId].UpdateWhenCreated(evnt);
	}

	public override void OnEvent(StateMsg evnt)
	{
		// if(!_stateMsgReceived.ContainsKey(evnt.EntityId))
		// 	_stateMsgReceived.Add(evnt.EntityId, new Queue<StateMsg>());

        // _stateMsgReceived[evnt.EntityId].Enqueue(evnt);

		StateSnapshot snapshot = new StateSnapshot(evnt);

		if(!_players.ContainsKey(snapshot.EntityId))
		{
			Debug.LogWarningFormat("entity id {0} not attached, ignored", evnt.EntityId);
			return;
		}

		_players[evnt.EntityId].ReceiveState(snapshot);

/* 		if(!_stateReceived.ContainsKey(evnt.EntityId))
			_stateReceived.Add(evnt.EntityId, new StateMsg[1024]);

		int tick = evnt.TickNumber % 1024;
		_stateReceived[evnt.EntityId][tick] = evnt; */
	}

	public override void ControlOfEntityGained(BoltEntity entity)
	{
		_thisClientId = entity.networkId.PackedValue.ToString();
		Debug.LogWarning(string.Format("entity {0} gained control", _thisClientId));
	}

	void AttachPlayer(BoltEntity entity)
	{
		BallFighter bf = entity.GetComponent<BallFighter>();
		if(bf == null)
		{
			Debug.LogErrorFormat("ballFighter component should not be null on entity {0}", entity.networkId.PackedValue);
			return;
		}
		string id = entity.networkId.PackedValue.ToString();
		_players.Add(id, bf);
	}

	void DetachPlayer(BoltEntity entity)
	{
		string id = entity.networkId.PackedValue.ToString();
		// _players[id].SelfDestroy();
		_players.Remove(id);

		Debug.LogWarning(string.Format("entity {0} disconnected", id));
	}

	public override void EntityDetached(BoltEntity entity)
	{
		DetachPlayer(entity);
		Debug.LogWarning(string.Format("entity {0} detached", entity.networkId.PackedValue));
	}

	public override void EntityAttached(BoltEntity entity)
	{
		AttachPlayer(entity);
		Debug.LogWarning(string.Format("entity {0} attached", entity.networkId.PackedValue));
	}

	private void OnGUI() {
		GUILayout.BeginVertical();
		foreach(string id in _players.Keys)
		{
			// int count = 0;
			// if(_stateMsgCount.ContainsKey(id))
			// 	count = _stateMsgCount[id];
			GUILayout.Label(string.Format("{0} received count {1}",id, _players[id].RewindTickCount));
		}
		GUILayout.EndVertical();
	}

	private void Update() 
	{
		if(!string.IsNullOrEmpty(_thisClientId))
			_players[_thisClientId].LocalSimulateTick(true);
		// foreach (BallFighter bf in _players.Values)
		// {
		// 	if(bf.)
		// }
	}

	private void FixedUpdate() {
		Physics.Simulate(Time.fixedDeltaTime);
		Physics.SyncTransforms();
	}
}

public class StateSnapshot
{
	// public BallFighter Body;
	// public StateMsg TheState;
	public int TickNumber;
	public string EntityId;
	public Vector3 Position;
	public Quaternion Rotation;
	public Vector3 Velocity;
	public Vector3 AngularVelocity;
	public Vector3 MoonPosition;
	public Quaternion MoonRotation;
	public Vector3 MoonVelocity;
	public Vector3 MoonAngularVelocity;
	public Vector2 StateInput;

	public StateSnapshot(StateMsg state)
	{
		TickNumber = state.TickNumber;
		EntityId = state.EntityId;
		StateInput = state.StateInput;
		Position = state.RigPosition;
		Rotation = state.RigRotation;
		Velocity = state.RigVelocity;
		AngularVelocity = state.RigAngularVelocity;
		MoonPosition = state.MoonPosition;
		MoonRotation = state.MoonRotation;
		MoonVelocity = state.MoonVelocity;
		MoonAngularVelocity = state.MoonAngularVelocity;
	}
}