using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // 【新增】用于加载本地文件
using System.Collections;   // 【新增】用于使用协程
using System.Collections.Generic;
using TMPro; // 【新增】引入TextMeshPro的命名空间
using UnityEngine.SceneManagement; // 【新增】引入场景管理命名空间

/// <summary>
/// 音乐播放的总控制器（最终版 V4 - 角度解环）。
/// 采用“角度累积”算法，彻底解决了在处理360度环绕输入时的“跳变”和“抽搐”问题。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicPlayerController : MonoBehaviour
{
    [Header("核心引用")]
    [SerializeField] private Slider _songProgressBar;

    [Tooltip("需要同步更换壁纸的所有Image组件列表")]
    [SerializeField] private List<Image> _cassetteWallpaperImages; // 【修正】将单个Image改为Image列表

    [Tooltip("需要同步显示歌曲名称的所有【普通Text】组件列表")]
    [SerializeField] private List<TextMeshProUGUI> _songTitleTexts;

    [Header("搓碟效果设置")]
    [Tooltip("滑块搓碟效果的灵敏度。")]
    [SerializeField] private float _sliderScratchSensitivity = 40f;

    [Tooltip("转盘搓碟时，旋转多少度对应音乐时间变化一秒。负值代表反向。")]
    [SerializeField] private float _scratchDegreesPerSecond = -90f;

    [Tooltip("转盘搓碟时，音高变化的灵敏度。")]
    [SerializeField] private float _scratchPitchSensitivity = 3f;


    // --- 公开状态属性 ---
    public bool IsPlaying { get; private set; }
    public float CurrentTime => _audioSource.time;
    public float SongDuration => (_audioSource.clip != null) ? _audioSource.clip.length : 0;
    public float DisplayTime { get; private set; }
    public bool IsTonearmOnRecord { get; private set; } = false; // 默认为false

    // --- 内部状态变量 ---
    private AudioSource _audioSource;
    private bool _isDraggingSlider = false;
    private bool _isScratchingRecord = false;
    private float _lastSliderValue = 0f;

    // 【新增】用于标记音乐是否被用户主动暂停
    private bool _isPausedByUser = false;

    // 【核心修正】用于“角度解环”算法的变量
    private float _initialScratchTime;
    private float _lastFrameAngle;
    private float _accumulatedAngle = 0f;
    // 【新增】用于存储所有歌曲名文本框的原始模板
    private List<string> _songTitleTemplates = new List<string>();

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
        if (_songProgressBar != null) { _songProgressBar.value = 0; }

        // 【新增】允许应用在后台运行时继续运行
        Application.runInBackground = true;
    }

    void Start()
    {
        //阻止安卓端的息屏操作
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // 由唱臂控制初始播放，但我们在这里调用一次Pause，
        // 让AudioSource从“停止”进入“已暂停”状态，以解决首次寻址问题。
        // PauseMusic();

        // 不再自动播放，而是启动加载协程
        StartCoroutine(LoadFilesAndSetup());
    }
    void Update()
    {
        if (_audioSource == null || _audioSource.clip == null) return;

        IsPlaying = _audioSource.isPlaying;

        // 核心重构：根据用户交互状态，决定DisplayTime的“权威来源”
        if (_isScratchingRecord)
        {
            // 状态一：正在搓转盘。此时DisplayTime由Scratch()方法实时更新。
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

        // 根据当前状态，决定是否需要同步AudioSource的时间和音高
        HandleAudioEffects();

        // UI进度条的更新永远在最后，它只负责忠实地反映最终的DisplayTime
        if (!_isDraggingSlider && SongDuration > 0)
        {
            _songProgressBar.value = (SongDuration > 0) ? (DisplayTime / SongDuration) : 0;
        }

    }

    /// <summary>
    /// 一个新的辅助函数，集中处理所有对AudioSource的修改
    /// </summary>
    private void HandleAudioEffects()
    {
        if (_isScratchingRecord)
        {
            // 搓碟时，音高由Scratch()方法控制
        }
        else if (_isDraggingSlider)
        {
            // 拖动滑块时，音高和时间由滑块的移动决定
            float delta = _songProgressBar.value - _lastSliderValue;
            _audioSource.pitch = IsTonearmOnRecord ? (delta * _sliderScratchSensitivity) : 0f;
            _audioSource.time = Mathf.Clamp(DisplayTime, 0, SongDuration - 0.01f);
            _lastSliderValue = _songProgressBar.value;
        }
        else // 正常播放或暂停状态
        {
            _audioSource.pitch = 1f;
        }
    }

    private void HandleSliderScratching()
    {
        float currentValue = _songProgressBar.value;
        DisplayTime = currentValue * SongDuration;
        _audioSource.time = Mathf.Clamp(DisplayTime, 0, SongDuration - 0.01f);
        float delta = currentValue - _lastSliderValue;
        // 【修正】只有唱臂在位时，拖拽滑块才有刮擦音效
        _audioSource.pitch = IsTonearmOnRecord ? (delta * _sliderScratchSensitivity) : 0f;
        _lastSliderValue = currentValue;
    }

    private void HandleNormalPlayback()
    {
        // 如果应该自动播放，则播放
        if (!_audioSource.isPlaying && !_isPausedByUser && IsTonearmOnRecord)
        {
            PlayMusic();
        }

        _audioSource.pitch = 1f;

        // 【核心修正】
        // 只有在音乐【没有】被用户主动暂停时，才让视觉时间跟随音频的真实时间。
        // 如果是用户暂停的（或游戏刚启动时），就让DisplayTime保持在它被暂停或寻址到的位置。
        if (!_isPausedByUser)
        {
            DisplayTime = CurrentTime;
        }
    }
    // --- 公开的控制方法 ---
    public void PlayMusic()
    {
        if (_audioSource != null && !_audioSource.isPlaying)
        {
            _isPausedByUser = false; // 用户点击播放，清除“主动暂停”标志
            _audioSource.Play();
        }
    }
    public void PauseMusic()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _isPausedByUser = true; // 用户点击暂停，设置“主动暂停”标志
            _audioSource.Pause();
        }
    }

    // --- 事件处理方法 ---
    public void OnSliderPointerDown()
    {
        // 【修正】只在唱臂在位时，这个交互才会顺带触发播放
        if (IsTonearmOnRecord && !_audioSource.isPlaying) { PlayMusic(); }

        _isDraggingSlider = true;
        if (_songProgressBar != null) { _lastSliderValue = _songProgressBar.value; }
    }
    public void OnSliderPointerUp() { _isDraggingSlider = false; }

    // 当开始搓碟时，记录锚点信息
    public void StartScratching(float initialAngle)
    {
        _isScratchingRecord = true;
        _initialScratchTime = DisplayTime; // 【核心修正】从DisplayTime记录初始时间
        _lastFrameAngle = initialAngle;
        _accumulatedAngle = 0f;
    }

    public void StopScratching() { _isScratchingRecord = false; }

    // 在拖拽过程中，累积角度并计算时间和音高
    public void UpdateScratch(float currentAngle)
    {
        if (!_isScratchingRecord) return;
        float frameAngleDelta = Mathf.DeltaAngle(currentAngle, _lastFrameAngle);
        _accumulatedAngle += frameAngleDelta;
        float timeChange = _accumulatedAngle / _scratchDegreesPerSecond;

        // 所有时间计算都基于DisplayTime
        float newTime = _initialScratchTime + timeChange;
        DisplayTime = Mathf.Clamp(newTime, 0, SongDuration - 0.01f);
        _audioSource.time = DisplayTime;

        _audioSource.pitch = IsTonearmOnRecord ? (1.0f + (frameAngleDelta * _scratchPitchSensitivity * 0.1f)) : 0f;
        _lastFrameAngle = currentAngle;
    }

    /// <summary>
    /// 【新增】切换播放/暂停状态的公开方法，专门给UI按钮调用。
    /// </summary>
    public void TogglePlayPause()
    {
        if (!IsTonearmOnRecord) return;

        // 直接使用AudioSource的状态来判断，不再依赖我们自己的IsPlaying
        if (_audioSource.isPlaying) { PauseMusic(); }
        else { PlayMusic(); }
    }

    public void SetTonearmState(bool isOnRecord)
    {
        IsTonearmOnRecord = isOnRecord;
        // 当唱臂抬起时，强制暂停音乐
        if (!isOnRecord) { PauseMusic(); }
    }

    private IEnumerator LoadFilesAndSetup()
    {

        // 【第一步：学习模板】
        // 在最开始，如果模板列表是空的，就读取并存储所有文本框的初始内容作为模板。
        if (_songTitleTemplates.Count == 0 && _songTitleTexts != null)
        {
            foreach (TextMeshProUGUI titleText in _songTitleTexts)
            {
                if (titleText != null)
                {
                    _songTitleTemplates.Add(titleText.text);
                }
            }
        }

        // 从PlayerPrefs读取路径
        string musicPath = PlayerPrefs.GetString("SelectedMusicPath");
        string wallpaperPath = PlayerPrefs.GetString("SelectedWallpaperPath");

        // --- 加载壁纸 ---
        if (_cassetteWallpaperImages != null && _cassetteWallpaperImages.Count > 0 && !string.IsNullOrEmpty(wallpaperPath))
        {
            UnityWebRequest wallpaperRequest = UnityWebRequestTexture.GetTexture("file:///" + wallpaperPath);
            yield return wallpaperRequest.SendWebRequest();

            if (wallpaperRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D loadedTexture = DownloadHandlerTexture.GetContent(wallpaperRequest);
                Sprite wallpaperSprite = Sprite.Create(loadedTexture, new Rect(0, 0, loadedTexture.width, loadedTexture.height), new Vector2(0.5f, 0.5f));

                // 【核心修正】遍历列表，为所有Image组件设置Sprite
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

        // --- 加载音乐 (这是修改的核心) ---
        if (_audioSource != null && !string.IsNullOrEmpty(musicPath))
        {
            // 【核心修正】根据文件扩展名，动态决定AudioType
            AudioType audioType = AudioType.UNKNOWN;
            string extension = System.IO.Path.GetExtension(musicPath).ToLower();

            switch (extension)
            {
                case ".mp3":
                    audioType = AudioType.MPEG;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                // 你可以根据需要添加更多格式，如 aiff, mod, etc.
                default:
                    Debug.LogError("不支持的音频格式: " + extension);
                    yield break; // 提前退出协程
            }

            // 使用我们动态判断出的audioType来创建请求
            UnityWebRequest musicRequest = UnityWebRequestMultimedia.GetAudioClip("file:///" + musicPath, audioType);
            yield return musicRequest.SendWebRequest();

            if (musicRequest.result == UnityWebRequest.Result.Success)
            {
                AudioClip loadedClip = DownloadHandlerAudioClip.GetContent(musicRequest);
                _audioSource.clip = loadedClip;

                // 【新增】从PlayerPrefs读取歌曲名并更新UI
                string songTitle = PlayerPrefs.GetString("SelectedSongTitle", "未知歌曲");

                // 【第二步：填充模板】
                // 使用我们储存的模板来更新UI
                if (_songTitleTexts != null)
                {
                    for (int i = 0; i < _songTitleTexts.Count; i++)
                    {
                        if (i < _songTitleTemplates.Count && _songTitleTexts[i] != null)
                        {
                            // 【核心修正】使用string.Format，它能完美保留模板中的所有样式
                            _songTitleTexts[i].text = string.Format(_songTitleTemplates[i], songTitle);
                        }
                    }
                }

                // 加载成功后，进入就绪暂停状态
                PauseMusic();
            }
            else
            {
                Debug.LogError("加载音乐失败: " + musicRequest.error);
            }
        }
    }

    /// <summary>
    /// 【新增】返回到设置场景，并清除所有已选文件的状态，实现完全重置。
    /// </summary>
    public void GoToSetupScene()
    {
        // 1. 停止当前所有音频活动，以防万一
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }

        // 2. 【核心】清除PlayerPrefs中保存的文件路径和歌曲名
        PlayerPrefs.DeleteKey("SelectedMusicPath");
        PlayerPrefs.DeleteKey("SelectedWallpaperPath");
        PlayerPrefs.DeleteKey("SelectedSongTitle");
        PlayerPrefs.Save(); // 确保删除操作被写入磁盘

        // 3. 加载设置场景
        // 请确保你的设置场景文件名就是 "SetupScene"
        SceneManager.LoadScene("SetupScene");
    }


}