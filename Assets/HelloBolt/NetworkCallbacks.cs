using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour]
public class NetworkCallbacks : Bolt.GlobalEventListener
{
    const int MSG_QUEUE_COUNT = 10;
    List<string> logMsgs = new List<string>();

    public override void SceneLoadLocalDone(string scene)
    {
        Vector3 spawn_pos = new Vector3(Random.Range(-4,4), 2, Random.Range(-4,4));

        Debug.LogFormat("scene {0} loadLocalDone", scene);

        BoltEntity body = BoltNetwork.Instantiate(BoltPrefabs.ballFighter, spawn_pos, Quaternion.identity);
        BoltEntity moon = BoltNetwork.Instantiate(BoltPrefabs.moon, spawn_pos + Vector3.right * 0.5f, Quaternion.identity);
        body.GetComponent<BallFighter>().SetupRolling(moon);
        // BoltNetwork.Instantiate(BoltPrefabs.robot, spawn_pos, Quaternion.identity);
    }

    public override void OnEvent(LogEvent evnt)
    {
        logMsgs.Insert(0,evnt.Message);
    }

    private void OnGUI() {
        int maxMessages = Mathf.Min(5, logMsgs.Count);

        GUILayout.BeginArea(new Rect(Screen.width / 2 - 200, Screen.height - 100, 400, 100), GUI.skin.box);

        for(int i=0;i<maxMessages;i++)
            GUILayout.Label(logMsgs[i]);

        GUILayout.EndArea();
    }
}
