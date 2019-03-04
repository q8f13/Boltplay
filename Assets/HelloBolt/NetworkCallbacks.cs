using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkCallbacks : Bolt.GlobalEventListener
{
    public override void SceneLoadLocalDone(string scene)
    {
        Vector3 spawn_pos = new Vector3(Random.Range(-8,8), 0, Random.Range(-8,8));

        BoltNetwork.Instantiate(BoltPrefabs.Cube, spawn_pos, Quaternion.identity);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
