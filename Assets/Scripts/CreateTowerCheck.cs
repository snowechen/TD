using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CreateTowerCheck : MonoBehaviour {

    Color NormalColor; //選択したタワー置ける状態の色
    Color _towerC; //今タワーの色
    public Renderer bottom; //model下の色面
    public bool createflag; //置けるかどうか状態
    
    void Start () {
        NormalColor = new Color(0, 1, 0, 0.3f);
        _towerC = NormalColor;
    }
	
	// Update is called once per frame
	//void Update () {

 //       GetComponent<Renderer>().material.color = _towerC; //タワーのmaterialの色を得る

        
 //   }

    //void OnTriggerStay(Collider col)
    //{

    //    if (!col.CompareTag("CreateFloor"))//もしクリエイター出来ないFloorだったら置けない状態になる
    //    {
    //        _towerC = new Color(1, 0, 0, 0.3f);　//タワーのいろを変更
    //        bottom.materials[0].SetColor("_TintColor", new Color(1, 0, 0, 0.5f));//model下の色面、色変更
    //        createflag = false;//置けない状態
    //        Debug.Log(col.gameObject.name);
    //    }
    //    else {
    //        createflag = true;
    //        _towerC = NormalColor;
    //        bottom.materials[0].SetColor("_TintColor", new Color(0, 1, 0, 0.5f));//model下の色面、色変更
    //    }

    //    //もし指定キューブ置き場にあったら位置修正
    //    if (col.CompareTag("CreateFloor_cube"))
    //    {
    //        Vector2 thispos = new Vector2(transform.position.x, transform.position.z);
    //        Vector2 targetpos = new Vector2(col.transform.position.x, col.transform.position.z);
    //        if (Vector2.Distance(thispos,targetpos) <= 2.5f)
    //        {
    //            transform.position = col.transform.position + new Vector3(0, col.transform.localScale.y/2+ 2.5f, 0);
    //            createflag = true;
    //            _towerC = NormalColor;
    //        }else
    //        {
    //            createflag = false;
    //            _towerC = new Color(1, 0, 0, 0.3f); ;
    //        }
            
    //    }
      
       
    //}

    //void OnTriggerExit(Collider col)
    //{
    //    if (!col.CompareTag("CreateFloor"))
    //    {
    //        _towerC = NormalColor;
    //        createflag = true;
    //    }
        
    //}
}
