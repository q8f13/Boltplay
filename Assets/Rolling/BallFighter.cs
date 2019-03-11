using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), BoltGlobalBehaviour]
public class BallFighter : Bolt.EntityEventListener<IBallState>
{
    public const float ERROR_THRESHOLD = 0.0f;
    // public const float ERROR_THRESHOLD = 0.00001f;

    public GameObject MoonPrefab;

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

    private Queue<StateMsg> _stateMsgReceived = new Queue<StateMsg>();
    public int StateMsgReceivedCount{get{return _stateMsgReceived.Count;}}
    #endregion

    private void Awake() {
    }

    public override void Attached()
    {
        _rig = GetComponent<Rigidbody>();
        _mr =GetComponent<MeshRenderer>();

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

        // set color
        state.SetTransforms(state.BallTransform, transform);
        state.SetTransforms(state.MoonTransform, _moon.transform);

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

        state.AddCallback("BallTransform", ()=>
        {
            transform.position = state.BallTransform.Position;
            transform.rotation = state.BallTransform.Rotation;
        });

        state.AddCallback("MoonTransform", ()=>
        {
            _moon.transform.position = state.MoonTransform.Position;
            _moon.transform.rotation = state.MoonTransform.Rotation;
            _moon.transform.localScale = Vector3.one * 0.5f;
        });

        _attached = true;
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

            if(withSimulate)
            {
                AddForceToRigid(input);
            }

            // if(withSimulate)
            //     Physics.Simulate(Time.fixedDeltaTime);
            ++_tickNumber;
        }
    }

    public void SelfDestroy()
    {
        Destroy(_moon.gameObject);
        Destroy(this.gameObject);
    }

    public string GetEntityId()
    {
        return entity.networkId.PackedValue.ToString();
    }

    private void FixedUpdate() 
    {
        UpdateAndCheckRewindTickCatched();
    }

    void UpdateAndCheckRewindTickCatched()
    {
        // StateMsg state = _stateMsgReceived.Dequeue();
        while(_stateMsgReceived.Count > 0)
        {
            StateMsg state = _stateMsgReceived.Dequeue();
            RewindTick(state);
        }
    }

    void RewindTick(StateMsg state)
    {
        // StateMsg state = stateMsgQ.Dequeue();

        int slot = state.TickNumber % 1024;
        Vector3 position_err = state.RigPosition - this._clientStateBuffer[slot].Position;
        Vector3 moon_position_err = state.MoonPosition - this._clientStateBuffer[slot].MoonPosition;
        // Vector2 input = state.StateInput;

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
            // _moon.
            _rewindTickCount++;

            int rewind_tick_number = state.TickNumber;
            while(rewind_tick_number < _tickNumber)
            {
                // float ratio = (rewind_tick_number - state.TickNumber) / (_tickNumber - state.TickNumber);
                // ratio = Mathf.Clamp01(ratio);
                int rw_slot = rewind_tick_number % 1024;
                _clientInputBuffer[rw_slot] = state.StateInput;
                // _clientInputBuffer[rw_slot] = input;
                _clientStateBuffer[rw_slot].Position = Rig.position;
                _clientStateBuffer[rw_slot].Rotation = Rig.rotation;
                _clientStateBuffer[rw_slot].MoonPosition = MoonRig.position;
                _clientStateBuffer[rw_slot].MoonRotation = MoonRig.rotation;

                AddForceToRigid(state.StateInput);

                ++rewind_tick_number;
            }
        }
    }

    public void ReceiveState(StateMsg evnt)
    {
        _stateMsgReceived.Enqueue(evnt);
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