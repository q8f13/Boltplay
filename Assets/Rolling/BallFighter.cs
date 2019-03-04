using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallFighter : MonoBehaviour
{
    private Rigidbody _rig;

    [SerializeField]
    private float _forceMultiplier = 1.0f;

    [SerializeField]
    private InputSource _input;

    private Vector2 _currentForce;

    // Start is called before the first frame update
    void Start()
    {
        _rig = GetComponent<Rigidbody>();
    }

    private void Update() {
        switch(_input)
        {
            case InputSource.Keyboard:
                _currentForce.x = Input.GetAxis("Horizontal");
                _currentForce.y = Input.GetAxis("Vertical");
                break;
            case InputSource.Mouse:
                _currentForce.x = Input.GetAxis("Mouse X");
                _currentForce.y = Input.GetAxis("Mouse Y");
                break;
        }
    }

    private void FixedUpdate() {
        _rig.AddForce(new Vector3(_currentForce.x, 0, _currentForce.y) * _forceMultiplier, ForceMode.Force);
    }
}   

public enum InputSource
{
    None = 0,
    Keyboard,
    Mouse,
}