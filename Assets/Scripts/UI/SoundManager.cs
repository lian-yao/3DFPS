using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    [Header("背景音乐")]
    public AudioClip bgmClip; // 拖入背景音乐文件
    [Range(0, 1)] public float bgmVolume = 0.5f; // 背景音乐音量
    private AudioSource bgmSource;

    // 单例模式
    public static SoundManager Instance;

    void Awake()
    {
        // 单例：保证全局唯一
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化音频组件
        bgmSource = GetComponent<AudioSource>();
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        // 监听场景切换事件（核心：场景变化时控制BGM）
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // 启动时自动播放BGM（仅start场景生效）
        if (SceneManager.GetActiveScene().name == "start")
        {
            PlayBGM();
        }
    }

    // 场景加载完成后触发
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果进入游戏场景（如test/test2），停止BGM
        if (scene.name != "start")
        {
            StopBGM();
        }
        // 如果回到start场景，重新播放BGM
        else
        {
            PlayBGM();
        }
    }

    // 播放BGM
    public void PlayBGM()
    {
        if (bgmSource != null && bgmClip != null && !bgmSource.isPlaying)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
            Debug.Log("start场景BGM开始播放");
        }
    }

    // 停止BGM
    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
            Debug.Log("进入游戏场景，BGM已停止");
        }
    }

    // 移除监听（防止内存泄漏）
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}