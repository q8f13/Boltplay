using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour("rolling")]
public class ClientCallbacks : Bolt.GlobalEventListener {
    private Dictionary<string, BallFighter> _players = new Dictionary<string, BallFighter>();
    private Queue<StateMsg> _stateMsgReceived = new Queue<StateMsg>();

	private string _thisClientId;

	public override void OnEvent(StateMsg evnt)
	{
        _stateMsgReceived.Enqueue(evnt);
	}

	public override void ControlOfEntityGained(BoltEntity entity)
	{
		_thisClientId = entity.networkId.PackedValue.ToString();
	}

	void AttachPlayer(BoltEntity entity)
	{
		BallFighter bf = entity.GetComponent<BallFighter>();
		if(bf == null)
			return;
		string id = entity.networkId.PackedValue.ToString();
		_players.Add(id, bf);
	}

	public override void EntityAttached(BoltEntity entity)
	{
		AttachPlayer(entity);
	}

	private void Update() 
	{
		if(string.IsNullOrEmpty(_thisClientId))
		{
			Debug.LogError("thisClientId not gained yet");
			return;
		}

		foreach(BallFighter bf in _players.Values)
		{
			if(bf.entity.networkId.PackedValue.ToString() == _thisClientId)
			{
				bf.LocalSimulateTick();
				break;
			}
		}

		while(_stateMsgReceived.Count > 0)
		{
			StateMsg state = _stateMsgReceived.Dequeue();
			_players[state.EntityId].RewindTick(state);
		}
	}
}