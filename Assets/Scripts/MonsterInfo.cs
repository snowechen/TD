using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonsterInfo : MonoBehaviour {
    public static MonsterInfo instance;
    public Text timeText;
  //  public Text EnemyName;
	// Use this for initialization
	void Awake () {
        instance = this;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void TimeUpdate(string msg)
    {
        timeText.text = "下一波敌人: " + msg;
    }
}
