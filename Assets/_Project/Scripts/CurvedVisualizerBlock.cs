using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一个可绘制在Canvas上的、能程序化生成平滑弧形网格的UI组件。
/// </summary>
public class CurvedVisualizerBlock : Graphic
{
    // --- 由总控制器在运行时设置的公开属性 ---
    public float InnerRadius = 50f;
    public float OuterRadius = 100f;
    public float StartAngle_Degrees = 0f;
    public float EndAngle_Degrees = 30f;
    
    // --- 可以在Prefab上预设的属性 ---
    [Tooltip("模拟曲线所用的线段数。值越高，弧形越平滑，但性能开销略高。")]
    [SerializeField, Range(1, 40)] private int _curveSegments = 10;
    
    // --- 内部私有变量 ---
    private Color _activeColor = Color.cyan;
    private readonly Color _inactiveColor = Color.white;

    /// <summary>
    /// Unity的UI重绘回调函数，我们在这里定义形状的顶点。
    /// </summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_curveSegments < 1) _curveSegments = 1;
        float segmentAngleIncrement = (EndAngle_Degrees - StartAngle_Degrees) / _curveSegments;
        for (int i = 0; i < _curveSegments; i++)
        {
            float currentStartAngle = StartAngle_Degrees + (i * segmentAngleIncrement);
            float currentEndAngle = StartAngle_Degrees + ((i + 1) * segmentAngleIncrement);
            float startRad = currentStartAngle * Mathf.Deg2Rad;
            float endRad = currentEndAngle * Mathf.Deg2Rad;
            Vector2 v0 = new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * InnerRadius;
            Vector2 v1 = new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * OuterRadius;
            Vector2 v2 = new Vector2(Mathf.Cos(endRad), Mathf.Sin(endRad)) * OuterRadius;
            Vector2 v3 = new Vector2(Mathf.Cos(endRad), Mathf.Sin(endRad)) * InnerRadius;
            vh.AddUIVertexQuad(new UIVertex[] {
                new UIVertex { position = v0, color = this.color },
                new UIVertex { position = v1, color = this.color },
                new UIVertex { position = v2, color = this.color },
                new UIVertex { position = v3, color = this.color }
            });
        }
    }
    
    /// <summary>
    /// 设置当方块被激活时应该显示的颜色。
    /// </summary>
    public void SetActiveColor(Color color) 
    {
        _activeColor = color;
    }

    /// <summary>
    /// 设置方块的激活状态，并改变其颜色。
    /// </summary>
    public void SetActive(bool isActive) 
    {
        // Graphic类自带一个color属性，我们直接修改它来改变整个网格的颜色
        this.color = isActive ? _activeColor : _inactiveColor;
    }

    // 这两个方法在新版代码中没有被直接调用，但保留着是好习惯
    public Color GetActiveColor() { return _activeColor; }
    public Color GetInactiveColor() { return _inactiveColor; }
}