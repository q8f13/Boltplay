using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour(BoltNetworkModes.Client, "rolling")]
public class ClientCallbacks : Bolt.GlobalEventListener {
    private Dictionary<string, BallFighter> _players = new Dictionary<string, BallFighter>();
    // private Dictionary<string, Queue<StateMsg>> _stateMsgReceived = new Dictionary<string, Queue<StateMsg>>();

	private Dictionary<string, StateSnapshot> _stateMsgRelay = new Dictionary<string, StateSnapshot>();
    // private Queue<StateMsg> _stateMsgReceived = new Queue<StateMsg>();

	// private Dictionary<string, StateMsg[]> _stateReceived = new Dictionary<string, StateMsg[]>();

	private string _thisClientId;

	public override void OnEvent(StateMsg evnt)
	{
		// if(!_stateMsgReceived.ContainsKey(evnt.EntityId))
		// 	_stateMsgReceived.Add(evnt.EntityId, new Queue<StateMsg>());

        // _stateMsgReceived[evnt.EntityId].Enqueue(evnt);

		// Debug.Assert(string.IsNullOrEmpty(evnt.EntityId) == false, "entity id should not be empty");

		if(_players.ContainsKey(evnt.EntityId))
			_players[evnt.EntityId].ReceiveState(evnt);

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

	public override void EntityDetached(BoltEntity entity)
	{
		_players.Remove(entity.networkId.PackedValue.ToString());
		Debug.LogWarning(string.Format("entity {0} detached", entity.networkId.PackedValue));
	}

	public override void EntityAttached(BoltEntity entity)
	{
		AttachPlayer(entity);
		Debug.LogWarning(string.Format("entity {0} attached", entity.networkId.PackedValue));
	}

	private void OnGUI() {
		GUILayout.BeginHorizontal();
		foreach(string id in _players.Keys)
			GUILayout.Label(id);
		GUILayout.EndHorizontal();
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
	public BallFighter Body;
	public StateMsg TheState;
	public int TickNumber;

	public StateSnapshot(BallFighter body, StateMsg state, int tick_number_start)
	{
		Body = body;
		TheState = state;
		TickNumber = tick_number_start;
	}
}