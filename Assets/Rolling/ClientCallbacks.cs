using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour("rolling")]
public class ClientCallbacks : Bolt.GlobalEventListener {
    private Dictionary<string, BallFighter> _players = new Dictionary<string, BallFighter>();
    private Dictionary<string, Queue<StateMsg>> _stateMsgReceived = new Dictionary<string, Queue<StateMsg>>();

	private Dictionary<string, StateSnapshot> _stateMsgRelay = new Dictionary<string, StateSnapshot>();
    // private Queue<StateMsg> _stateMsgReceived = new Queue<StateMsg>();

	// private Dictionary<string, StateMsg[]> _stateReceived = new Dictionary<string, StateMsg[]>();

	private string _thisClientId;

	public override void OnEvent(StateMsg evnt)
	{
		if(!_stateMsgReceived.ContainsKey(evnt.EntityId))
			_stateMsgReceived.Add(evnt.EntityId, new Queue<StateMsg>());

        _stateMsgReceived[evnt.EntityId].Enqueue(evnt);

		// Debug.Assert(string.IsNullOrEmpty(evnt.EntityId) == false, "entity id should not be empty");

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
			return;
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

	bool AllStateEmpty()
	{
		foreach(Queue<StateMsg> state in _stateMsgReceived.Values)
		{
			if(state.Count > 0)
				return false;
		}

		return true;
	}

	private void Update() 
	{
		if(string.IsNullOrEmpty(_thisClientId))
		{
			Debug.LogError("thisClientId not gained yet");
			return;
		}

		_players[_thisClientId].LocalSimulateTick();
/* 		foreach(BallFighter bf in _players.Values)
		{
			if(bf.entity.networkId.PackedValue.ToString() == _thisClientId)
			{
				bf.LocalSimulateTick();
				break;
			}
		} */

		// while(_stateMsgReceived.Count > 0)
		while(!AllStateEmpty())
		{
			bool need_rewind = false;
			foreach(string id in  _stateMsgReceived.Keys)
			{
				if(_stateMsgReceived[id].Peek() == null)
				{
					if(_stateMsgRelay.ContainsKey(id))
						_stateMsgRelay[id] = null;
					continue;
				}

				StateMsg state = _stateMsgReceived[id].Dequeue();
				StateSnapshot snapshot = new StateSnapshot(_players[state.EntityId], state, state.TickNumber);
				if(_stateMsgRelay.ContainsKey(id))
					_stateMsgRelay[id] = snapshot;
				else
					_stateMsgRelay.Add(id, snapshot);

				need_rewind |= _players[state.EntityId].RewindTick(state);
			}

			int failsafe = 999999;
			while(true)
			{
				if(failsafe < 0)
				{
					throw new System.Exception("dead loop");
				}

				failsafe--;

				bool need_step = false;
				foreach(StateSnapshot snap in _stateMsgRelay.Values)
				{
					if(snap == null)
						continue;
					bool need_step_on_this = snap.Body.UpdateAndCheckRewindTickCatched(snap.TheState, snap.TickNumber);
					need_step |= need_step_on_this;

					if(need_step_on_this)
						++snap.TickNumber;
				}

				if(need_step)
				{
					Physics.Simulate(Time.fixedDeltaTime);
				}
				else
				{
					break;
				}
			}
		}
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