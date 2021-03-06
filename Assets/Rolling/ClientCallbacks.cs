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

	// private Dictionary<string, Queue<StateSnapshot>> _stateReceived = new Dictionary<string, Queue<StateSnapshot>>();

	// public override void BoltStartBegin()
	// {
	// 	BoltNetwork.RegisterTokenClass<InputArrayToken>();
	// }

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

		if(!_stateMsgCount.ContainsKey(snapshot.EntityId))
			_stateMsgCount.Add(snapshot.EntityId, 0);
		_stateMsgCount[snapshot.EntityId]++;

		_players[snapshot.EntityId].ReceiveState(snapshot);
		// if(!_stateReceived.ContainsKey(evnt.EntityId))
		// 	_stateReceived.Add(evnt.EntityId, new Queue<StateSnapshot>());
		// _stateReceived[evnt.EntityId].Enqueue(snapshot);

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
			if(_stateMsgCount.ContainsKey(id))
				GUILayout.Label(string.Format("{0} received count {1}",id, _stateMsgCount[id]));
		}
		GUILayout.EndVertical();
	}

	private void Update() 
	{
		if(string.IsNullOrEmpty(_thisClientId))
			return;

		// _players[_thisClientId].ToggleRigidbody(true);

		_players[_thisClientId].LocalSimulateTick(true);

/* 		foreach(BallFighter bf in _players.Values)
		{
			bool with_control = bf.GetEntityId() == _thisClientId;
			// bf.gameObject.SetActive(with_control);
			bf.ToggleRigidbody(with_control);
		} */

/* 		while(!MsgQueueEmpty())
		{
			foreach(string id in _stateReceived.Keys)
			{
				if(_stateReceived[id].Count == 0)
					continue;
				StateSnapshot state = _stateReceived[id].Dequeue();
				_players[id].RewindTick(state);
			}
		} */

/* 		foreach(BallFighter bf in _players.Values)
		{
			// bool use_simulate = bf.GetEntityId() != _thisClientId;
			bf.UpdateAndCheckRewindTickCatched(true);
		} */

		// _players[_thisClientId].UpdateAndCheckRewindTickCatched(true);
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