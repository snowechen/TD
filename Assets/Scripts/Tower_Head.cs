using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower_Head : MonoBehaviour {

    EnemyMovement target = null;
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
        target = GetComponentInParent<Tower>().finalTarget;
        if (target != null)
        {
            Vector3 targetpos = target.transform.position;
            transform.LookAt(new Vector3(targetpos.x, transform.position.y, targetpos.z));
        }
    }
}
