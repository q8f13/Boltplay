using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallFighter : Bolt.EntityEventListener<IBallState>
{
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
    private MoonBehaviour _moonBh;

    private Vector2 _currentForce;

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

        state.AddCallback("BallRig", ()=>
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
        });
    }

    public void SetupRolling(BoltEntity moon)
    {
        _moon = moon.GetComponent<SpringJoint>();
        _moon.connectedBody = Rig;
        _moonBh = _moon.GetComponent<MoonBehaviour>();
    }

    public override void SimulateOwner()
    {
        switch(_input)
        {
            case InputSource.Keyboard:
                _currentForce.x = Input.GetAxis("Horizontal");
                _currentForce.y = Input.GetAxis("Vertical");
                break;
/*             case InputSource.Mouse:
                _currentForce.x = Input.GetAxis("Mouse X");
                _currentForce.y = Input.GetAxis("Mouse Y");
                break; */
        }

        state.CurrentForce = _currentForce;

        BallRigid br = state.BallRig;
        if(br!=null)
        {
            br.RigVelocity = _rig.velocity;
            br.Drag = _rig.drag;
            br.AngularDrag = _rig.angularDrag;
            br.AngulalrVelocity = _rig.angularVelocity;
        }
    }

    private void Update() {
        if(Application.isEditor)
        {
            switch(_input)
            {
                case InputSource.Keyboard:
                    _currentForce.x = Input.GetAxis("Horizontal");
                    _currentForce.y = Input.GetAxis("Vertical");
                    break;
    /*             case InputSource.Mouse:
                    _currentForce.x = Input.GetAxis("Mouse X");
                    _currentForce.y = Input.GetAxis("Mouse Y");
                    break; */
            }
        }

        state.SetTransforms(state.BallTransform, transform);
    }

    private void FixedUpdate() {
        if(_rig == null)
            _rig = GetComponent<Rigidbody>();

        _rig.AddForce(new Vector3(_currentForce.x, 0, _currentForce.y) * _forceMultiplier, ForceMode.Force);
    }
}   

public enum InputSource
{
    None = 0,
    Keyboard,
    Mouse,
}