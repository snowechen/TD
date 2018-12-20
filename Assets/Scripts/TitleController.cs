using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleController : MonoBehaviour {

    public GameObject titlebackground;
    public GameObject titleText;
    public Text pressKey;

    public Image panel;
    public Image Loadimage;

    private float BkH = 0;
    Color titleColor;

    bool isGameStart;
    void Start () {
        Time.timeScale = 1;
        titlebackground.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,0);
        titleColor = titleText.GetComponent<Text>().color;
        titleColor.a = 0;
        titleText.GetComponent<Text>().color = titleColor;
        panel.color = new Color(0, 0, 0, 0);
        StartCoroutine(PressKey());
        StartCoroutine(Title());
    }
	
    IEnumerator PressKey()
    {
        Color presskeycolor = pressKey.color;
        float a = 0.05f;
        while (true)
        {
            presskeycolor.a += a;
            pressKey.color = presskeycolor;
            if (presskeycolor.a<=0 || presskeycolor.a>=1) a = a * -1;
            yield return new WaitForSeconds(0.05f);
        }
        
    }

    IEnumerator Title()
    {
        while (true)
        {
            BkH = Mathf.Lerp(BkH, 200, 0.05f);
            titlebackground.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BkH);
            if (BkH >= 190) titleColor.a += Time.deltaTime;
            titleText.GetComponent<Text>().color = titleColor;
            if (titleColor.a >= 1) break;
            yield return new WaitForFixedUpdate();
        }
    }
	// Update is called once per frame
	void Update () {


        if (Input.GetKeyDown(KeyCode.Space))
        {
            isGameStart = true;
            SceneController.Instacne.SwitchScene("main");
        }
        //if(isGameStart)
        //{
        //    panel.color += new Color(0, 0, 0, 0.02f);
        //    Loadimage.gameObject.SetActive(true);
        //    Quaternion loading = Loadimage.transform.rotation;
        //    loading.eulerAngles -= new Vector3(0, 0, 15);
        //    Loadimage.transform.rotation = loading;
        //    if (panel.color.a >= 1) SceneManager.LoadScene(1);
        //}
    }
}
