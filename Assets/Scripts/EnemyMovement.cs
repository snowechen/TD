/* =============================
 * 作者：Snowe (斯诺)
 * QQ：275273997
 * Email：snowe0517@gmail.com ，snowe@isnowe.com
 *==============================*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour {

    NavMeshAgent agent; //寻路
    Transform Exit; //出口
    public int baseMaxHealth = 100;//基础血量
    public int speed = 10; //速度

    public int scoreValue = 10;//分数
    public int killManaReward = 5;//

    public int level = 0;

   public int currentHealth;

    float slowTime = 0f;
    Vector3 startPos;
    public int CurrentHealth
    {
        get { return currentHealth; }
    }
    public int MaxHealth
    {
        get { return baseMaxHealth + 10 * level; }
    }
    public float CurrentHealthPercent
    {
        get { return (float)currentHealth / (float)MaxHealth; }
    }
    public int EffectiveSpeed // Halves speed if slowed.
    {
        get
        {
            int output = speed;
            if (slowTime > 0) output /= 5;
            return output;
        }
    }
    void Start () {
        //获取寻路组件
        agent = GetComponent<NavMeshAgent>();
        //设定目的地
        agent.destination = Nexus.CurrentInstance.transform.position;
        //初始化血量
        currentHealth = MaxHealth;
        //初始化价值
        killManaReward = killManaReward + level * 5;
        //起始坐标
        startPos = transform.position;
    }
	
	// Update is called once per frame
	void FixedUpdate () {
       
        if (slowTime > 0)
        {
            slowTime -= Time.deltaTime;
            //if (slowTime <= 0) agent.speed = speed;
        }
        agent.speed = EffectiveSpeed;
    }
    /// <summary>
    /// 剩余距离
    /// </summary>
    public float RemainingDistance
    {
        get
        {
            float distance = 0.0f;
            if (agent == null) return float.MaxValue;//如果为空返回最大值
            Vector3[] corners = agent.path.corners;//获取路径坐标点组
            for (int c = 0; c < corners.Length - 1; ++c)
            {
                //累加坐标距离
                distance += Mathf.Abs((corners[c] - corners[c + 1]).magnitude);
            }
            //返回距离
            return distance;
        }
    }
    /// <summary>
    /// 伤害
    /// </summary>
    /// <param name="amount">值</param>
    public void Damage(int amount)
    {
        //减少血量
        currentHealth -= amount;
        //死亡判定
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
            //增加能源
            Nexus.CurrentInstance.GainMana(killManaReward);
            //GameStateManager.CurrentInstance.AddScore(scoreValue);
        }
    }
    /// <summary>
    /// 减速时间
    /// </summary>
    /// <param name="time">时间</param>
    public void SlowDown(float time)
    {
        if (slowTime < time) slowTime = time;
        //agent.speed = EffectiveSpeed;
    }

    //碰撞
    void OnTriggerEnter(Collider col)
    {
        //是否碰到的是基地
        if (col.CompareTag("Exit"))
        {
            //给基地提交伤害
            col.GetComponent<Nexus>().Damage(CurrentHealth);
            //初始化路坐标
            agent.Warp(startPos);
            //设定目的地
            agent.destination = Nexus.CurrentInstance.transform.position;
        }
    }
}
