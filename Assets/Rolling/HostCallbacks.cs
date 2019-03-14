using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[BoltGlobalBehaviour(BoltNetworkModes.Server, "rolling")]
public class HostCallbacks : Bolt.GlobalEventListener {
    // private Queue<InputSender> _playerInputs = new Queue<InputSender>();
    private Dictionary<string, Rigidbody> _players = new Dictionary<string, Rigidbody>();

	private Dictionary<string, Queue<InputSender>> _playerInputs = new Dictionary<string, Queue<InputSender>>();

	private Queue<InputSender> _playerInputRelay = new Queue<InputSender>();
	// private Dictionary<string, InputSender> _playerInputRelay = new Dictionary<string, InputSender>();	

	private PlayPlayerObject _serverPlayer;

	private int _serverTick;

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
		// _playerInputRelay.Remove(entity_id);

		player.SelfDestroy();
	}

	#region Events
	public override void OnEvent(InputSender evnt)
	{
		// TODO: 这里需要将收到的inputSender重新拆出来
		// 组织到对应entityId的Vector2[]里
		// 这样才好在后面的循环更新中保证物理simulate的步调一致
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

		int server_tick = _serverTick;

		// collect player inputs and simulate them with same step
        while(!PlayerInputEmpty())
        {
            // receive one inputMsg and simulate on server side
			foreach(string id in _playerInputs.Keys)
			{
				if(_playerInputs[id].Count == 0)
				{
					// _playerInputRelay[id] = null;
					continue;
				}

				InputSender evt = _playerInputs[id].Dequeue();

				InputArrayToken input_token = (InputArrayToken)evt.InputArray;

				int max_tick = evt.TickNumber + input_token.Count - 1;

				if(max_tick >= server_tick)
				{
					int start_i = server_tick > evt.TickNumber ? (server_tick - evt.TickNumber) : 0;
				}

				Rigidbody playerRig = _players[evt.EntityId];
				Vector2[] inputs = input_token.GetInputs;
				for(int i=0;i<inputs.Length;i++)
				{
					AddForceToRigid(playerRig, inputs[i]);
				}

				_playerInputRelay.Enqueue(evt);
				// AddForceToRigid(playerRig, evt.InputParam);

/* 				if(_playerInputRelay.ContainsKey(id))
					_playerInputRelay[id] = evt;
				else
					_playerInputRelay.Add(id, evt); */
				
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

/* 			foreach(string id in _playerInputRelay.Keys)
			{
				InputSender evt = _playerInputRelay[id];
				if(evt == null)
					continue;
				Rigidbody playerRig = _players[id];

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
			} */
        }

/*         foreach(Rigidbody rig in _players.Values)
        {
            BallFighter bf = rig.GetComponent<BallFighter>();
            bf.Tick();
        } */
	}

    void AddForceToRigid(Rigidbody rig, Vector2 input)
    {
        rig.AddForce(new Vector3(input.x, 0, input.y) * 30.0f, ForceMode.Force);
    }
	#endregion
}

public class InputSnapshot
{
	public Vector2[] _inputs;
}