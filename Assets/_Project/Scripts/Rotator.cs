using UnityEngine;

/// <summary>
/// 一个通用的旋转组件。
/// 它会根据MusicPlayerController提供的DisplayTime来旋转它所在的GameObject。
/// </summary>
public class Rotator : MonoBehaviour
{
    [Tooltip("每秒旋转的角度。负值为顺时针。")]
    [SerializeField] private float _degreesPerSecond = -15f; 

    [Tooltip("音乐总控制器")]
    [SerializeField] private MusicPlayerController _musicPlayerController;

    void Update()
    {
        if (_musicPlayerController == null)
        {
            // 为了防止在没有设置引用时报错，增加一个安全返回
            return;
        }

        // 核心逻辑：旋转角度直接由DisplayTime决定
        float targetZRotation = _musicPlayerController.DisplayTime * _degreesPerSecond;

        // 使用localRotation，确保旋转是相对于其父物体的
        transform.localRotation = Quaternion.Euler(0, 0, targetZRotation);
    }
}