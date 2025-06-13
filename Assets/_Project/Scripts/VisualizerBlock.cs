using UnityEngine;
using UnityEngine.UI;

public class VisualizerBlock : MonoBehaviour
{
    private Image _image;
    private Color _activeColor; // 这个方块被激活时应该显示的颜色
    private readonly Color _inactiveColor = Color.white; // 未激活时显示白色

    // Awake在Instantiate（实例化）时就会被调用
    void Awake()
    {
        // 获取自身的Image组件，方便后续操作
        _image = GetComponent<Image>();
    }

    // 一个公共方法，让外部控制器可以设置这个方块的激活颜色
    public void SetActiveColor(Color color)
    {
        _activeColor = color;
    }

    // 一个公共方法，控制是显示激活颜色还是白色
    public void SetActive(bool isActive)
    {
        // 如果isActive为true，就显示激活颜色，否则显示白色
        _image.color = isActive ? _activeColor : _inactiveColor;
    }
}