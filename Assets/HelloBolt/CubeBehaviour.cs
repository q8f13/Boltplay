using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeBehaviour : Bolt.EntityEventListener<ICubeState>
{
    private MeshRenderer _mr;
    private float _resetColorTime;

    [SerializeField]
    private GameObject[] _weaponObjects;

    public override void Attached()
    {
        _mr =GetComponent<MeshRenderer>();

        state.SetTransforms(state.CubeTransform, transform);

        if(entity.isOwner)
        {
            state.CubeColor = Random.ColorHSV();

            for(int i=0;i<state.WeaponArray.Length;i++)
            {
                state.WeaponArray[i].WeaponId = Random.Range(0, _weaponObjects.Length - 1);
                state.WeaponArray[i].WeaponAmmo = Random.Range(50,100);
            }

            state.WeaponActiveIdx = -1;
        }

        state.AddCallback("CubeColor", ()=>{
            _mr.material.color = state.CubeColor;
        });

        state.AddCallback("WeaponActiveIdx", ()=>
        {
            int objectId = state.WeaponActiveIdx < 0 ? -1 : state.WeaponArray[state.WeaponActiveIdx].WeaponId;

            for(int i=0;i<_weaponObjects.Length;i++)
            {
                _weaponObjects[i].SetActive(objectId == i);
            }
        });
    }

    private void OnGUI() {
        if(entity.isOwner)
        {
            GUI.color = state.CubeColor;
            GUILayout.Label("@@@");
            GUI.color = Color.white;
        }
    }

    public override void SimulateOwner()
    {
		var speed = 4f;
		var movement = Vector3.zero;

		if (Input.GetKey(KeyCode.W)) { movement.z += 1; }
		if (Input.GetKey(KeyCode.S)) { movement.z -= 1; }
		if (Input.GetKey(KeyCode.A)) { movement.x -= 1; }
		if (Input.GetKey(KeyCode.D)) { movement.x += 1; }

		if (Input.GetKeyDown(KeyCode.Alpha1)) state.WeaponActiveIdx = 0;
		if (Input.GetKeyDown(KeyCode.Alpha2)) state.WeaponActiveIdx = 1;
		if (Input.GetKeyDown(KeyCode.Alpha3)) state.WeaponActiveIdx = 2;
		if (Input.GetKeyDown(KeyCode.Alpha0)) state.WeaponActiveIdx = -1;

		if (movement != Vector3.zero)
		{
			transform.position = transform.position + (movement.normalized * speed * BoltNetwork.FrameDeltaTime);
		}

        // flash color
        if(Input.GetKeyUp(KeyCode.F))
        {
            FlashColorEvt flash = FlashColorEvt.Create(entity);
            flash.FlashColor = Color.red;
            flash.Send();
        }
	}

    public override void OnEvent(FlashColorEvt evnt)
    {
        _resetColorTime = Time.time + 0.2f;
        _mr.material.color = evnt.FlashColor;
    }

    private void Update() {
        if(_resetColorTime < Time.time)
        {
            _mr.material.color = state.CubeColor;
        }
    }
}
