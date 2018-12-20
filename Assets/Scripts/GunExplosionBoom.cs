using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunExplosionBoom : MonoBehaviour {

    public int damage;
	void Start () {
        Destroy(gameObject, 0.1f);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnTriggerEnter(Collider col)
    {
        EnemyMovement enemy = col.GetComponent<EnemyMovement>();
        if (enemy != null)
        {
            enemy.Damage(damage);
        }
    }
}
