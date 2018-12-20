/* =============================
 * 作者：Snowe (斯诺)
 * QQ：275273997
 * Email：snowe0517@gmail.com ，snowe@isnowe.com
 *==============================*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpwan_property
{
    public string name;　//enemy name
    public int enemyLevel; //等级
    public int total; // 总数
    public EnemyMovement enemy;//enemy 本体
}
public class EnemySpawner : MonoBehaviour {

    public float WaveReloadTime; //每回合等待时间
    public float EnemyInterval; // 每个怪刷出间隔
    float currReloadTime;//当前时间
    float currInterval;//当前刷怪间隔
    public List<EnemySpwan_property> enemyList = new List<EnemySpwan_property>(); //

    int currentWave = 0; //当前波数初始值

    public float  currentReloadTime { get { return currReloadTime; } }
 
    void Start () {
        currReloadTime = 5;
        StartCoroutine(EnemySpawn());
    }
	
    /// <summary>
    /// 敌人刷新控制
    /// </summary>
    /// <returns></returns>
    IEnumerator EnemySpawn()
    {
        while (currentWave < enemyList.Count)
        {
            var EL = enemyList[currentWave];
            if (EL.total > 0 && currReloadTime <= 0)
            {
                if (TimeDown(ref currInterval))
                {
                    EnemyMovement enemy = EL.enemy;
                    enemy.level = EL.enemyLevel;
                    Instantiate(enemy, transform.position, Quaternion.identity);
                    enemyList[currentWave].total--;
                    currInterval = EnemyInterval;
                }
            }
            else
            {
                TimeDown(ref currReloadTime);
                
                if (enemyList[currentWave].total <= 0)
                {
                    currentWave++;
                    currReloadTime = WaveReloadTime;
                }
            }
            MonsterInfo.instance.TimeUpdate(currentReloadTime.ToString("f2"));
            if (EL.total > 0 && currReloadTime <= 0)
            {
                //显示下一波
                MonsterInfo.instance.TimeUpdate(EL.name + " Lv: " + EL.enemyLevel);
            }
            yield return new WaitForFixedUpdate();
        }
    }
   

    //void Update()
    //{
        
       
    //}

    bool TimeDown(ref float time)
    {
        time -= Time.deltaTime;
        if (time <= 0) { time = 0; return true; }
        return false;
    }

    EnemyMovement SetEnemy(EnemyMovement enemy,EnemySpwan_property ESP)
    {
        enemy.level = ESP.enemyLevel;
        enemy.name = ESP.name;

        return enemy;
    }
}
