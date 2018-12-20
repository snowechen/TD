using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TowerCreate : MonoBehaviour {

    public GameObject _tower_cursor;　//置き範囲のオブジェクト
    GameObject Tower_Cursor; //選択したタワーの置き範囲
    public Tower[] towerPrefabs; //クリエイターしたタワーのObject

    public LayerMask mouseMask; //レヤー
    public int prefabSelect;//選択タワー番号
    public float gridIncrement = 2.5f;//マウスが移動どのぐらい時modelを移動、一回2.5を移動する
    bool mouseOnScreen = false;　//マウスがシーンになるか
    Vector3 mouseGridPosition;　//マウスの座標

    bool Cancel = true; //キャンセル状態
    void Start () {
        //初期化マウスのブロック
        Tower_Cursor = Instantiate(_tower_cursor, Vector3.zero, Quaternion.identity) as GameObject;
        Tower_Cursor.SetActive(false);
    }
	
	// Update is called once per frame
	void Update() {

        mouseOnScreen = Input.mousePresent; 
        bool mouseOnUI = EventSystem.current.IsPointerOverGameObject();//マウスの位置がUIにあるのか
        if (!mouseOnUI)
        {
            if (mouseOnScreen)
            {
                RaycastHit hitInfo;
                //カメラからマウスに通じて射線が地面に当たるか
                bool mouseIsOverSomething = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, 100, mouseMask);
                mouseGridPosition = hitInfo.point;　//マウスの位置を取得
                mouseGridPosition.x =
                    Mathf.Round(mouseGridPosition.x / gridIncrement) * gridIncrement;　//マウスのｘ座標を修正
                mouseGridPosition.y =
                    Mathf.Round(mouseGridPosition.y / 5) * 5+2.5f;　//マウスのｙ座標を修正
                mouseGridPosition.z =
                    Mathf.Round(mouseGridPosition.z / gridIncrement) * gridIncrement;　//マウスのｚ座標を修正
                if (Input.GetMouseButtonDown(1) && !Cancel) { SelectCancel(); }　//マウスを右クリックするとキャンセル
            }
            // cursorObject.SetActive(mouseOnScreen);
            if (Tower_Cursor != null)
            {
                //マウスmodel座標をマウス座標にする
                Tower_Cursor.transform.position = mouseGridPosition;
                Color cursorColor = new Color(0.58f, 0.15f, 0.15f, 0.2f);　//今modelの色
                bool isBuildableGround = false;　//当たる範囲がタワー作るか状態
                foreach (var col in Physics.OverlapBox(Tower_Cursor.transform.position, Tower_Cursor.transform.localScale/2))
                {
                    //もし当たる範囲Wall,敵の道,他のタワーがあれば、タワーを作らない
                    if (col.tag == "Wall" || col.tag =="Enemy_Floor" || col.tag=="Tower")
                    {
                        cursorColor = new Color(0.58f, 0.15f, 0.15f, 0.2f);
                        Tower_Cursor.GetComponent<Renderer>().material.SetColor("_TintColor", cursorColor);
                        Tower_Cursor.transform.Find("bottom").GetComponentInChildren<Renderer>().material.SetColor("_TintColor", cursorColor);
                       // Debug.Log(col.tag);
                        return;
                    }
                    else if (col.tag == "CreateFloor")
                    {
                        //タワーを作れるところになったら
                        cursorColor = new Color(0.2f, 0.58f, 0.18f, 0.2f);
                        isBuildableGround = true;
                    }
                    //マウスブロックの色を変わる
                    Tower_Cursor.GetComponent<Renderer>().material.SetColor("_TintColor", cursorColor);
                    //マウスブロック下の部分のmodel色を変わる
                    Tower_Cursor.transform.Find("bottom").GetComponentInChildren<Renderer>().material.SetColor("_TintColor", cursorColor);
                }
                if (Input.GetMouseButtonDown(0) && isBuildableGround)
                {
                    //タワーを作る
                    PlaceTower(towerPrefabs[prefabSelect]);
                }
                
            }
        }

    }
    /// <summary>
    /// タワー作るメソッド
    /// </summary>
    /// <param name="tower">選択したタワー</param>
    void PlaceTower(Tower tower)
    {
        if (!Tower_Cursor.activeInHierarchy) return;
        //もしお金が足りるなら
        if (Nexus.CurrentInstance.UseMana(tower.upgradeCosts[0]))
        {
            //マウスブロックにあるタワーを削除
            Destroy(Tower_Cursor.GetComponentInChildren<Tower>().gameObject);
            Tower_Cursor.SetActive(false);//マウスブロックをオフにする
            //マウスブロックの位置にタワーを生成する
            Instantiate(tower, Tower_Cursor.transform.position - new Vector3(0, 2.5f, 0), Quaternion.identity);
            Cancel = true;//キャンセル状態
        }
    }
   /// <summary>
   /// タワーを選択するメソッド
   /// </summary>
   /// <param name="index">タワー番号</param>
    public void SelectPrefab(int index)
    {
        while (index < 0) index += towerPrefabs.Length; //選択した番号が間違い時
        while (index >= towerPrefabs.Length) index -= towerPrefabs.Length;//選択した番号が間違い時
        //今のお金足りるかをチェック
        if (!(Nexus.CurrentInstance.CurrentMana >= towerPrefabs[index].upgradeCosts[0])) { return; }
        prefabSelect = index;
        //タワーを生成
        Tower tower_mode = Instantiate(towerPrefabs[prefabSelect], Tower_Cursor.transform.position-new Vector3(0,2.5f,0), Quaternion.identity) as Tower;
        //選択したmodelの当たり判定Colliderをオフする
        tower_mode.GetComponentInChildren<Collider>().enabled = false;
        //選択したmodelのshader描画方法を変わる
        tower_mode.GetComponentInChildren<Renderer>().material.shader = Shader.Find("Particles/Additive");
        //選択したタワーのScriptをオフにする
        tower_mode.GetComponent<Tower>().enabled = false;
        //選択したタワーをマウスブロックの子供になる
        tower_mode.transform.SetParent(Tower_Cursor.transform);
        Tower_Cursor.SetActive(true);//マウスブロックをオンにする
        Cancel = false;//キャンセル状態
    }
    /// <summary>
    /// 選択キャンセル
    /// </summary>
    void SelectCancel()
    {
        Destroy(Tower_Cursor.GetComponentInChildren<Tower>().gameObject);
        Tower_Cursor.SetActive(false);
        Cancel = true;
    }
}
