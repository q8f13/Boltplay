using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), BoltGlobalBehaviour]
public class BallFighter : Bolt.EntityEventListener<IBallState>
{
    // public const float ERROR_THRESHOLD = 0.0f;
    public const float ERROR_THRESHOLD = 0.00001f;

    public GameObject MoonPrefab;
    public GameObject MoonConcretePrefab;
    public GameObject BallConcratePrefab;

    private GameObject _concreteInstance;
    private GameObject _moonConcreteInstance;
    public GameObject ConcreteInstance{get{return _concreteInstance;}}
    public GameObject MoonConcreteInstance{get{return _moonConcreteInstance;}}

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
    // private BoltEntity _moonEntity;
    public Rigidbody MoonRig{
        get
        {
            if(_moonRig == null)
                _moonRig = _moon.GetComponent<Rigidbody>();
            return _moonRig;
        }
    }
    private Rigidbody _moonRig;
    // public BoltEntity MoonEntity{get{return _moonEntity;}}

    private Vector2 _currentInput;
    public Vector2 CurrentInput{get{return _currentInput;}}


    #region ClientPrediction
    private float _timer;
    private int _tickNumber = 0;
    private bool _attached = false;

    private Vector2[] _clientInputBuffer = new Vector2[1024];
    private ClientState[] _clientStateBuffer = new ClientState[1024];

    private int _rewindTickCount = 0;
    public int RewindTickCount{get{return _rewindTickCount;}}

    private Stack<StateSnapshot> _stateMsgReceived = new Stack<StateSnapshot>();
    public int StateMsgReceivedCount{get{return _stateMsgReceived.Count;}}

    private Vector3 _clientPosError;
    private Quaternion _clientRotError;
    private Vector3 _clientMoonPosError;
    private Quaternion _clientMoonRotError;

    #endregion

    #region RedundantInputs
    private int _clientLastReceivedStateTick;
    #endregion

    private bool _beSelf = false;

    public override void ControlGained()
    {
        _beSelf = true;
    }

    public override void ControlLost()
    {
        _beSelf = false;
    }

    public override void Attached()
    {
        _rig = GetComponent<Rigidbody>();

        // initialize moon
        GameObject moon_go = Instantiate(MoonPrefab, transform.position + Vector3.right * 0.5f, Quaternion.identity);
        _moon = moon_go.GetComponent<SpringJoint>();
        _moon.connectedBody = Rig;
        _moon.autoConfigureConnectedAnchor = false;
        _moon.anchor = Vector3.up * 0.5f;
        _moon.connectedAnchor = Vector3.up * 0.25f;
        _moon.spring = 30;
        _moon.damper = 0.2f;
        _moon.enableCollision = true;
        _moon.enablePreprocessing = true;

        // moon concrete
        _moonConcreteInstance = Instantiate(MoonConcretePrefab, moon_go.transform.position, moon_go.transform.rotation);

        // intialize concrete
        _concreteInstance = Instantiate(BallConcratePrefab, transform.position, transform.rotation);
        _mr = _concreteInstance.GetComponent<MeshRenderer>();

        // set color
        // state.SetTransforms(state.BallTransform, transform);
        // state.SetTransforms(state.MoonTransform, _moon.transform);

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

         state.addcallback("balltransform", ()=>
        {
            transform.position = state.balltransform.position;
            transform.rotation = state.balltransform.rotation;
        }); */ 

        // state.AddCallback("BallTransform", ()=>
        // {
        //     transform.position = state.BallTransform.Position;
        //     transform.rotation = state.BallTransform.Rotation;
        // });

        // state.AddCallback("MoonTransform", ()=>
        // {
        //     _moon.transform.position = state.MoonTransform.Position;
        //     _moon.transform.rotation = state.MoonTransform.Rotation;
        //     _moon.transform.localScale = Vector3.one * 0.5f;
        // });

        _attached = true;
    }

    public void UpdateWhenCreated(PlayerCreated evt)
    {
        transform.position = evt.Position;
        transform.rotation = evt.Rotation;
        _concreteInstance.transform.position = evt.Position;
        _concreteInstance.transform.rotation = evt.Rotation;
        // MoonRig.transform.position = evt.MoonPosition;
        // MoonRig.transform.rotation = evt.MoonRotation;
        MoonRig.transform.position = evt.MoonPosition;
        MoonRig.transform.rotation = evt.MoonRotation;
        _moonConcreteInstance.transform.position = evt.MoonPosition;
        _moonConcreteInstance.transform.rotation = evt.MoonRotation;

        // _clientInputBuffer[0] = Vector3.zero;
        // _clientStateBuffer[0].Position = evt.Position;
        // _clientStateBuffer[0].Rotation = evt.Rotation;
        // _clientStateBuffer[0].MoonPosition = evt.MoonPosition;
        // _clientStateBuffer[0].MoonRotation = evt.MoonRotation;
    }

    public override void Detached()
    {
        _attached = false;
    }

    public void LocalSimulateTick(bool withSimulate)
    {
        if(!_attached)
            return;

        _timer += Time.deltaTime;
        while(_timer >= Time.fixedDeltaTime)
        {
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            _timer -= Time.fixedDeltaTime;

            // TODO: 这里需要加上检查上次state的ticknumber
            // 将当前tick与上次ticknumber相差的input buffer都发出去
            // 即redundant input message
            InputSender evt = InputSender.Create(Bolt.GlobalTargets.OnlyServer);
            evt.InputParam = input;
            evt.TickNumber = _tickNumber;
            evt.EntityId = entity.networkId.PackedValue.ToString();
            evt.Send();

            int slot = _tickNumber % 1024;
            _clientInputBuffer[slot] = input;
            _clientStateBuffer[slot].Position = Rig.position;
            _clientStateBuffer[slot].Rotation = Rig.rotation;
            _clientStateBuffer[slot].MoonPosition = MoonRig.position;
            _clientStateBuffer[slot].MoonRotation = MoonRig.rotation;

            if(withSimulate)
            {
                AddForceToRigid(input);
                Physics.Simulate(Time.fixedDeltaTime);
                // Physics.SyncTransforms();
            }

            _currentInput = input;

            // if(withSimulate)
            ++_tickNumber;
        }
    }

    public string GetEntityId()
    {
        return entity.networkId.PackedValue.ToString();
    }

    private void FixedUpdate() {
        if(!_attached)
            return;
        if(BoltNetwork.IsServer)
        {
            _concreteInstance.transform.position = transform.position;
            _concreteInstance.transform.rotation = transform.rotation;
            _moonConcreteInstance.transform.position = _moon.transform.position;
            _moonConcreteInstance.transform.rotation = _moon.transform.rotation;
        }
        else if(BoltNetwork.IsClient)
        {
            UpdateAndCheckRewindTickCatched(true);
        }
    }

    void UpdateAndCheckRewindTickCatched(bool withSimulate)
    {
        if(_stateMsgReceived.Count > 0)
        {
            StateSnapshot state = _stateMsgReceived.Pop();
            RewindTick(state); 

            _clientLastReceivedStateTick = state.TickNumber;

            _stateMsgReceived.Clear();
        }

        // if correction smoothing
        _clientPosError *= 0.9f;
        _clientRotError = Quaternion.Slerp(this._clientRotError, Quaternion.identity, 0.1f);
        _clientMoonPosError *= 0.9f;
        _clientMoonRotError = Quaternion.Slerp(this._clientMoonRotError, Quaternion.identity, 0.1f);
        // else just snap
        // _clientPosError = Vector3.zero;
        // _clientRotError = Quaternion.identity;

        if(_beSelf)
        {
            _concreteInstance.transform.position = Rig.position + _clientPosError;
            _concreteInstance.transform.rotation = Rig.rotation * _clientRotError;
            _moonConcreteInstance.transform.position = MoonRig.position + _clientMoonPosError;
            _moonConcreteInstance.transform.rotation = MoonRig.rotation * _clientMoonRotError;
        }
        else
        {
            _concreteInstance.transform.position = Rig.position;
            _concreteInstance.transform.rotation = Rig.rotation;
            _moonConcreteInstance.transform.position = MoonRig.position;
            _moonConcreteInstance.transform.rotation = MoonRig.rotation;
        }

        // Rig.position = 
        // Rig.position = _rigProxyPosition + _clientPosError;
        // Rig.rotation = _rigProxyRotation * _clientRotError;
    }

    // client在local player没有输入信息要同步的时候（应该也就是不需要rewind的时候）
    // 不会抖
    // 一旦client的local player有state要同步，client remote player就会抖

    void RewindTick(StateSnapshot state)
    {
        // StateMsg state = stateMsgQ.Dequeue();

        int slot = state.TickNumber % 1024;
        Vector3 position_err = state.Position - this._clientStateBuffer[slot].Position;
        Vector3 moon_position_err = state.MoonPosition - this._clientStateBuffer[slot].MoonPosition;
        // Vector2 input = state.StateInput;

        if(position_err.sqrMagnitude > ERROR_THRESHOLD || moon_position_err.sqrMagnitude > ERROR_THRESHOLD )
        {
            // capture the current predicted pos for smoothing
            Vector3 prev_pos = Rig.position + this._clientPosError;
            Quaternion prev_rot = Rig.rotation * this._clientRotError;

            Vector3 prev_moon_pos = MoonRig.position + this._clientMoonPosError;
            Quaternion prev_moon_rot = MoonRig.rotation * this._clientMoonRotError;

            // rewind a replay
            Rig.position = state.Position;
            Rig.rotation = state.Rotation;
            Rig.velocity = state.Velocity;
            Rig.angularVelocity = state.AngularVelocity;

            MoonRig.position = state.MoonPosition;
            MoonRig.rotation = state.MoonRotation;
            MoonRig.velocity = state.MoonVelocity;
            MoonRig.angularVelocity = state.MoonAngularVelocity;

            Color clr = new Color(slot / 1024.0f, 0, 0);
            Debug.DrawRay(state.Position, Vector3.up * 0.5f, clr, 0.5f);

            // transform.position = state.Position;
            // transform.rotation = state.Rotation;

            // MoonRig.transform.position = state.MoonPosition;
            // MoonRig.transform.rotation = state.MoonRotation;

            // _moon.
            _rewindTickCount++;
            // _currentInput = state.StateInput;

            int rewind_tick_number = state.TickNumber;
            while(rewind_tick_number < _tickNumber)
            {
                // float ratio = (rewind_tick_number - state.TickNumber) / (_tickNumber - state.TickNumber);
                // ratio = Mathf.Clamp01(ratio);
                int rw_slot = rewind_tick_number % 1024;
                _clientInputBuffer[rw_slot] = state.StateInput;
                // _clientInputBuffer[rw_slot] = _currentInput;
                _clientStateBuffer[rw_slot].Position = Rig.position;
                _clientStateBuffer[rw_slot].Rotation = Rig.rotation;
                _clientStateBuffer[rw_slot].MoonPosition = MoonRig.position;
                _clientStateBuffer[rw_slot].MoonRotation = MoonRig.rotation;

                Debug.LogFormat("save input buffer: id {0}, tick {1}, position {2}", this.GetEntityId(), rewind_tick_number, Rig.position);

                // AddForceToRigid(_currentInput);
                AddForceToRigid(state.StateInput);
                Physics.Simulate(Time.fixedDeltaTime);
                // Physics.SyncTransforms();

                ++rewind_tick_number;
            }

            // correction smoothing
            // 让物理模拟与显示分离
            // 物理负责同步，显示部分利用每次的误差来做插值

            // if more than 2ms apart, just snap
            if((prev_pos - Rig.position).sqrMagnitude >= 4.0f)
            {
                _clientPosError = Vector3.zero;
                _clientRotError = Quaternion.identity;
            }
            else
            {
                _clientPosError = prev_pos - Rig.position;
                _clientRotError = Quaternion.Inverse(Rig.rotation) * prev_rot;
            }

            if((prev_moon_pos - MoonRig.position).sqrMagnitude >= 4.0f)
            {
                _clientMoonPosError = Vector3.zero;
                _clientMoonRotError = Quaternion.identity;
            }
            else
            {
                _clientMoonPosError = prev_moon_pos - MoonRig.position;
                _clientMoonRotError = Quaternion.Inverse(MoonRig.rotation) * prev_moon_rot;
            }
        }
    }

    public void ReceiveState(StateSnapshot evnt)
    {
        if(evnt.EntityId == null)
        {
            Debug.LogError("enqueue: invalid entity id");
            return;
        }
        _stateMsgReceived.Push(evnt);
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