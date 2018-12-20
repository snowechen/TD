using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
[Serializable]
public class SceneBGM
{
    public string Name;
    public AudioClip clip;
}
public class SceneController : MonoBehaviour
{
    private static SceneController instance;
    public static SceneController Instacne
    {
        get
        {
            if(!instance)
            {
                instance = FindObjectOfType<SceneController>();
            }
            if(!instance)
            {
                Debug.Log("场景控制物体不存在！");
            }
            return instance;
        }
    }

    public bool IsLoadingScene
    {
        get
        {
            return isLodaingScene;
        }
    }

    [SerializeField]
    private Fade fade;//幕布
    [SerializeField]
    private Text loadText;//加载文本
    [SerializeField]
    private String firstSceneName;//初始场景名
    [SerializeField]
    private GameObject loadAnim;//加载动画
    [SerializeField]
    private Text loadUI;//显示加载进度

    [SerializeField,Header("背景音乐")]
    List<SceneBGM> BGMS;//所有BGM的存放
    //private int BGMIndex;//现在需要播放的BGM指针
    AudioSource BGMaudio;//BGM播放器

    private bool isLodaingScene; // 是否处于加载Scene的过程中
  
    private void Awake()
    {
        BGMaudio = GetComponent<AudioSource>();
    }

    private void Start()
    {
        StartCoroutine(ChangeScene(firstSceneName));
        loadText.text = "Please Wait";
    }
    
    /// <summary>
    /// 加载并切换到新的场景
    /// </summary>
    /// <param name="sceneName">场景名字</param>
    public void SwitchScene(string sceneName)
    {
        StartCoroutine(ChangeScene(sceneName));
    }

    /// <summary>
    /// 加载场景的协程
    /// </summary>
    /// <param name="sceneName"></param>
    /// <returns></returns>
    private IEnumerator ChangeScene(string sceneName)
    {
        isLodaingScene = true;
        // 如果有别的场景在，就先淡出
        if (SceneManager.sceneCount >= 2)
        {
            yield return fade.FadeOut();
        }

        // 先释放掉其他的场景
        while(SceneManager.sceneCount >= 2)
        {
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(SceneManager.sceneCount - 1));
        }

        // 然后开始加载新场景
        yield return LoadSceneAsync(sceneName);

        //切换背景音乐BGM
        BGMChange(sceneName);

        isLodaingScene = false;
        // 加载完成后淡入
        yield return fade.FadeIn();
    }
    
    /// <summary>
    /// 异步加载一个场景
    /// </summary>
    /// <param name="sceneName">需要加载的场景名</param>
    /// <returns></returns>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        int currProgress = 0;
        int showProgress = 0;
        loadText.enabled = true;
        loadAnim.SetActive(true);
        loadUI.enabled = true;

        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        async.allowSceneActivation = false;

        
        while (async.progress<0.9f)
        {
            currProgress = (int)(async.progress * 100);
            while (showProgress < currProgress)
            {
                showProgress++;
                setProgressValue(showProgress);
                yield return new WaitForEndOfFrame(); //等待一帧
            }
            
            
            //loadText.text = "NowLoading: " + (asyncOperation.progress * 100).ToString("f1") + "%";
            yield return null;
        }

        currProgress = 100;

        while (showProgress < currProgress)
        {
            showProgress++;
            setProgressValue(showProgress);
            yield return new WaitForEndOfFrame(); //等待一帧
        }

        async.allowSceneActivation = true;
        yield return async.isDone;
        SceneManager.SetActiveScene(SceneManager.GetSceneAt(SceneManager.sceneCount - 1));

        loadAnim.SetActive(false);
        loadText.enabled = false;
        loadUI.enabled = false;
    }

    void setProgressValue(int value)
    {
        loadUI.text = value + "%";
    }
    /// <summary>
    /// 切换背景音乐
    /// </summary>
    /// <param name="name"></param>
    void BGMChange(string name)
    {
        for (int i = 0; i < BGMS.Count; i++)
        {
            if (BGMS[i].Name == name)
            {
                BGMaudio.clip = BGMS[i].clip;
                BGMaudio.Play();
                BGMaudio.loop = true;
            }
        }
      
    }
    /// <summary>
    /// BGM 音量
    /// </summary>
    /// <param name="volume">音量</param>
    public void BGMvolume(float volume)
    {
        BGMaudio.volume = volume;
    }
}
