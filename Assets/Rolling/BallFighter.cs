using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), BoltGlobalBehaviour]
public class BallFighter : Bolt.EntityEventListener<IBallState>
{
    // public const float ERROR_THRESHOLD = 0.0f;
    public const float ERROR_THRESHOLD = 0.00001f;

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
    public Rigidbody MoonRig{
        get
        {
            if(_moonRig == null)
                _moonRig = _moon.GetComponent<Rigidbody>();
            return _moonRig;
        }
    }
    private Rigidbody _moonRig;

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

        _attached = true;
    }

    public void UpdateWhenCreated(PlayerCreated evt)
    {
        transform.position = evt.Position;
        transform.rotation = evt.Rotation;
        MoonRig.transform.position = evt.MoonPosition;
        MoonRig.transform.rotation = evt.MoonRotation;
        MoonRig.position = evt.MoonPosition;
        MoonRig.rotation = evt.MoonRotation;
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
            }

            _currentInput = input;

            ++_tickNumber;
        }
    }

    public string GetEntityId()
    {
        return entity.networkId.PackedValue.ToString();
    }

    public void ToggleRigidbody(bool on)
    {
        _rig.isKinematic = !on;
        MoonRig.isKinematic = !on;
    }

    private void FixedUpdate() {
        UpdateAndCheckRewindTickCatched(true);
    }

    void UpdateAndCheckRewindTickCatched(bool withSimulate)
    {
        if(_stateMsgReceived.Count == 0)
            return;

        StateSnapshot state = _stateMsgReceived.Pop();
        RewindTick(state); 

        _stateMsgReceived.Clear();

        // if correction smoothing
        _clientPosError *= 0.9f;
        _clientRotError = Quaternion.Slerp(this._clientRotError, Quaternion.identity, 0.1f);
        // else just snap
        // _clientPosError = Vector3.zero;
        // _clientRotError = Quaternion.identity;

    }

    // client在local player没有输入信息要同步的时候（应该也就是不需要rewind的时候）
    // 不会抖
    // 一旦client的local player有state要同步，client remote player就会抖

    // FIXME: 现在问题在于，对client上的所有player来说
    // 每个player在rewind阶段所需要的Physics.Simulate()次数是不一样的
    // 需要想个办法让这个次数一致
    void RewindTick(StateSnapshot state)
    {
        int slot = state.TickNumber % 1024;
        Vector3 position_err = state.Position - this._clientStateBuffer[slot].Position;
        Vector3 moon_position_err = state.MoonPosition - this._clientStateBuffer[slot].MoonPosition;

        if(position_err.sqrMagnitude > ERROR_THRESHOLD || moon_position_err.sqrMagnitude > ERROR_THRESHOLD )
        {
            // capture the current predicted pos for smoothing
            Vector3 prev_pos = Rig.position + this._clientPosError;
            Quaternion prev_rot = Rig.rotation * this._clientRotError;

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

            _rewindTickCount++;

            int rewind_tick_number = state.TickNumber;
            while(rewind_tick_number < _tickNumber)
            {
                int rw_slot = rewind_tick_number % 1024;
                _clientInputBuffer[rw_slot] = state.StateInput;
                _clientStateBuffer[rw_slot].Position = Rig.position;
                _clientStateBuffer[rw_slot].Rotation = Rig.rotation;
                _clientStateBuffer[rw_slot].MoonPosition = MoonRig.position;
                _clientStateBuffer[rw_slot].MoonRotation = MoonRig.rotation;

                Debug.LogFormat("save input buffer: id {0}, tick {1}, position {2}", this.GetEntityId(), rewind_tick_number, Rig.position);

                AddForceToRigid(state.StateInput);
                Physics.Simulate(Time.fixedDeltaTime);

                ++rewind_tick_number;
            }

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

    void AddForceToRigid(Vector2 input)
    {
        Rig.AddForce(new Vector3(input.x, 0, input.y) * _forceMultiplier, ForceMode.Force);
    }
}   

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