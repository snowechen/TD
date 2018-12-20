using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {
    public int power = 10; //弾のパワー
    public float range = 10;//移動範囲
    public EnemyMovement target; //敵のscript
    public float travelSpeed; //弾のスピード
    public bool homing = false;
    public float slowTime = 0;

    [Range(0.0f, 1.0f)]public float homingRate = 0.1f;
    Vector3 targetDirection = Vector3.up;

    public GameObject impactPrefab;
    public GameObject ExplosionBoom;

    Vector3 startPosition;

    Vector3 targetLastPosition = Vector3.zero;
    const float lockonThreshold = 1.0f;

    bool firstFrame = false; //prevents unwanted retargeting on the first frame.

	// Use this for initialization
	void Start () {
        if(!homing) targetDirection = target.transform.position-transform.position;
        startPosition = transform.position;
        targetLastPosition = target.transform.position;
    }
	
	// Update is called once per frame
	void FixedUpdate () {
        if (homing)
        {
            if (target)
            {
                if (Vector3.Distance(targetLastPosition, target.transform.position) > lockonThreshold && !firstFrame)
                {
                    Retarget();
                    //Debug.Log("Retargeting...");
                }
               
                targetDirection = Vector3.Slerp(
                targetDirection,
                target.transform.position - transform.position,
                homingRate);
                targetLastPosition = target.transform.position;
               
                firstFrame = false;
            }
            else
            {
                Retarget();
            }
            targetDirection.Normalize();
            transform.Translate(targetDirection * Time.deltaTime * travelSpeed);
        }


        if (!homing) {
            homingRate += Time.deltaTime * travelSpeed;
            // transform.position = Vector3.Slerp(startPosition, targetLastPosition, homingRate);
            targetDirection = Vector3.Slerp(
                 Vector3.zero,
                 targetDirection,
                 homingRate);
            targetDirection.Normalize();
            transform.Translate(targetDirection * Time.deltaTime * travelSpeed);
            if (Vector3.Distance(transform.position, targetLastPosition) < 1) Destroy(gameObject);
        }
        if (Vector3.Distance(transform.position, startPosition) > range) Destroy(gameObject);
	}

    private void OnTriggerEnter(Collider other)
    {
        //if (other.tag == "floor") Destroy(gameObject);
        //Debug.Log("Hit something.");
        EnemyMovement hitEnemy = other.GetComponent<EnemyMovement>();
        if (hitEnemy != null)
        {
            hitEnemy.Damage(power);
            if(slowTime > 0)
            {
                hitEnemy.SlowDown(slowTime);
            }
            //Debug.Log("Enemy hit!!");
            Destroy(gameObject);
        }
    }

    void Retarget()
    {
        Collider[] otherEnemies = Physics.OverlapSphere(transform.position, range);
        foreach (var col in otherEnemies)
        {
            EnemyMovement other = col.GetComponent<EnemyMovement>();
            if (other != null)
            {
                target = other;
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if(impactPrefab)
        {
            Instantiate(impactPrefab, transform.position, Quaternion.identity);
        }
        if (ExplosionBoom)
        {
            GameObject boom = ExplosionBoom;
            boom.GetComponent<GunExplosionBoom>().damage = power;
            Instantiate(boom, transform.position, Quaternion.identity);
        }
    }
}
