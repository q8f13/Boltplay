using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public static class PlayerRegistry {
	static List<PlayPlayerObject> players = new List<PlayPlayerObject>();

	static PlayPlayerObject CreatePlayer(BoltConnection connection)
	{
		PlayPlayerObject player = new PlayPlayerObject();
		player.Conn = connection;

		if(player.Conn != null)
		{
			player.Conn.UserData = player;
		}

		players.Add(player);

		return player;
	}

	public static PlayPlayerObject ServerPlayer
	{
		get{return players.First(player => player.IsServer);}
	}

	public static IEnumerable<PlayPlayerObject> AllPlayers
	{
		get{return players;}
	}

	public static PlayPlayerObject CreatePlayerOnHost()
	{
		return CreatePlayer(null);
	}

	public static PlayPlayerObject CreatePlayerOnClient(BoltConnection conn)
	{
		return CreatePlayer(conn);
	}

	public static PlayPlayerObject GetPlayer(BoltConnection connection)
	{
		if(connection == null)
			return ServerPlayer;

		return (PlayPlayerObject)(connection.UserData);
	}
}

public class PlayPlayerObject
{
	public BoltEntity Char;
	public BoltConnection Conn;

	private BallFighter _body;
	public BallFighter GetBody{get{return _body;}}
	// public BoltEntity GetMoon{get{return _body.MoonEntity;}}

	public bool IsServer
	{
		get{return Conn == null;}
	}

	public bool IsClient
	{
		get{return Conn != null;}
	}

	public void SelfDestroy()
	{
		// BoltNetwork.Detach(_body.MoonRig.gameObject);
		// BoltNetwork.Detach(Char.gameObject);
		BoltNetwork.Destroy(_body.MoonRig.gameObject);
		BoltNetwork.Destroy(Char.gameObject);
	}

	public PlayPlayerObject Spawn()
	{
		Vector3 spawn_pos = new Vector3(Random.Range(-4,4), 4, Random.Range(-4,4));
		if(!Char)
		{
			Char = BoltNetwork.Instantiate(BoltPrefabs.ballFighter, Vector3.zero, Quaternion.identity);
			// BoltEntity moon = BoltNetwork.Instantiate(BoltPrefabs.moon, Vector3.zero, Quaternion.identity);
			_body = Char.GetComponent<BallFighter>();
			// _body.SetupRolling(moon);

			if(IsServer)
				Char.TakeControl();
			else
				Char.AssignControl(Conn);
		}

		Char.transform.position = spawn_pos;

		PlayerCreated create_evt = PlayerCreated.Create(Bolt.GlobalTargets.AllClients);
		create_evt.Position = spawn_pos;
		create_evt.Rotation = Quaternion.identity;
		create_evt.MoonPosition = spawn_pos + Vector3.right * 0.5f;
		create_evt.MoonRotation = Quaternion.identity;
		create_evt.EntityId = Char.networkId.PackedValue.ToString();
		create_evt.Send();
		// _body.MoonRig.transform.position = spawn_pos + Vector3.right * 0.5f;

		return this;
	}
}