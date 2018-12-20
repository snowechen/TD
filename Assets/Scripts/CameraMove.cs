using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraMove : MonoBehaviour {

    Vector2 mousePos;
    Camera maincamera;
    public float speed;
	// Use this for initialization
   // Vector3 max
	void Start () {
        maincamera = Camera.main;
	}
	
	// Update is called once per frame
	void Update () {
        mousePos = Input.mousePosition;
        Vector2 max = new Vector2(Camera.main.pixelWidth, Camera.main.pixelHeight);
        Vector3 CameraPos = maincamera.transform.position;
        if (mousePos.x >= max.x-5) { CameraPos.x += (mousePos.x-(max.x-5)) * Time.deltaTime *speed;  }
        if (Input.GetKey(KeyCode.D)) { CameraPos.x += 2;  }
        if(mousePos.x<= 0) { CameraPos.x +=- (5-mousePos.x) * Time.deltaTime * speed; }
        if(Input.GetKey(KeyCode.A)) { CameraPos.x -= 2; }
        if (mousePos.y >= max.y-5) { CameraPos.z += (mousePos.y-(max.y-5)) * Time.deltaTime * speed;  }
        if (Input.GetKey(KeyCode.W)) { CameraPos.z += 2; }
        if (mousePos.y <= 0) { CameraPos.z +=- (5-mousePos.y) * Time.deltaTime * speed;  }
        if (Input.GetKey(KeyCode.S)) { CameraPos.z -= 2; }

        Clamp(ref CameraPos);
        
        if (CameraPos.y>=20 && CameraPos.y <= 80)
        {
            CameraPos.y -= Input.GetAxis("Mouse ScrollWheel") * 10;
        }
        if (CameraPos.y <= 20) CameraPos.y = 20;
        if (CameraPos.y >= 80) CameraPos.y = 80;
        maincamera.transform.position = CameraPos;
        

    }

    void Clamp(ref Vector3 pos)
    {
        if (pos.x > 300) pos.x = 300;
        if (pos.x < 188) pos.x = 188;
        if (pos.z > 180) pos.z = 180;
        if (pos.z < 45) pos.z = 45;
    }
}
