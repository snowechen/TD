using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthDisplayText : MonoBehaviour {

    Nexus target;
    public Text MoneyText;//金钱UI
    public Text HpText;//血量UI
   
    void Start()
    {
        target = Nexus.CurrentInstance;
    }

    
    void OnGUI()
    {
        //刷新金钱的显示
        MoneyText.text ="Money: " + target.CurrentMana + "/" + target.maxMana;
        //刷新血量显示
        HpText.text = "HP: " + target.CurrentHealth + "/" + target.maxHealth;
    }
}
