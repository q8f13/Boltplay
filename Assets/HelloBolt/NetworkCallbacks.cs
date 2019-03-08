using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [BoltGlobalBehaviour]
public class NetworkCallbacks : Bolt.GlobalEventListener
{
//     const int MSG_QUEUE_COUNT = 10;
//     List<string> logMsgs = new List<string>();

//     private Queue<InputSender> _playerInputs = new Queue<InputSender>();
//     private Dictionary<string, Rigidbody> _players = new Dictionary<string, Rigidbody>();

//     public override void SceneLoadLocalDone(string scene)
//     {
//         Debug.LogFormat("scene {0} loadLocalDone", scene);

//         // CreateEntity();
//         // BoltNetwork.Instantiate(BoltPrefabs.robot, spawn_pos, Quaternion.identity);
//     }

//     void CreateEntity()
//     {
//         Vector3 spawn_pos = new Vector3(Random.Range(-4,4), 2, Random.Range(-4,4));
//         BoltEntity body = BoltNetwork.Instantiate(BoltPrefabs.ballFighter, spawn_pos, Quaternion.identity);
//         // BoltEntity moon = BoltNetwork.Instantiate(BoltPrefabs.moon, spawn_pos + Vector3.right * 0.5f, Quaternion.identity);
//         // body.GetComponent<BallFighter>().SetupRolling(moon);
//     }

//     public override void EntityAttached(BoltEntity entity)
//     {
//         // logMsgs.Insert(0, string.Format("entity {0} attached", entity.networkId.ToString()));

//         if(entity.gameObject.GetComponent<BallFighter>() == null)
//             return;

//         string id = entity.networkId.PackedValue.ToString();
//         Debug.AssertFormat(!_players.ContainsKey(id), "entity with id {0} should not appeared before this attachment");
//         _players.Add(id, entity.gameObject.GetComponent<Rigidbody>());
//     }

//     public override void EntityDetached(BoltEntity entity)
//     {
//         // logMsgs.Insert(0, string.Format("entity {0} detached", entity.networkId.ToString()));

//         string id = entity.networkId.PackedValue.ToString();
//         Debug.AssertFormat(_players.ContainsKey(id), "entity with id {0} should appeared before this detachment");
//         _players.Remove(id);
//     }

//     public override void EntityReceived(BoltEntity entity)
//     {
//         logMsgs.Insert(0, string.Format("entity {0} received", entity.networkId.ToString()));
//     }

//     public override void OnEvent(InputSender evt)
//     {
//         string id = evt.EntityId;
//         _playerInputs.Enqueue(evt);
//     }

//     public override void OnEvent(StateMsg evnt)
//     {
//         // _players[evnt.EntityId].GetComponent<BallFighter>().ReceiveNewStateMsg(evnt);
//     }

//     public override void OnEvent(LogEvent evnt)
//     {
//         logMsgs.Insert(0,evnt.Message);
//     }

//     bool InputSenderEmpty()
//     {
//         return _playerInputs.Count == 0;
//     }

//     private void Update() {
//         while(_playerInputs.Count > 0
//             && !InputSenderEmpty())
//         {
//             // receive one inputMsg and simulate on server side
//             InputSender evt = _playerInputs.Dequeue();

//             Rigidbody playerRig = _players[evt.EntityId];
//             AddForceToRigid(playerRig, evt.InputParam);

//             Physics.Simulate(Time.fixedDeltaTime);

//             // create simulate result and reply to client
//             // for client prediction error correcting
//             StateMsg state = StateMsg.Create(Bolt.GlobalTargets.AllClients);
//             state.RigPosition = playerRig.position;
//             state.RigRotation = playerRig.rotation;
//             state.RigVelocity = playerRig.velocity;
//             state.RigAngularVelocity = playerRig.angularVelocity;
//             state.EntityId = evt.EntityId;
//             state.TickNumber = evt.TickNumber + 1;
//             state.StateInput = evt.InputParam;
//             state.Send();
//         }

// /*         foreach(Rigidbody rig in _players.Values)
//         {
//             BallFighter bf = rig.GetComponent<BallFighter>();
//             bf.Tick();
//         } */
//     }

//     void AddForceToRigid(Rigidbody rig, Vector2 input)
//     {
//         rig.AddForce(new Vector3(input.x, 0, input.y) * 30.0f, ForceMode.Force);
//     }

//     private void OnGUI() {
//         int maxMessages = Mathf.Min(5, logMsgs.Count);

//         GUILayout.BeginArea(new Rect(Screen.width / 2 - 200, Screen.height - 100, 400, 100), GUI.skin.box);

//         for(int i=0;i<maxMessages;i++)
//             GUILayout.Label(logMsgs[i]);

//         GUILayout.EndArea();
//     }
}
