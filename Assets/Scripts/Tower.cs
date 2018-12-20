/* =============================
 * 作者：Snowe (斯诺)
 * QQ：275273997
 * Email：snowe0517@gmail.com ，snowe@isnowe.com
 *==============================*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour {
    //タワーname
    public string Name;
    //攻撃距離
    public float range = 20;
    //最大距離
    public float maxRange = 30;
     //リロード時間
    public float reloadRate = 1;
    //最もリロード時間
    public float finalReloadRate = 1;
    //最大パワーの倍数
    public float finalPowerMultiplier = 1f;
    //弾のプロジェクト
    public Projectile projectilePrefab;
    //弾を発射するときの位置
    public Transform projectileSpawnPoint;

    //後何秒発射のTime
    float currentReloadTime = 0;

    //今のlevel
    public int level = 0;
    //タワーScale用変数
    //塔大小基础值
    public float scaleMultiplier = 0.0005f;
    //タワー最大Level
    //塔最大等级
    const int maxLevel = 4;
    //タワーID、今どんなタワー選択したか
    //防御塔ID
    public int Tower_ID;
    //タワーの値段Levelup値段
    //塔的价格列表
    public int[] upgradeCosts = { 100, 150, 250, 320, 500 };
    //敵のScript
    //敌人的脚本
    public EnemyMovement finalTarget { get; set; }
    //音が保存している配列
    //音效列表
    public AudioClip[] audioClip;
    /// <summary>
    /// Levelを取得と設定
    /// Levelを設定すると、タワーのサイズも増えてることになる
    /// 获取塔的等级，和根据塔的等级改变大小
    /// </summary>
    public int Level
    {
        get { return level; }
        set
        {
            level = value;
            //限制塔的最大等级
            if (level > maxLevel) level = maxLevel;
            //计算塔的大小
            float towerSize = scaleMultiplier * (level + 12f);
            //设定塔对象的大小
            transform.localScale = new Vector3(towerSize, towerSize, towerSize);
        }
    }
    //攻撃範囲を取得
    //攻击范围
    public float Range
    {
        //レベルによって変化する
        //根据等级获取攻击范围
        get { return Mathf.Lerp(range,maxRange , (float)level/maxLevel); }
    }
    //発射リロード時間を取得
    //攻击间隔
    public float ReloadRate
    {
        //レベルによって変化する
        //根据等级获取攻击间隔
        get { return Mathf.Lerp(reloadRate,finalReloadRate , (float)level/maxLevel); }
    }
    //パワーの倍数を取得
    //塔的攻击倍数
    public float PowerMultiplier
    {
        //レベルによって変化する
        //根据等级计算攻击力倍数
        get { return Mathf.Lerp(1,finalPowerMultiplier, (float)level/(float)maxLevel); }
    }
    //表示用パワー変数
    //显示用攻击力变量
    public float powerdisplay;
    

    void Start()
    {
        //表示用パワー変数初期化
        //初始化攻击力显示数值
        powerdisplay = projectilePrefab.power;
    }

    // Update is called once per frame
    void Update()
    {
        //今のリロード時間０より大きいなら
        //判断攻击时间
        if (currentReloadTime > 0)
        {
            //減ること
            //倒计时
            currentReloadTime -= Time.deltaTime;
        }
        else
        {
            //当たり範囲の配列
            //寻找攻击范围内的敌人
            Collider[] colList = Physics.OverlapSphere(transform.position, range);
            //敌人对象初始值
            finalTarget = null;//敵変数空にする
            //计算距离用变量
            float minimumDistance = 0;//距離計算用変数
            
            //遍历查找到的对象
            foreach (var col in colList)
            {
                //敵のScriptを持っているProjectは新しい敵scriptに代入
                //获取到敌人的脚本
                EnemyMovement target = col.GetComponent<EnemyMovement>();
                if (target != null)
                {
                    //もし目標敵が空なら上げる　
                    //如果目标为空就重新赋值
                    if (finalTarget == null)
                    {
                        finalTarget = target;
                        minimumDistance = target.RemainingDistance;
                    }
                    //敵の距離小より前の距離なら
                    //查找到的目标对象距离小于之前的距离时重新赋值目标对象
                    if (target.RemainingDistance < minimumDistance)
                    {
                        finalTarget = target;
                        minimumDistance = target.RemainingDistance;
                    }
                }
            }
            //目标不为空时进行攻击
            if (finalTarget != null) Attack(finalTarget);
        }
       
    }

    /// <summary>
    /// 発射
    /// 塔的攻击处理
    /// </summary>
    /// <param name="target">敵</param>
    void Attack(EnemyMovement target)
    {
        //生成子弹
        Projectile instance = Instantiate(projectilePrefab, projectileSpawnPoint.transform.position, Quaternion.identity);
        instance.target = target;//定义目标
        instance.range = Range;//攻击范围
        instance.power = (int)(instance.power * PowerMultiplier);//子弹威力
        currentReloadTime = ReloadRate;//重置攻击时间
        Destroy(instance.gameObject, 5);//5秒删除对象
        
        //Debug.Log("Reload Time: " + reloadTime);
    }

    /// <summary>
    /// タワーのレベルアップ
    /// 塔升级
    /// </summary>
    /// <returns>成功するかどうか</returns>
   public  bool Upgrade()
    {
        int startingLevel = Level;
        //判断是否到最高等级
        if (startingLevel + 1 > maxLevel)
        {
            Debug.Log("已经到了最大等级.");
            return false;
        }
        //判断有没有钱
        if (!Nexus.CurrentInstance.UseMana(upgradeCosts[startingLevel + 1])) return false;
        Level++;
        //威力提升计算
        powerdisplay = projectilePrefab.power;
        powerdisplay = (int)(powerdisplay * PowerMultiplier);
        NewAudio(audioClip[0]);//创建塔升级音乐
        return true;
    }
    /// <summary>
    /// タワーを売る
    /// 卖掉塔
    /// </summary>
    public void Sell()
    {
        int money = 0;
        for (int i = 0; i <= level; i++)
        {
            //计算此塔的总消费
            money += upgradeCosts[i];
        }
        //计算卖出后得价格
        money = (int)(money * 0.7f);
        //增加能源
        Nexus.CurrentInstance.GainMana(money);

        NewAudio(audioClip[1]);//创建卖掉塔的音乐

        Destroy(this.gameObject);
    }
    /// <summary>
    /// Soundを生成してプレイしてから削除
    /// 生成音效，播放后自动删除
    /// </summary>
    /// <param name="clip">音效</param>
    void NewAudio(AudioClip clip)
    {
        //新しいProjectを生成
        GameObject newaudio = new GameObject();
        //soundを設定
        newaudio.AddComponent<AudioSource>().clip = clip;
        //soundの音量を設定
        newaudio.GetComponent<AudioSource>().volume = 0.2f;
        //soundをプレイ
        newaudio.GetComponent<AudioSource>().Play();
        //2秒後削除
        Destroy(newaudio, 2);
    }
}
