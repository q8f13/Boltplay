using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[BoltGlobalBehaviour(BoltNetworkModes.Server, "rolling")]
public class HostCallbacks : Bolt.GlobalEventListener {
    private Queue<InputSender> _playerInputs = new Queue<InputSender>();
    private Dictionary<string, Rigidbody> _players = new Dictionary<string, Rigidbody>();

	private void Start() 
	{
		PlayerRegistry.CreatePlayerOnHost();
	}

	public override void SceneLoadLocalDone(string scene)
	{
		PlayPlayerObject server_player = PlayerRegistry.ServerPlayer.Spawn();
		string id = server_player.Char.networkId.PackedValue.ToString();
		_players.Add(id, server_player.GetBody.Rig);
	}

	public override void SceneLoadRemoteDone(BoltConnection connection)
	{
		PlayerRegistry.GetPlayer(connection).Spawn();
	}

	public override void Connected(BoltConnection connection)
	{
		PlayerRegistry.CreatePlayerOnClient(connection);
	}

	#region Events
	public override void OnEvent(InputSender evnt)
	{
        string id = evnt.EntityId;
        _playerInputs.Enqueue(evnt);
	}	

	private void Update() {
        while(_playerInputs.Count > 0
            && !InputSenderEmpty())
        {
            // receive one inputMsg and simulate on server side
            InputSender evt = _playerInputs.Dequeue();

            Rigidbody playerRig = _players[evt.EntityId];
            AddForceToRigid(playerRig, evt.InputParam);

            Physics.Simulate(Time.fixedDeltaTime);

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

/*         foreach(Rigidbody rig in _players.Values)
        {
            BallFighter bf = rig.GetComponent<BallFighter>();
            bf.Tick();
        } */
	}

    bool InputSenderEmpty()
    {
        return _playerInputs.Count == 0;
    }

    void AddForceToRigid(Rigidbody rig, Vector2 input)
    {
        rig.AddForce(new Vector3(input.x, 0, input.y) * 30.0f, ForceMode.Force);
    }

	public override void OnEvent(StateMsg evnt)
	{
        // _players[evnt.EntityId].GetComponent<BallFighter>().ReceiveNewStateMsg(evnt);
	}
	#endregion
}