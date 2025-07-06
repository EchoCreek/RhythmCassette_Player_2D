using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一个可绘制在Canvas上的、能程序化生成平滑弧形网格的UI组件。
/// 该组件负责渲染可视化器中的每一个独立小方块（弧形段）。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class CurvedVisualizerBlock : Graphic
{
    //======================================================================
    // 公开属性 (由外部控制器设置)
    // 这些参数定义了单个弧形块的几何形状。
    //======================================================================
    [Header("【几何形状参数】")]
    [Tooltip("弧形内边缘的半径")]
    public float InnerRadius = 50f;
    [Tooltip("弧形外边缘的半径")]
    public float OuterRadius = 100f;
    [Tooltip("弧形起始位置的角度（单位：度）")]
    public float StartAngle_Degrees = 0f;
    [Tooltip("弧形结束位置的角度（单位：度）")]
    public float EndAngle_Degrees = 30f;

    //======================================================================
    // 可在检视面板中配置的参数
    //======================================================================
    [Header("【渲染质量与颜色】")]
    [Tooltip("模拟曲线所用的线段数。值越高，弧形越平滑，但性能开销略高。")]
    [SerializeField, Range(1, 40)] private int _curveSegments = 10;

    [Tooltip("方块在非激活状态或作为渐变背景时的基础颜色。")]
    [SerializeField] private Color _baseColor = new Color(1f, 1f, 1f, 1f);

    //======================================================================
    // 内部私有变量
    //======================================================================
    private Color _activeColor = Color.cyan; // 存储方块在完全激活状态下的颜色

    /// <summary>
    /// Unity的UI重绘核心回调函数。
    /// 在此方法中，我们通过计算顶点和三角形来程序化地构建弧形网格。
    /// </summary>
    /// <param name="vh">一个用于辅助构建UI网格的工具对象。</param>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        // 1. 清空之前的所有顶点数据，为重绘做准备
        vh.Clear();
        if (_curveSegments < 1) _curveSegments = 1; // 保证至少有1段

        // 2. 计算每一小段弧形所占的角度
        float segmentAngleIncrement = (EndAngle_Degrees - StartAngle_Degrees) / _curveSegments;

        // 3. 循环创建每一个小段（四边形）
        for (int i = 0; i < _curveSegments; i++)
        {
            // 计算当前小段的起始和结束角度
            float currentStartAngle = StartAngle_Degrees + (i * segmentAngleIncrement);
            float currentEndAngle = StartAngle_Degrees + ((i + 1) * segmentAngleIncrement);

            // 将角度从度转换为弧度，以用于三角函数计算
            float startRad = currentStartAngle * Mathf.Deg2Rad;
            float endRad = currentEndAngle * Mathf.Deg2Rad;

            // 4. 计算构成当前小段四边形的四个顶点位置
            // v0: 内圈起始点, v1: 外圈起始点, v2: 外圈结束点, v3: 内圈结束点
            Vector2 v0 = new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * InnerRadius;
            Vector2 v1 = new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * OuterRadius;
            Vector2 v2 = new Vector2(Mathf.Cos(endRad), Mathf.Sin(endRad)) * OuterRadius;
            Vector2 v3 = new Vector2(Mathf.Cos(endRad), Mathf.Sin(endRad)) * InnerRadius;

            // 5. 将计算出的顶点添加到VertexHelper中
            UIVertex vert = UIVertex.simpleVert;
            vert.color = this.color; // 使用当前Graphic组件的颜色

            vert.position = v0; vh.AddVert(vert);
            vert.position = v1; vh.AddVert(vert);
            vert.position = v2; vh.AddVert(vert);
            vert.position = v3; vh.AddVert(vert);

            // 6. 将四个顶点组合成两个三角形，形成一个四边形
            int offset = i * 4;
            vh.AddTriangle(offset + 0, offset + 1, offset + 2);
            vh.AddTriangle(offset + 2, offset + 3, offset + 0);
        }
    }

    /// <summary>
    /// (由外部调用) 设置当方块被激活时应该显示的颜色。
    /// </summary>
    /// <param name="color">要设置的激活颜色。</param>
    public void SetActiveColor(Color color)
    {
        _activeColor = color;
    }

    /// <summary>
    /// (由外部调用) 设置方块的激活状态，并根据level值平滑改变其颜色。
    /// </summary>
    /// <param name="level">激活程度 (0.0 = 显示底色, 1.0 = 完全激活)</param>
    public void SetState(float level)
    {
        // 核心改动：在“底色”和“激活色”之间进行线性插值，以实现平滑的渐变效果
        this.color = Color.Lerp(_baseColor, _activeColor, level);
    }
}