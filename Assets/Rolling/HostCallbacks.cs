using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[BoltGlobalBehaviour(BoltNetworkModes.Server, "rolling")]
public class HostCallbacks : Bolt.GlobalEventListener {
    // private Queue<InputSender> _playerInputs = new Queue<InputSender>();
    private Dictionary<string, Rigidbody> _players = new Dictionary<string, Rigidbody>();

	// private Dictionary<string, Queue<InputSnapshot>> _playerInputs = new Dictionary<string, Queue<InputSnapshot>>();
	private Dictionary<string, Stack<InputSender>> _playerInputs = new Dictionary<string, Stack<InputSender>>();
	private Dictionary<string, PlayerStatus> _inputReceived = new Dictionary<string, PlayerStatus>();
	// private Dictionary<string, int> _inputReceived = new Dictionary<string, int>();
	// private Dictionary<string, Stack<InputSender>> _playerInputs = new Dictionary<string, Stack<InputSender>>();
	// private Dictionary<string, Queue<InputSender>> _playerInputs = new Dictionary<string, Queue<InputSender>>();

	// private Queue<InputSnapshot> _playerInputRelay = new Queue<InputSnapshot>();
	private Queue<InputSnapshot> _playerInputRelay = new Queue<InputSnapshot>();
	// private Queue<InputSender> _playerInputRelay = new Queue<InputSender>();
	// private Dictionary<string, InputSender> _playerInputRelay = new Dictionary<string, InputSender>();	

	private PlayPlayerObject _serverPlayer;

	private int _serverTick;

	private int _serverSnapshotRateAccumulate = 0;
	private const int SERVER_SNAPSHOT_RATE = 1;

	private void Start() 
	{
		PlayerRegistry.CreatePlayerOnHost();
	}

	public override void BoltStartBegin()
	{
		// BoltNetwork.RegisterTokenClass<InputArrayToken>();
	}

	public override void SceneLoadLocalDone(string scene)
	{
		PlayPlayerObject server_player = PlayerRegistry.ServerPlayer.Spawn(_serverTick);
		string id = server_player.Char.networkId.PackedValue.ToString();
		_players.Add(id, server_player.GetBody.Rig);
		_serverPlayer = server_player;
	}

	public override void SceneLoadRemoteDone(BoltConnection connection)
	{
		PlayPlayerObject client_player = PlayerRegistry.GetPlayer(connection).Spawn(_serverTick);
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

	private void OnGUI() {
		GUILayout.BeginVertical();
		foreach(KeyValuePair<string, PlayerStatus> kv in _inputReceived)
		{
			// int count = 0;
			// if(_stateMsgCount.ContainsKey(id))
			// 	count = _stateMsgCount[id];
			GUILayout.Label(string.Format("{0}, current tick: {1}, received input pack {2}, input cmd count {3}"
				,kv.Key
				,kv.Value.CurrentTick
				,kv.Value.InputEventPackReceived
				,kv.Value.InputCmdCount));
		}
		GUILayout.EndVertical();
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
			_playerInputs.Add(id, new Stack<InputSender>());
        _playerInputs[id].Push(evnt);

		if(!_inputReceived.ContainsKey(id))
		{
			_inputReceived.Add(id, new PlayerStatus());
			_inputReceived[id].Id = id;
		}

		_inputReceived[id].InputEventPackReceived++;
		_inputReceived[id].CurrentTick = evnt.TickNumber;

		// if(!_playerInputs.ContainsKey(id))
		// 	_playerInputs.Add(id, new Queue<InputSender>());

		// Vector2[] inputs = ((InputArrayToken)(evnt.InputArray)).GetInputs;
		// for(int i=0;i<inputs.Length;i++)
		// {
		// 	InputSnapshot ss = new InputSnapshot();
		// 	ss.EntityId = id;
		// 	ss.TickNumber = evnt.TickNumber;
		// 	ss.CountThisWave = inputs.Length;
		// 	ss.Input = inputs[i];
		// 	_playerInputs[id].Enqueue(ss);
		// }
	}	

	Vector2[] ExtractInput(string input_raw)
	{
		string[] splited = input_raw.Split('|');
		Vector2[] result = new Vector2[splited.Length];
		string[] foo;
		for(int i=0;i<splited.Length;i++)
		{
			foo = splited[i].Substring(1,splited[i].Length-2).Split(',');
			result[i].x = float.Parse(foo[0]);
			result[i].y = float.Parse(foo[1]);
		}

		return result;
	}

	private void Update() 
	{
		// send input from server player
		if(_serverPlayer != null)
			_serverPlayer.GetBody.LocalSimulateTick(false);

		int server_tick = _serverTick;
		// int server_rate_accumulator = _serverSnapshotRateAccumulate;

		// collect player inputs and simulate them with same step
        
		// receive one inputMsg and simulate on server side
		foreach(string id in _playerInputs.Keys)
		{

			if(_playerInputs[id].Count == 0)
			{
				// _playerInputRelay[id] = null;
				continue;
			}

			InputSender evt = _playerInputs[id].Pop();

			_playerInputs[id].Clear();

			// InputSnapshot snapshot = _playerInputs[id].Dequeue();
			string input_raw = evt.InputStrRaw;

			// InputArrayToken input_token = (InputArrayToken)evt.InputArray;
			Vector2[] inputs;
			if(string.IsNullOrEmpty(input_raw))
				inputs = new Vector2[]{Vector2.zero};
			else
				inputs = ExtractInput(input_raw);
			// Vector2[] inputs = input_token.GetInputs;

			_inputReceived[id].InputCmdCount+=inputs.Length;

			int max_tick = evt.TickNumber + inputs.Length - 1;
			// int max_tick = snapshot.TickNumber + snapshot.CountThisWave - 1;

			// if(max_tick >= server_tick )
			// {
			int start_i = server_tick > evt.TickNumber ? (server_tick  - evt.TickNumber) : 0;
			Rigidbody playerRig = _players[evt.EntityId];
			Debug.LogWarningFormat("id {2}, start_i = {0}, input len {1}", start_i, inputs.Length, id);
			for(int i=start_i;i<inputs.Length;++i)
			// for(int i=0;i<inputs.Length;++i)
			{
				AddForceToRigid(playerRig, inputs[i]);

				++server_tick;
				// ++server_rate_accumulator;
				Physics.Simulate(Time.fixedDeltaTime);
				// if(server_rate_accumulator >= SERVER_SNAPSHOT_RATE)
				// {
					// server_rate_accumulator = 0;

				InputSnapshot snapshot = new InputSnapshot();
				snapshot.EntityId = evt.EntityId;
				snapshot.TickNumber = server_tick;
				snapshot.Input = inputs[i];
				_playerInputRelay.Enqueue(snapshot);

				Debug.LogWarningFormat("entity {0} sync state enqueued", id);
				// }
			}

			// }
			// else
			// {
			// 	Debug.LogWarningFormat("entity {0} won't sync due to max_tick {1} is smaller than server tick {2}"
			// 		, id
			// 		, max_tick
			// 		, server_tick);
			// }
		}

		// Physics.SyncTransforms();

		while(_playerInputRelay.Count > 0)
		{
			// InputSender evt = _playerInputRelay.Dequeue();
			InputSnapshot evt = _playerInputRelay.Dequeue();
			// if(evt == null)
			// 	continue;
			if(evt.TickNumber < 0)
			{
				Debug.LogWarningFormat("state event ticknumber less than zero, ignored");
				continue;
			}

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
			state.TickNumber = server_tick;
			// state.TickNumber = evt.TickNumber + 1;
			state.StateInput = evt.Input;
			// state.StateInput = evt.InputParam;
			// moon
			BallFighter bf = playerRig.GetComponent<BallFighter>();
			state.MoonPosition = bf.MoonRig.position;
			state.MoonRotation = bf.MoonRig.rotation;
			state.MoonVelocity = bf.MoonRig.velocity;
			state.MoonAngularVelocity = bf.MoonRig.angularVelocity;
			state.Send();
			Debug.LogWarningFormat("send state to entity {0} with tickNumber {1}", state.EntityId, state.TickNumber);
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
        

		// _serverSnapshotRateAccumulate = server_rate_accumulator;
		_serverTick = server_tick;

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

public struct InputSnapshot
{
	public string EntityId;
	public int TickNumber;
	public int CountThisWave;
	public Vector2 Input;
}

public class PlayerStatus
{
	public string Id;
	public int CurrentTick;
	public int InputEventPackReceived;
	public int InputCmdCount;
}