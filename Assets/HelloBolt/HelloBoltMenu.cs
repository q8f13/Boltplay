using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UdpKit;
using System;

public class HelloBoltMenu : Bolt.GlobalEventListener
{
	// const string SERVER_ADDRESS = "127.0.0.1";
	// const int SERVER_PORT = 5557;

	private UdpEndPoint _endpoint;
	private UdpEndPoint _endpoint_client;
	private BoltConfig _config;

	private void Start() {
		_endpoint = new UdpEndPoint(UdpIPv4Address.Localhost, (ushort)BoltRuntimeSettings.instance.debugStartPort);
		_endpoint_client = new UdpEndPoint(UdpIPv4Address.Localhost, 0);
	}

	void OnGUI()
	{
		GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

		if (GUILayout.Button("Start Server", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
		{
			// START SERVER
			// BoltLauncher.StartServer(_endpoint);
			BoltLauncher.StartServer();
		}

		if (GUILayout.Button("Start Client", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
		{
			// START CLIENT
			// BoltLauncher.StartClient(UdpEndPoint.Parse("127.0.0.1"));
			BoltLauncher.StartClient();
			// BoltLauncher.StartClient(_endpoint_client);
		}

		GUILayout.EndArea();
	}

	public override void BoltStartDone()
	{
		if (BoltNetwork.IsServer)
		{
			string matchName = Guid.NewGuid().ToString();

			BoltNetwork.SetServerInfo(matchName, null);
			BoltNetwork.LoadScene("rolling");
			// BoltNetwork.LoadScene("helloBolt");
		}
/* 		else if(BoltNetwork.isClient)
		{
			BoltNetwork.Connect((ushort)BoltRuntimeSettings.instance.debugStartPort);
		} */
	}

	public override void SessionListUpdated(Map<Guid, UdpSession> sessionList)
	{
		Debug.LogFormat("Session list updated: {0} total sessions", sessionList.Count);

		foreach (var session in sessionList)
		{
			UdpSession photonSession = session.Value as UdpSession;

			if (photonSession.Source == UdpSessionSource.Photon)
			{
				BoltNetwork.Connect(photonSession);
			}
		}
	}
}
