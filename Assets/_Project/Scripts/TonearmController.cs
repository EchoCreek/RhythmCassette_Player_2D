using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 控制唱臂的交互逻辑。
/// 负责处理拖拽、限制旋转角度，并在到达特定角度时触发音乐的播放/暂停。
/// </summary>
public class TonearmController : MonoBehaviour, IDragHandler, IPointerUpHandler
{
    [Header("核心引用")]
    [Tooltip("需要被旋转的那个唱臂对象的RectTransform")]
    [SerializeField] private RectTransform _tonearmTransform;

    [Tooltip("音乐总控制器")]
    [SerializeField] private MusicPlayerController _musicPlayerController;

    [Header("旋转设置")]
    [Tooltip("拖拽的灵敏度。值越大，轻微拖拽就能旋转更大角度。")]
    [SerializeField] private float _dragSensitivity = 0.5f;

    [Tooltip("唱臂在静止位置时的旋转角度(Z值)")]
    [SerializeField] private float _minAngle = 20f;

    [Tooltip("唱臂在播放位置时的旋转角度(Z值)")]
    [SerializeField] private float _maxAngle = -15f;

    // --- 内部状态变量 ---
    private bool _isOnRecord = false; // 用于追踪唱臂当前是否在播放区域，防止重复发送指令

    void Start()
    {
        // 游戏开始时，确保唱臂在静止位置
        if (_tonearmTransform != null)
        {
            _tonearmTransform.localRotation = Quaternion.Euler(0, 0, _minAngle);
        }
        // 检查初始状态
        CheckPlayState();
    }

    /// <summary>
    /// 当用户在此UI上拖拽时，每一帧都会调用
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_tonearmTransform == null) return;

        // 1. 根据水平拖拽的距离，计算角度变化量
        float angleChange = eventData.delta.x * _dragSensitivity;

        // 2. 计算出新的Z轴旋转角度
        // 我们需要先获取当前的角度，注意要处理好超过360度的问题
        float currentAngle = _tonearmTransform.localEulerAngles.z;
        // 将角度转换到-180到180的范围，方便比较
        if (currentAngle > 180) currentAngle -= 360; 
        
        float newAngle = currentAngle + angleChange;
        
        // 3. 将新角度限制在最小和最大值之间
        newAngle = Mathf.Clamp(newAngle, _maxAngle, _minAngle); // 注意min和max的位置

        // 4. 应用新的旋转
        _tonearmTransform.localRotation = Quaternion.Euler(0, 0, newAngle);

        // 5. 检查是否需要改变播放状态
        CheckPlayState();
    }

    /// <summary>
    /// 当用户松开手时调用，再次检查状态
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        CheckPlayState();
    }

    /// <summary>
    /// 检查当前唱臂角度，并根据情况命令总控制器播放或暂停
    /// </summary>
    private void CheckPlayState()
    {
        if (_musicPlayerController == null || _tonearmTransform == null) return;
        
        float currentAngle = _tonearmTransform.localEulerAngles.z;
        if (currentAngle > 180) currentAngle -= 360;

        bool isNowOnRecord = Mathf.Approximately(currentAngle, _maxAngle);
        bool isNowOffRecord = Mathf.Approximately(currentAngle, _minAngle);

        // 如果【刚刚】移动到播放位置
        if (isNowOnRecord && !_isOnRecord)
        {
            _isOnRecord = true;
            // 【核心修正】汇报状态，并命令播放
            _musicPlayerController.SetTonearmState(true);
            _musicPlayerController.PlayMusic();
        }
        // 如果【刚刚】移动到静止位置
        else if (isNowOffRecord && _isOnRecord)
        {
            _isOnRecord = false;
            // 【核心修正】命令暂停，并汇报状态
            _musicPlayerController.PauseMusic();
            _musicPlayerController.SetTonearmState(false);
        }
    }
}