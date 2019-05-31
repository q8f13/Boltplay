using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[BoltGlobalBehaviour(BoltNetworkModes.Server, "rolling")]
public class HostCallbacks : Bolt.GlobalEventListener {
    private Dictionary<string, Rigidbody> _players = new Dictionary<string, Rigidbody>();

	private Dictionary<string, Queue<InputSender>> _playerInputs = new Dictionary<string, Queue<InputSender>>();

	private Queue<InputSender> _playerInputRelay = new Queue<InputSender>();

	private PlayPlayerObject _serverPlayer;

	private void Start() 
	{
		PlayerRegistry.CreatePlayerOnHost();
	}

	public override void SceneLoadLocalDone(string scene)
	{
		PlayPlayerObject server_player = PlayerRegistry.ServerPlayer.Spawn();
		string id = server_player.Char.networkId.PackedValue.ToString();
		_players.Add(id, server_player.GetBody.Rig);
		_serverPlayer = server_player;
	}

	public override void SceneLoadRemoteDone(BoltConnection connection)
	{
		PlayPlayerObject client_player = PlayerRegistry.GetPlayer(connection).Spawn();
		string id = client_player.Char.networkId.PackedValue.ToString();
		_players.Add(id, client_player.GetBody.Rig);
	}

	public override void Connected(BoltConnection connection)
	{
		PlayerRegistry.CreatePlayerOnClient(connection);
	}

	public override void Disconnected(BoltConnection connection)
	{
		PlayPlayerObject player = PlayerRegistry.GetPlayer(connection);
		string entity_id = player.GetBody.GetEntityId();
		_players.Remove(entity_id);
		_playerInputs.Remove(entity_id);

		player.SelfDestroy();
	}

	#region Events
	public override void OnEvent(InputSender evnt)
	{
        string id = evnt.EntityId;
		if(string.IsNullOrEmpty(id))
		{
			Debug.LogError("entity id should not be empty");
			return;
		}
		if(!_playerInputs.ContainsKey(id))
			_playerInputs.Add(id, new Queue<InputSender>());
        _playerInputs[id].Enqueue(evnt);
	}	

	bool PlayerInputEmpty()
	{
		foreach(Queue<InputSender> input in _playerInputs.Values)
		{
			if(input.Count > 0)
				return false;
		}

		return true;
	}

	private void Update() {
		// send input from server player
		if(_serverPlayer != null)
			_serverPlayer.GetBody.LocalSimulateTick(false);

		// collect player inputs and simulate them with same step
        while(!PlayerInputEmpty())
        {
            // receive one inputMsg and simulate on server side
			foreach(string id in _playerInputs.Keys)
			{
				if(_playerInputs[id].Count == 0)
				{
					continue;
				}

				InputSender evt = _playerInputs[id].Dequeue();

				Rigidbody playerRig = _players[evt.EntityId];
				AddForceToRigid(playerRig, evt.InputParam);
				
				_playerInputRelay.Enqueue(evt);
			}

			Physics.Simulate(Time.fixedDeltaTime);
			// Physics.SyncTransforms();

			while(_playerInputRelay.Count > 0)
			{
				InputSender evt = _playerInputRelay.Dequeue();
				if(evt == null)
					continue;

				Rigidbody playerRig = _players[evt.EntityId];

				// create simulate result and reply to client
				// for client prediction error correcting
				StateMsg state = StateMsg.Create(Bolt.GlobalTargets.AllClients);
				// ball
				state.RigPosition = playerRig.position;
				state.RigRotation = playerRig.rotation;
				state.RigVelocity = playerRig.velocity;
				state.RigAngularVelocity = playerRig.angularVelocity;
				state.EntityId = evt.EntityId;
				state.TickNumber = evt.TickNumber + 1;
				state.StateInput = evt.InputParam;
				// moon
				BallFighter bf = playerRig.GetComponent<BallFighter>();
				state.MoonPosition = bf.MoonRig.position;
				state.MoonRotation = bf.MoonRig.rotation;
				state.MoonVelocity = bf.MoonRig.velocity;
				state.MoonAngularVelocity = bf.MoonRig.angularVelocity;
				state.Send();
			}
		}
	}

    void AddForceToRigid(Rigidbody rig, Vector2 input)
    {
        rig.AddForce(new Vector3(input.x, 0, input.y) * 30.0f, ForceMode.Force);
    }
	#endregion
}