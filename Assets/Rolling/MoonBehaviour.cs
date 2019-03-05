using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoonBehaviour : Bolt.EntityBehaviour<IMoonState>
{
    private Rigidbody _rig;
    private SpringJoint _spring;

    private Vector3 _currentPos;
    private Quaternion _currentRot;

    public override void Attached()
    {
        _rig = GetComponent<Rigidbody>();
        _spring = GetComponent<SpringJoint>();

        state.AddCallback("RigVelocity", ()=>
        {
            _rig.velocity = state.RigVelocity;
        });

        state.AddCallback("RigPosition", ()=>
        {
            transform.position = state.RigPosition;
            transform.rotation = state.RigRotation;
        });
    }

    public override void SimulateOwner()
    {
        state.RigVelocity = _rig.velocity;
        state.RigPosition = _currentPos;
        state.RigRotation = _currentRot;
    }

    private void FixedUpdate() {
        _currentPos = _rig.position;
        _currentRot = _rig.rotation;
    }
}
