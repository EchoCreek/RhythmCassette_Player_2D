using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // 【新增】用于加载本地文件
using System.Collections;   // 【新增】用于使用协程
using System.Collections.Generic;
using TMPro; // 【新增】引入TextMeshPro的命名空间
using UnityEngine.SceneManagement; // 【新增】引入场景管理命名空间

/// <summary>
/// 音乐播放的总控制器。
/// 负责处理音频播放、暂停、进度控制、搓碟效果以及从本地加载音乐和壁纸文件。
/// 采用“角度累积”算法，彻底解决了在处理360度环绕输入时的“跳变”和“抽搐”问题。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicPlayerController : MonoBehaviour
{
    //======================================================================
    // 核心引用 (在Unity Inspector中拖拽赋值)
    //======================================================================
    [Header("核心引用")]
    [Tooltip("用于显示和控制歌曲进度的滑块")]
    [SerializeField] private Slider _songProgressBar;

    [Tooltip("需要同步更换壁纸的所有Image组件列表")]
    [SerializeField] private List<Image> _cassetteWallpaperImages; // 【修正】将单个Image改为Image列表

    [Tooltip("需要同步显示歌曲名称的所有【TextMeshPro UGUI】组件列表")]
    [SerializeField] private List<TextMeshProUGUI> _songTitleTexts;

    //======================================================================
    // 搓碟效果设置
    //======================================================================
    [Header("搓碟效果设置")]
    [Tooltip("通过拖动进度滑块实现搓碟效果的灵敏度。")]
    [SerializeField] private float _sliderScratchSensitivity = 40f;

    [Tooltip("转盘搓碟时，旋转多少度对应音乐时间变化一秒。负值代表反向。")]
    [SerializeField] private float _scratchDegreesPerSecond = -90f;

    [Tooltip("转盘搓碟时，音高变化的灵敏度。")]
    [SerializeField] private float _scratchPitchSensitivity = 3f;


    //======================================================================
    // 公开状态属性 (供其他脚本读取)
    //======================================================================
    public bool IsPlaying { get; private set; } // 音乐是否正在播放
    public float CurrentTime => _audioSource.time; // 音频源的当前播放时间
    public float SongDuration => (_audioSource.clip != null) ? _audioSource.clip.length : 0; // 歌曲总时长
    public float DisplayTime { get; private set; } // 用于UI显示和逻辑计算的“表面”时间，它可能与真实时间不同（例如在搓碟时）
    public bool IsTonearmOnRecord { get; private set; } = false; // 唱臂是否已放置在唱片上

    //======================================================================
    // 内部状态变量
    //======================================================================
    private AudioSource _audioSource;       // 音频播放组件的引用
    private bool _isDraggingSlider = false; // 标记用户是否正在拖动进度滑块
    private bool _isScratchingRecord = false;// 标记用户是否正在搓动唱片
    private float _lastSliderValue = 0f;    // 上一帧滑块的值，用于计算拖动速度

    private bool _isPausedByUser = false;   // 标记音乐是否被用户主动暂停（区别于因唱臂抬起等原因的暂停）

    // --- “角度解环”算法所需变量 ---
    private float _initialScratchTime;      // 开始搓碟时的时间点
    private float _lastFrameAngle;          // 上一帧的角度，用于计算角度增量
    private float _accumulatedAngle = 0f;   // 累积的角度变化量

    // --- UI文本模板 ---
    private List<string> _songTitleTemplates = new List<string>(); // 存储所有歌曲名文本框的原始模板，以保留样式

    /// <summary>
    /// Awake在所有Start函数之前被调用，用于初始化。
    /// </summary>
    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false; // 禁止自动播放
        _audioSource.loop = true;         // 开启循环播放
        if (_songProgressBar != null) { _songProgressBar.value = 0; }

        // 允许应用在后台运行时继续运行，防止切出应用后音乐停止
        Application.runInBackground = true;
    }

    /// <summary>
    /// Start在Awake之后、第一帧Update之前被调用。
    /// </summary>
    void Start()
    {
        // 阻止移动设备自动锁屏/息屏
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // 启动协程，从本地异步加载音乐和壁纸文件
        StartCoroutine(LoadFilesAndSetup());
    }

    /// <summary>
    /// 每帧被调用，处理核心的播放逻辑和UI更新。
    /// </summary>
    void Update()
    {
        if (_audioSource == null || _audioSource.clip == null) return; // 如果没有音频剪辑，则不执行任何操作

        IsPlaying = _audioSource.isPlaying;

        // --- 核心状态机：根据用户交互，决定时间的“权威来源” ---
        if (_isScratchingRecord)
        {
            // 状态一：正在搓转盘。此时DisplayTime由UpdateScratch()方法实时更新，此处无需操作。
        }
        else if (_isDraggingSlider)
        {
            // 状态二：正在拖动滑块。此时DisplayTime由滑块的UI值决定。
            DisplayTime = _songProgressBar.value * SongDuration;
        }
        else
        {
            // 状态三：正常播放或暂停。此时DisplayTime应与AudioSource的真实时间同步。
            DisplayTime = CurrentTime;
        }

        // 根据当前状态，集中处理对AudioSource的时间和音高修改
        HandleAudioEffects();

        // UI进度条的更新永远在最后，它只负责忠实地反映最终的DisplayTime
        if (!_isDraggingSlider && SongDuration > 0)
        {
            _songProgressBar.value = (SongDuration > 0) ? (DisplayTime / SongDuration) : 0;
        }
    }

    /// <summary>
    /// 辅助函数，集中处理所有对AudioSource的修改（音高、时间）。
    /// </summary>
    private void HandleAudioEffects()
    {
        if (_isScratchingRecord)
        {
            // 搓碟时，音高和时间由UpdateScratch()方法控制，这里无需处理。
        }
        else if (_isDraggingSlider)
        {
            // 拖动滑块时，根据滑块移动速度改变音高，并实时设置音频时间。
            float delta = _songProgressBar.value - _lastSliderValue;
            _audioSource.pitch = IsTonearmOnRecord ? (delta * _sliderScratchSensitivity) : 0f;
            _audioSource.time = Mathf.Clamp(DisplayTime, 0, SongDuration - 0.01f);
            _lastSliderValue = _songProgressBar.value;
        }
        else // 正常播放或暂停状态
        {
            // 恢复正常音高。
            _audioSource.pitch = 1f;
        }
    }

    // --- 公开的控制方法 (供其他脚本或UI事件调用) ---

    /// <summary>
    /// 播放音乐。
    /// </summary>
    public void PlayMusic()
    {
        if (_audioSource != null && !_audioSource.isPlaying)
        {
            _isPausedByUser = false; // 用户点击播放，清除“主动暂停”标志
            _audioSource.Play();
        }
    }

    /// <summary>
    /// 暂停音乐。
    /// </summary>
    public void PauseMusic()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _isPausedByUser = true; // 用户点击暂停，设置“主动暂停”标志
            _audioSource.Pause();
        }
    }

    // --- UI事件处理方法 ---

    /// <summary>
    /// 当鼠标/手指在进度条上按下时调用。
    /// </summary>
    public void OnSliderPointerDown()
    {
        if (IsTonearmOnRecord && !_audioSource.isPlaying) { PlayMusic(); } // 如果唱臂在位且未播放，则开始播放

        _isDraggingSlider = true;
        if (_songProgressBar != null) { _lastSliderValue = _songProgressBar.value; }
    }

    /// <summary>
    /// 当鼠标/手指在进度条上抬起时调用。
    /// </summary>
    public void OnSliderPointerUp() { _isDraggingSlider = false; }

    /// <summary>
    /// 当开始搓碟时调用，记录初始状态。
    /// </summary>
    /// <param name="initialAngle">开始搓碟时的初始角度。</param>
    public void StartScratching(float initialAngle)
    {
        _isScratchingRecord = true;
        _initialScratchTime = DisplayTime; // 从DisplayTime记录初始时间，确保与视觉同步
        _lastFrameAngle = initialAngle;
        _accumulatedAngle = 0f; // 重置累积角度
    }

    /// <summary>
    /// 当结束搓碟时调用。
    /// </summary>
    public void StopScratching() { _isScratchingRecord = false; }

    /// <summary>
    /// 在搓碟拖拽过程中持续调用，实时更新音乐状态。
    /// </summary>
    /// <param name="currentAngle">当前的角度。</param>
    public void UpdateScratch(float currentAngle)
    {
        if (!_isScratchingRecord) return;
        // 使用DeltaAngle安全地计算角度差，避免在0/360度附近跳变
        float frameAngleDelta = Mathf.DeltaAngle(currentAngle, _lastFrameAngle);
        _accumulatedAngle += frameAngleDelta; // 累积角度变化

        // 根据累积角度和灵敏度计算时间变化量
        float timeChange = _accumulatedAngle / _scratchDegreesPerSecond;

        // 计算并应用新的播放时间
        float newTime = _initialScratchTime + timeChange;
        DisplayTime = Mathf.Clamp(newTime, 0, SongDuration - 0.01f);
        _audioSource.time = DisplayTime;

        // 根据每帧的角度变化量来改变音高，模拟搓碟音效
        _audioSource.pitch = IsTonearmOnRecord ? (1.0f + (frameAngleDelta * _scratchPitchSensitivity * 0.1f)) : 0f;
        _lastFrameAngle = currentAngle; // 更新上一帧的角度
    }

    /// <summary>
    /// 切换播放/暂停状态的公开方法，专门给UI按钮调用。
    /// </summary>
    public void TogglePlayPause()
    {
        if (!IsTonearmOnRecord) return; // 唱臂未落下，不响应操作

        if (_audioSource.isPlaying) { PauseMusic(); }
        else { PlayMusic(); }
    }

    /// <summary>
    /// 设置唱臂状态（由唱臂控制器调用）。
    /// </summary>
    /// <param name="isOnRecord">唱臂是否落在唱片上。</param>
    public void SetTonearmState(bool isOnRecord)
    {
        IsTonearmOnRecord = isOnRecord;
        if (!isOnRecord) { PauseMusic(); } // 当唱臂抬起时，强制暂停音乐
    }

    /// <summary>
    /// 协程：异步加载本地的音乐和壁纸文件，并进行设置。
    /// </summary>
    private IEnumerator LoadFilesAndSetup()
    {
        // --- 步骤一：学习UI文本模板 ---
        // 如果模板列表是空的，就读取并存储所有文本框的初始内容作为模板。
        if (_songTitleTemplates.Count == 0 && _songTitleTexts != null)
        {
            foreach (TextMeshProUGUI titleText in _songTitleTexts)
            {
                if (titleText != null)
                {
                    // {0} 是一个占位符，将来会被实际的歌曲名替换
                    _songTitleTemplates.Add(titleText.text); 
                }
            }
        }

        // --- 步骤二：从PlayerPrefs读取文件路径 ---
        string musicPath = PlayerPrefs.GetString("SelectedMusicPath");
        string wallpaperPath = PlayerPrefs.GetString("SelectedWallpaperPath");

        // --- 步骤三：加载并应用壁纸 ---
        if (_cassetteWallpaperImages != null && _cassetteWallpaperImages.Count > 0 && !string.IsNullOrEmpty(wallpaperPath))
        {
            UnityWebRequest wallpaperRequest = UnityWebRequestTexture.GetTexture("file:///" + wallpaperPath);
            yield return wallpaperRequest.SendWebRequest();

            if (wallpaperRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D loadedTexture = DownloadHandlerTexture.GetContent(wallpaperRequest);
                Sprite wallpaperSprite = Sprite.Create(loadedTexture, new Rect(0, 0, loadedTexture.width, loadedTexture.height), new Vector2(0.5f, 0.5f));

                // 遍历列表，为所有指定的Image组件设置壁纸
                foreach (Image imageComponent in _cassetteWallpaperImages)
                {
                    if (imageComponent != null)
                    {
                        imageComponent.sprite = wallpaperSprite;
                    }
                }
            }
            else { Debug.LogError("加载壁纸失败: " + wallpaperRequest.error); }
        }

        // --- 步骤四：加载并设置音乐 ---
        if (_audioSource != null && !string.IsNullOrEmpty(musicPath))
        {
            // 根据文件扩展名，动态决定AudioType，增强兼容性
            AudioType audioType = AudioType.UNKNOWN;
            string extension = System.IO.Path.GetExtension(musicPath).ToLower();

            switch (extension)
            {
                case ".mp3": audioType = AudioType.MPEG; break;
                case ".ogg": audioType = AudioType.OGGVORBIS; break;
                case ".wav": audioType = AudioType.WAV; break;
                default:
                    Debug.LogError("不支持的音频格式: " + extension);
                    yield break; // 提前退出协程
            }

            // 使用动态判断出的audioType来创建请求
            UnityWebRequest musicRequest = UnityWebRequestMultimedia.GetAudioClip("file:///" + musicPath, audioType);
            yield return musicRequest.SendWebRequest();

            if (musicRequest.result == UnityWebRequest.Result.Success)
            {
                AudioClip loadedClip = DownloadHandlerAudioClip.GetContent(musicRequest);
                _audioSource.clip = loadedClip;

                // 从PlayerPrefs读取歌曲名并更新UI
                string songTitle = PlayerPrefs.GetString("SelectedSongTitle", "未知歌曲");

                // 使用之前储存的模板来更新UI文本
                if (_songTitleTexts != null)
                {
                    for (int i = 0; i < _songTitleTexts.Count; i++)
                    {
                        if (i < _songTitleTemplates.Count && _songTitleTexts[i] != null)
                        {
                            // 使用string.Format，它能完美保留模板中的所有样式，只替换"{0}"占位符
                            _songTitleTexts[i].text = string.Format(_songTitleTemplates[i], songTitle);
                        }
                    }
                }

                // 加载成功后，进入就绪暂停状态，等待用户操作
                PauseMusic(); 
            }
            else
            {
                Debug.LogError("加载音乐失败: " + musicRequest.error);
            }
        }
    }

    /// <summary>
    /// 返回到设置场景，并清除所有已选文件的状态，实现完全重置。
    /// </summary>
    public void GoToSetupScene()
    {
        // 1. 停止当前所有音频活动
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }

        // 2. 清除PlayerPrefs中保存的文件路径和歌曲名
        PlayerPrefs.DeleteKey("SelectedMusicPath");
        PlayerPrefs.DeleteKey("SelectedWallpaperPath");
        PlayerPrefs.DeleteKey("SelectedSongTitle");
        PlayerPrefs.Save(); // 确保删除操作被立即写入磁盘

        // 3. 加载设置场景 (请确保场景已添加到Build Settings中)
        SceneManager.LoadScene("SetupScene");
    }
}