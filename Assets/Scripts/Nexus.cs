/* =============================
 * 作者：Snowe (斯诺)
 * QQ：275273997
 * Email：snowe0517@gmail.com ，snowe@isnowe.com
 *==============================*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Nexus : MonoBehaviour {
    static Nexus currentInstance;

    public int maxHealth = 1000;
    public int maxMana = 1000;
    public float manaRegenRate = 1.0f; // per second

    int currentHealth;
    int currentMana;
    int manaSpent = 0; //计算金钱消耗总量

    //float manaRegenCurrent = 0;

    MeshRenderer mRen;//模型网格

    //基地爆炸的对象组建
    public GameObject explosionPrefab;

    /// <summary>
    /// 获取当前血量
    /// </summary>
    public int CurrentHealth
    {
        get { return currentHealth; }
    }
    /// <summary>
    /// 当前血量百分比
    /// </summary>
    public float CurrentHealthPercent
    {
        get { return (float)currentHealth / (float)maxHealth; }
    }
    /// <summary>
    /// 获取当前能源
    /// </summary>
    public int CurrentMana
    {
        get { return currentMana; }
    }
    /// <summary>
    /// 能源百分比
    /// </summary>
    public float CurrentManaPercent
    {
        get { return (float)currentMana / (float)maxMana; }
    }
    /// <summary>
    /// 死亡判定值
    /// </summary>
    public bool IsDead
    {
        get { return currentHealth <= 0; }
    }
    /// <summary>
    /// 获取静态化对象
    /// </summary>
    public static Nexus CurrentInstance
    {
        get { return currentInstance; }
    }

	// 初始化
	void Awake () {
        currentHealth = maxHealth;
        currentMana = 200;
        currentInstance = this;
        mRen = GetComponent<MeshRenderer>();
        explosionPrefab.SetActive(false);

    }
	public void GoToTitle()
    {
        SceneManager.LoadScene(0);
    }
	// Update is called once per frame
	void Update () {
        //manaRegenCurrent += Time.deltaTime * manaRegenRate;
        //while (manaRegenCurrent > 1)
        //{
        //    manaRegenCurrent -= 1;
        //    currentMana += 1;
        //}

        if (Input.GetKey(KeyCode.Alpha1)) Time.timeScale = 5;
        else Time.timeScale = 1;
        // if(Input.GetKeyUp())
        if (IsDead)
        {
            Time.timeScale = 0;
            if (Input.GetKeyDown(KeyCode.Space))
            SceneManager.LoadScene(0);
        }
    }
    /// <summary>
    /// 增加伤害
    /// </summary>
    /// <param name="power">伤害</param>
    public void Damage(int power)
    {
        bool alreadyDead = IsDead;
        currentHealth -= power;
        if (IsDead && !alreadyDead) Death();
    }
    /// <summary>
    /// 增加能源
    /// </summary>
    /// <param name="amount"></param>
    public void GainMana(int amount)
    {
        currentMana += amount;
    }

    /// <summary>
    /// 消费能源
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool UseMana(int amount)
    {
        //判断能源是否足够
        if(amount > currentMana)
        {
            Debug.Log("钱不够!");
            return false;
        }
        //减少当前能源
        currentMana -= amount;
        //增加消费总量
        manaSpent += amount;
        return true;
    }

    /// <summary>
    /// 死亡判定
    /// </summary>
    void Death()
    {
        explosionPrefab.SetActive(true);
        
        //Instantiate(explosionPrefab, transform.position, transform.rotation);
        mRen.enabled = false;
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
       
    }
}
