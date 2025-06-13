using UnityEngine;
using UnityEngine.EventSystems; // 引入事件系统的命名空间

/// <summary>
/// 一个模块化的音量旋钮控制器。
/// 负责处理拖拽手势，并同步更新系统音量和旋钮的视觉旋转。
/// </summary>
public class VolumeKnob : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Header("旋钮视觉设置")]
    [Tooltip("需要被旋转的那个旋钮对象的RectTransform")]
    [SerializeField] private RectTransform _knobToRotate;

    [Tooltip("旋钮可旋转的最小角度（对应音量为0时）")]
    [SerializeField] private float _minRotationAngle = -135f;

    [Tooltip("旋钮可旋转的最大角度（对应音量为1时）")]
    [SerializeField] private float _maxRotationAngle = 135f;

    [Header("灵敏度设置")]
    [Tooltip("手指垂直拖动多少像素，对应音量变化0.01（即1%）。值越小越灵敏。")]
    [SerializeField] private float _pixelsPerVolumeStep = 5f;

    void Start()
    {
        // 游戏开始时，根据当前的系统音量，设置旋钮的初始角度
        UpdateKnobRotation(AudioListener.volume);
    }

    /// <summary>
    /// 当拖拽开始时，由EventSystem调用（我们目前不需要在这里做什么）
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 此处可以留空，或者用于播放“开始调节”的音效等
    }

    /// <summary>
    /// 当用户在此UI上拖拽时，每一帧都会调用
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_knobToRotate == null) return;

        // 1. 计算音量变化
        // eventData.delta.y 是手指从上一帧到这一帧在垂直方向上移动的像素距离
        float volumeChange = eventData.delta.y / _pixelsPerVolumeStep / 100f;

        // 2. 计算并约束新音量
        float newVolume = AudioListener.volume + volumeChange;
        newVolume = Mathf.Clamp01(newVolume); // 确保音量在0到1之间

        // 3. 应用新音量
        AudioListener.volume = newVolume;

        // 4. 根据新的音量值，更新旋钮的视觉旋转
        UpdateKnobRotation(newVolume);
    }

    /// <summary>
    /// 一个辅助方法，根据当前的音量(0-1)，来设置旋钮的角度
    /// </summary>
    private void UpdateKnobRotation(float currentVolume)
    {
        // 使用Lerp（线性插值），将音量的[0, 1]范围映射到旋转角度的[min, max]范围
        float newRotationZ = Mathf.Lerp(_minRotationAngle, _maxRotationAngle, currentVolume);
        
        _knobToRotate.localRotation = Quaternion.Euler(0, 0, newRotationZ);
    }
}