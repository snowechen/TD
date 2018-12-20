using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorWater : MonoBehaviour {

    Material secondmaps;
	void Start () {
        secondmaps = GetComponent<Renderer>().material;
    }
	
	
	void Update () {
        Vector2 offset = secondmaps.mainTextureOffset;
        offset.y -= 0.01f * Time.deltaTime;
        secondmaps.SetTextureOffset("_MainTex",offset);
        // Debug.Log(secondmaps.GetTextureOffset(8));
    }
}
