using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), BoltGlobalBehaviour(BoltNetworkModes.Client)]
public class BallFighter : Bolt.EntityEventListener<IBallState>
{
    public const float ERROR_THRESHOLD = 0.00001f;

    private Rigidbody _rig;
    public Rigidbody Rig{get{
        if(_rig == null)
            _rig = GetComponent<Rigidbody>();
        return _rig;
    }}
    private MeshRenderer _mr;

    [SerializeField]
    private float _forceMultiplier = 1.0f;

    [SerializeField]
    private InputSource _input;

    private SpringJoint _moon;
    private BoltEntity _moonEntity;
    private MoonBehaviour _moonBh;
    public Rigidbody MoonRig{get{return _moon.GetComponent<Rigidbody>();}}
    public BoltEntity MoonEntity{get{return _moonEntity;}}

    private Vector2 _currentForce;

    #region ClientPrediction
    private float _timer;
    private int _tickNumber = 0;
    private bool _attached = false;

    private Vector2[] _clientInputBuffer = new Vector2[1024];
    private ClientState[] _clientStateBuffer = new ClientState[1024];

    // private Queue<StateMsg> _stateMsgReceived = new Queue<StateMsg>();
    #endregion

    public override void Attached()
    {
        _rig = GetComponent<Rigidbody>();
        _mr =GetComponent<MeshRenderer>();

        // set color
        state.SetTransforms(state.BallTransform, transform);

        if(entity.isOwner)
        {
            state.BallColor = Random.ColorHSV();
        }

        state.AddCallback("BallColor", ()=>
        {
            _mr.material.color = state.BallColor;
        });

/*         state.AddCallback("BallRig", ()=>
        {
            _rig.velocity = state.BallRig.RigVelocity;
            _rig.drag = state.BallRig.Drag;
            _rig.angularDrag = state.BallRig.AngularDrag;
            _rig.angularVelocity = state.BallRig.AngulalrVelocity;
        });

        state.AddCallback("BallTransform", ()=>
        {
            transform.position = state.BallTransform.Position;
            transform.rotation = state.BallTransform.Rotation;
        }); */

        _attached = true;
    }

    public void SetupRolling(BoltEntity moon)
    {
        _moon = moon.GetComponent<SpringJoint>();
        _moon.connectedBody = Rig;
        _moonBh = _moon.GetComponent<MoonBehaviour>();
    }

    public void LocalSimulateTick()
    {
        _timer += Time.deltaTime;
        while(_timer >= Time.fixedDeltaTime)
        {
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            _timer -= Time.fixedDeltaTime;

            InputSender evt = InputSender.Create(Bolt.GlobalTargets.OnlyServer);
            evt.InputParam = input;
            evt.TickNumber = _tickNumber;
            evt.EntityId = entity.networkId.PackedValue.ToString();
            evt.Send();

            int slot = _tickNumber % 1024;
            Debug.Assert(slot >= 0);
            _clientInputBuffer[slot] = input;
            _clientStateBuffer[slot].Position = Rig.position;
            _clientStateBuffer[slot].Rotation = Rig.rotation;
            _clientStateBuffer[slot].MoonPosition = MoonRig.position;
            _clientStateBuffer[slot].MoonRotation = MoonRig.rotation;

            AddForceToRigid(input);

            Physics.Simulate(Time.fixedDeltaTime);

            ++_tickNumber;
        }
    }

    public void RewindTick(StateMsg state)
    {
        // StateMsg state = stateMsgQ.Dequeue();

        if(state.EntityId != this.entity.networkId.PackedValue.ToString())
        {
            Debug.Log("other player state received");
            return;
        }

        int slot = state.TickNumber % 1024;
        Vector3 position_err = state.RigPosition - this._clientStateBuffer[slot].Position;
        Vector3 moon_position_err = state.MoonPosition - this._clientStateBuffer[slot].MoonPosition;
        Vector2 input = state.StateInput;

        if(position_err.sqrMagnitude > ERROR_THRESHOLD || moon_position_err.sqrMagnitude > ERROR_THRESHOLD )
        {
            // rewind a replay
            Rig.position = state.RigPosition;
            Rig.rotation = state.RigRotation;
            Rig.velocity = state.RigVelocity;
            Rig.angularVelocity = state.RigAngularVelocity;

            MoonRig.position = state.MoonPosition;
            MoonRig.rotation = state.MoonRotation;
            MoonRig.velocity = state.MoonVelocity;
            MoonRig.angularVelocity = state.MoonAngularVelocity;

            int rewind_tick_number = state.TickNumber;
            while(rewind_tick_number < _tickNumber)
            {
                slot = rewind_tick_number % 1024;
                _clientInputBuffer[slot] = input;
                _clientStateBuffer[slot].Position = Rig.position;
                _clientStateBuffer[slot].Rotation = Rig.rotation;
                _clientStateBuffer[slot].MoonPosition = MoonRig.position;
                _clientStateBuffer[slot].MoonRotation = MoonRig.rotation;

                AddForceToRigid(input);

                Physics.Simulate(Time.fixedDeltaTime);

                ++rewind_tick_number;
            }
        }
    }

/*     public override void OnEvent(StateMsg evnt)
    {
        _stateMsgReceived.Enqueue(evnt);
    } */

    public override void SimulateOwner()
    {
//         switch(_input)
//         {
//             case InputSource.Keyboard:
//                 _currentForce.x = Input.GetAxis("Horizontal");
//                 _currentForce.y = Input.GetAxis("Vertical");
//                 break;
// /*             case InputSource.Mouse:
//                 _currentForce.x = Input.GetAxis("Mouse X");
//                 _currentForce.y = Input.GetAxis("Mouse Y");
//                 break; */
//         }

//         state.CurrentForce = _currentForce;

//         BallRigid br = state.BallRig;
//         if(br!=null)
//         {
//             br.RigVelocity = _rig.velocity;
//             br.Drag = _rig.drag;
//             br.AngularDrag = _rig.angularDrag;
//             br.AngulalrVelocity = _rig.angularVelocity;
//         }
    }

    // private void Update() {
    //     if(Application.isEditor)
    //     {
    //         switch(_input)
    //         {
    //             case InputSource.Keyboard:
    //                 _currentForce.x = Input.GetAxis("Horizontal");
    //                 _currentForce.y = Input.GetAxis("Vertical");
    //                 break;
    // /*             case InputSource.Mouse:
    //                 _currentForce.x = Input.GetAxis("Mouse X");
    //                 _currentForce.y = Input.GetAxis("Mouse Y");
    //                 break; */
    //         }
    //     }

    //     state.SetTransforms(state.BallTransform, transform);
    // }

    void AddForceToRigid(Vector2 input)
    {
        Rig.AddForce(new Vector3(input.x, 0, input.y) * _forceMultiplier, ForceMode.Force);
    }

    // private void FixedUpdate() {
    //     if(_rig == null)
    //         _rig = GetComponent<Rigidbody>();

    //     _rig.AddForce(new Vector3(_currentForce.x, 0, _currentForce.y) * _forceMultiplier, ForceMode.Force);
    // }
}   

/* public interface IClientSync
{
    void Register(Bolt.IEntityBehaviour entity);
    void Unregister(Bolt.IEntityBehaviour entity);
} */

public enum InputSource
{
    None = 0,
    Keyboard,
    Mouse,
}

public struct ClientState
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 MoonPosition;
    public Quaternion MoonRotation;
}