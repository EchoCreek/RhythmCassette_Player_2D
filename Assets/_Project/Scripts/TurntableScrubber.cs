using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 负责处理对转盘的直接拖拽交互（最终绝对位置映射版）。
/// </summary>
public class TurntableScrubber : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Tooltip("音乐总控制器")]
    [SerializeField] private MusicPlayerController _musicPlayerController;

    [Tooltip("旋转的中心点，通常是其自身的RectTransform")]
    [SerializeField] private RectTransform _turntableTransform;
    
    // 【移除】不再需要平滑，因为我们不再依赖帧间delta
    // [SerializeField, Range(1f, 50f)] private float _angleSmoothingSpeed = 15f;

    private bool _isScratching = false;
    private Canvas _canvas;
    private Camera _uiCamera;

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            _uiCamera = _canvas.worldCamera;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isScratching = true;
        // 【修正】将按下的初始角度传递给总控制器
        _musicPlayerController.StartScratching(GetAngle(eventData));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isScratching)
        {
            // 【修正】在拖拽的每一帧，都将当前角度传递给总控制器
            _musicPlayerController.UpdateScratch(GetAngle(eventData));
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isScratching = false;
        _musicPlayerController.StopScratching();
    }

    private float GetAngle(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_turntableTransform, eventData.position, _uiCamera, out Vector2 localPoint);
        return Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
    }
}