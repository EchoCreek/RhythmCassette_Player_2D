using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 引入场景管理命名空间
using TMPro; // 如果你使用TextMeshPro来显示路径的话
using SFB; // 引入StandaloneFileBrowser的命名空间

/// <summary>
/// 负责处理设置场景中的UI交互和文件选择。
/// </summary>
public class SetupSceneController : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Button _playButton;

    [Tooltip("“选择音乐”按钮上的TextMeshPro文本组件")]
    [SerializeField] private TextMeshProUGUI _selectMusicButtonText;

    [Tooltip("“选择壁纸”按钮上的TextMeshPro文本组件")]
    [SerializeField] private TextMeshProUGUI _selectWallpaperButtonText;

    private string _selectedMusicPath;
    private string _selectedWallpaperPath;

    void Start()
    {
        // 游戏开始时，禁用播放按钮，直到两个文件都已选择
        UpdatePlayButtonState();
    }

    /// <summary>
    /// “选择音乐”按钮的点击事件处理函数
    /// </summary>
    public void OnSelectMusicButtonClicked()
    {
#if UNITY_STANDALONE
    var extensions = new[] {
        new ExtensionFilter("音频文件", "mp3", "wav", "ogg")
    };
    var paths = StandaloneFileBrowser.OpenFilePanel("选择音乐文件", "", extensions, false);
    if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
    {
        HandleFileSelected(paths[0], "music");
    }
#elif UNITY_ANDROID
        // 这是只在安卓上执行的代码
        // 使用 NativeFilePicker 插件
        NativeFilePicker.PickFile((path) =>
        {
            if (path != null)
            {
                HandleFileSelected(path, "music");
            }
        }, "audio/*"); // "audio/*" 是MIME类型，告诉系统我们要选择音频
#endif
    }

    /// <summary>
    /// “选择壁纸”按钮的点击事件处理函数
    /// </summary>
    public void OnSelectWallpaperButtonClicked()
    {
#if UNITY_STANDALONE
    var extensions = new[] {
        new ExtensionFilter("图片文件", "png", "jpg", "jpeg")
    };
    var paths = StandaloneFileBrowser.OpenFilePanel("选择壁纸图片", "", extensions, false);
    if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
    {
        HandleFileSelected(paths[0], "wallpaper");
    }
#elif UNITY_ANDROID
        // 这是只在安卓上执行的代码
        NativeFilePicker.PickFile((path) =>
        {
            if (path != null)
            {
                HandleFileSelected(path, "wallpaper");
            }
        }, "image/*"); // "image/*" 是MIME类型，告诉系统我们要选择图片
#endif
    }

    /// <summary>
    /// “播放”按钮的点击事件处理函数
    /// </summary>
    public void OnPlayButtonClicked()
    {
        // 【新增守卫】在执行任何操作前，先检查两个文件路径是否都有效
        if (string.IsNullOrEmpty(_selectedMusicPath) || string.IsNullOrEmpty(_selectedWallpaperPath))
        {
            Debug.Log("音乐和壁纸都需要选择后才能播放！"); // 在控制台给一个提示
            return; // 如果有任何一个没选，就直接退出此方法，不执行后续代码
        }

        // --- 只有在两个文件都已选择时，以下代码才会被执行 ---
        PlayerPrefs.SetString("SelectedMusicPath", _selectedMusicPath);
        PlayerPrefs.SetString("SelectedWallpaperPath", _selectedWallpaperPath);
        
        string songTitle = System.IO.Path.GetFileNameWithoutExtension(_selectedMusicPath);
        PlayerPrefs.SetString("SelectedSongTitle", songTitle);

        PlayerPrefs.Save(); 
        SceneManager.LoadScene("PlayerScene");
    }

    /// <summary>
    /// 检查是否两个文件都已选择，并更新播放按钮的可交互状态
    /// </summary>
    private void UpdatePlayButtonState()
    {
        // if (_playButton != null)
        // {
        //     _playButton.interactable = !string.IsNullOrEmpty(_selectedMusicPath) && !string.IsNullOrEmpty(_selectedWallpaperPath);
        // }
    }

    private void HandleFileSelected(string path, string type)
    {
        if (type == "music")
        {
            _selectedMusicPath = path;
            // 【修正】提取不带后缀的文件名，并用它来更新按钮文字
            string songTitle = System.IO.Path.GetFileNameWithoutExtension(path);
            if (_selectMusicButtonText != null) 
            {
                // 让按钮直接显示歌曲名，比“已选择”更直观
                _selectMusicButtonText.text = "音乐:" + songTitle; 
            }
            Debug.Log("选择了音乐: " + _selectedMusicPath);
        }
        else if (type == "wallpaper")
        {
            _selectedWallpaperPath = path;
            // 【修正】提取不带后缀的文件名，并用它来更新按钮文字
            string songTitle = System.IO.Path.GetFileNameWithoutExtension(path);
            if (_selectWallpaperButtonText != null)
            {
                _selectWallpaperButtonText.text = "壁纸:" + songTitle;
            }
            Debug.Log("选择了壁纸: " + _selectedWallpaperPath);
        }
        UpdatePlayButtonState();
    }
}