using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionDamage : MonoBehaviour {
    public float hitboxDuration = 0.5f; // 碰撞体持续时间.
    public float selfDestructTime = 2.0f;
    public int power = 10;
    public float slowTime = 0;
    float timeSinceExplosion = 0; // 开始时间

	// Use this for initialization
	void Start () {
        Destroy(gameObject, selfDestructTime);
	}
	
	// Update is called once per frame
	void Update () {
        timeSinceExplosion += Time.deltaTime;
	}

    void OnTriggerEnter(Collider other)
    {
        if (timeSinceExplosion > hitboxDuration) return;
        EnemyMovement hitEnemy = other.GetComponent<EnemyMovement>();
        if (hitEnemy != null)
        {
            //Debug.Log(hitEnemy.gameObject.name + ": " + hitEnemy.CurrentHealth + " HP");
            hitEnemy.Damage(power);
            if (slowTime > 0) hitEnemy.SlowDown(slowTime);
            //Debug.Log("Hit " + hitEnemy.gameObject.name + " with " + power + " damage. It has " + hitEnemy.CurrentHealth + " HP left.");
        }
    }
}
