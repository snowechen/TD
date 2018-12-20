using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tower_Info : MonoBehaviour {

    Tower tower;
    public LayerMask mouseMask;

    public Text towerName;
    public Text property;
    bool mouseOnScreen = false;

    public Button levelUpBtn;
    public Button SellBtn;
    // Use this for initialization
    public Sprite[] towerImages;
    public Image T_image;
    void Awake () {
        levelUpBtn.gameObject.SetActive(false);
        SellBtn.gameObject.SetActive(false);
        towerName.text = property.text= "";
        T_image.enabled = false;

    }
	
	// Update is called once per frame
	void Update () {
        mouseOnScreen = Input.mousePresent;
        bool mouseOnUI = EventSystem.current.IsPointerOverGameObject();
        RaycastHit hitInfo;
        bool mouseIsOverSomething = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, 100, mouseMask);

        if (!mouseOnUI)
        {
            if (mouseOnScreen)
            {
                if (mouseIsOverSomething && Input.GetMouseButtonDown(0))
                {
                    tower = hitInfo.collider.GetComponentInParent<Tower>();
                    TowerInfoUpdate();
                    
                   // levelUpBtn.GetComponentInChildren<Text>().text = "▲$" + tower.upgradeCosts[tower.level+1].ToString();
                    
                }
                else if(!mouseIsOverSomething && (Input.GetMouseButton(0))){ panelclose(); }
                else if (Input.GetKeyDown(KeyCode.Escape)) { panelclose(); }
            }
        }
        if (tower!=null) {
            if (tower.level + 1 >= 5) levelUpBtn.gameObject.SetActive(false);
        }

    }
    public void OnClick()
    {
         tower.Upgrade();
        TowerInfoUpdate();
    }

    public void DeleteTower(AudioClip audio)
    {
        GameObject a = new GameObject();
        a.AddComponent<AudioSource>().clip=audio;
       
        a.GetComponent<AudioSource>().Play();
        a.GetComponent<AudioSource>().volume = 0.3f;
        Destroy(a, 2);
        tower.Sell();
        panelclose();
    }

    /// <summary>
    /// 更新塔信息
    /// </summary>
    void TowerInfoUpdate()
    {
        string tl = (tower.level + 1).ToString();
        if (tower.level + 1 >= 5) tl = "Max";
        else levelUpBtn.GetComponentInChildren<Text>().text = "▲$" + tower.upgradeCosts[tower.level + 1].ToString();
        towerName.text = tower.Name;
        property.text = "Level:" + tl + "\n攻撃力:" + tower.powerdisplay+"\n範囲:"+tower.Range.ToString("f0")+"\n射速:"+tower.ReloadRate;
        
        levelUpBtn.gameObject.SetActive(true);
        SellBtn.gameObject.SetActive(true);
        T_image.sprite = towerImages[tower.Tower_ID];
        T_image.enabled = true;
    }

    /// <summary>
    /// 清除塔信息
    /// </summary>
    void panelclose()
    {
        levelUpBtn.gameObject.SetActive(false); SellBtn.gameObject.SetActive(false);
        towerName.text = property.text = "";
        T_image.enabled = false;
    }
}
