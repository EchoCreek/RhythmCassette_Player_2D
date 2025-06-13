using UnityEngine;

/// <summary>
/// 环形音频可视化工具的总控制器（最终完整注释版）。
/// 实现了可手动调整的静态布局、以及可自动或手动控制的、带有独立Attack/Release和动态基底的动画逻辑。
/// </summary>
public class CircularAudioVisualizer : MonoBehaviour
{
    // =================================================================================================
    // 1. 在Inspector中设置的参数
    // =================================================================================================

    [Header("【核心设置】")]
    [Tooltip("提供音频数据的Audio Source组件")]
    [SerializeField] private AudioSource _audioSource;

    [Tooltip("单个可视化方块的Prefab（需挂载CurvedVisualizerBlock脚本）")]
    [SerializeField] private GameObject _blockPrefab; 

    [Header("【视觉设置】")]
    [Tooltip("每一列方块在激活时对应的颜色。数组大小建议与列数匹配。")]
    [SerializeField] private Color[] _columnColors = new Color[12];

    [Tooltip("可视化的列数。")]
    [SerializeField, Range(1, 100)] private int _numberOfColumns = 12;

    [Tooltip("每一列包含的方块数量（即圈数）。")]
    [SerializeField, Range(1, 20)] private int _blocksPerColumn = 4;
    
    [Header("【布局设置】")]
    [Tooltip("最内圈距离中心的半径。")]
    [SerializeField] private float _innerRadius = 80f;

    [Tooltip("每一圈之间的距离。")]
    [SerializeField] private float _radiusStep = 50f;

    [Tooltip("所有方块统一的厚度（在径向上的长度）。")]
    [SerializeField] private float _barHeight = 40f;
    
    [Tooltip("分别控制每一圈的横向紧凑度(0-1)，数组大小应与圈数(Blocks Per Column)相等。值越接近1，该圈方块间的横向缝隙越小。")]
    [SerializeField] private float[] _rowAngularFactors = { 1f, 0.95f, 0.9f, 0.85f };

    [Header("【动画设置】")]
    [Tooltip("勾选后，将启用自动灵敏度调节；取消勾选，则使用下方的手动灵敏度。")]
    [SerializeField] private bool _useAutoSensitivity = true;

    [Tooltip("【手动模式】当禁用自动灵敏度时，手动控制律动的整体幅度。相当于一个总音量增益。")]
    [SerializeField] private float _manualSensitivity = 1.0f;

    [Tooltip("【自动模式】“峰值记忆”衰减的速度。\n值越大，峰值忘得越快，对音乐短期安静段落的反应更灵敏；\n值越小，记忆越长久，对整首歌的响度更稳定。")]
    [SerializeField] private float _decaySpeed = 0.5f;

    [Tooltip("控制方块“亮起”的响应速度。值越大，对节拍的响应越快、越硬朗。")]
    [SerializeField, Range(1f, 50f)] private float _attackSpeed = 25f;

    [Tooltip("控制方块“暗淡”的消退速度，即存留时间。值越小，存留时间越长，消退得越慢，“余晖”效果越明显。")]
    [SerializeField, Range(0.1f, 20f)] private float _releaseSpeed = 1.5f;

    [Tooltip("控制“呼吸”基底的灵敏度。值为0则无基底。值越高，在音乐安静时，整体的脉动越明显。")]
    [SerializeField, Range(0f, 10f)] private float _baselineSensitivity = 0.5f;


    // =================================================================================================
    // 2. 内部私有变量
    // =================================================================================================

    private CurvedVisualizerBlock[][] _visualizerBlocks;
    private float[] _spectrumData;
    private float[] _smoothedIntensities; 
    private float _maxObservedIntensity = 0.001f;


    // =================================================================================================
    // 3. 核心功能函数
    // =================================================================================================

    /// <summary>
    /// 在编辑器中通过右键菜单手动调用，用于生成或刷新布局。
    /// </summary>
    [ContextMenu("Generate Layout")]
    void GenerateLayout()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) { DestroyImmediate(transform.GetChild(i).gameObject); }
        if (_blockPrefab == null) { Debug.LogError("Block Prefab is not set!"); return; }

        _visualizerBlocks = new CurvedVisualizerBlock[_numberOfColumns][];
        float angleIncrement = 360f / _numberOfColumns;

        for (int i = 0; i < _numberOfColumns; i++)
        {
            _visualizerBlocks[i] = new CurvedVisualizerBlock[_blocksPerColumn];
            float columnStartAngle = i * angleIncrement;

            for (int j = 0; j < _blocksPerColumn; j++)
            {
                float blockInnerRadius = _innerRadius + (j * _radiusStep);
                float blockOuterRadius = blockInnerRadius + _barHeight;
                float factor = (j < _rowAngularFactors.Length) ? _rowAngularFactors[j] : 1f;
                float angularWidth = angleIncrement * factor;
                float anglePadding = (angleIncrement - angularWidth) / 2f;
                float blockStartAngle = columnStartAngle + anglePadding;
                float blockEndAngle = blockStartAngle + angularWidth;
                GameObject blockObj = Instantiate(_blockPrefab, transform);
                CurvedVisualizerBlock blockScript = blockObj.GetComponent<CurvedVisualizerBlock>();
                
                blockScript.InnerRadius = blockInnerRadius;
                blockScript.OuterRadius = blockOuterRadius;
                blockScript.StartAngle_Degrees = blockStartAngle;
                blockScript.EndAngle_Degrees = blockEndAngle;

                if (i < _columnColors.Length) { blockScript.SetActiveColor(_columnColors[i]); }
                _visualizerBlocks[i][j] = blockScript;
            }
        }
    }
    
    /// <summary>
    /// Unity生命周期函数：在游戏开始第一帧前调用
    /// </summary>
    void Start()
    {
        GenerateLayout(); 
        _spectrumData = new float[512];
        _smoothedIntensities = new float[_numberOfColumns];
    }
    
    /// <summary>
    /// Unity生命周期函数：每一帧都会调用
    /// </summary>
    void Update()
    {
        if (!Application.isPlaying || _audioSource == null) return;
        
        _audioSource.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris);

        float averageVolume = 0f;
        foreach (float s in _spectrumData) { averageVolume += s; }
        averageVolume /= _spectrumData.Length;
        float baselineIntensity = averageVolume * _baselineSensitivity;

        if (_useAutoSensitivity)
        {
            float currentFrameMaxIntensity = 0f;
            for (int i = 0; i < _numberOfColumns; i++) { currentFrameMaxIntensity = Mathf.Max(currentFrameMaxIntensity, _spectrumData[Mathf.FloorToInt((float)i * _spectrumData.Length / _numberOfColumns)]); }
            _maxObservedIntensity = Mathf.Max(_maxObservedIntensity, currentFrameMaxIntensity);
            
            for (int i = 0; i < _numberOfColumns; i++)
            {
                float rawIntensity = _spectrumData[Mathf.FloorToInt((float)i * _spectrumData.Length / _numberOfColumns)];
                rawIntensity = Mathf.Max(rawIntensity, baselineIntensity);

                if (rawIntensity > _smoothedIntensities[i])
                    _smoothedIntensities[i] = Mathf.Lerp(_smoothedIntensities[i], rawIntensity, _attackSpeed * Time.deltaTime);
                else
                    _smoothedIntensities[i] -= _smoothedIntensities[i] * _releaseSpeed * Time.deltaTime;
                
                float normalizedIntensity = Mathf.Clamp01(_smoothedIntensities[i] / _maxObservedIntensity);
                int activeBlocks = Mathf.RoundToInt(normalizedIntensity * _blocksPerColumn);
                SetColumnActive(i, activeBlocks);
            }

            _maxObservedIntensity -= _decaySpeed * Time.deltaTime;
            _maxObservedIntensity = Mathf.Max(_maxObservedIntensity, 0.001f);
        }
        else
        {
            for (int i = 0; i < _numberOfColumns; i++)
            {
                float rawIntensity = _spectrumData[Mathf.FloorToInt((float)i * _spectrumData.Length / _numberOfColumns)] * _manualSensitivity;
                rawIntensity = Mathf.Max(rawIntensity, baselineIntensity);

                if (rawIntensity > _smoothedIntensities[i])
                    _smoothedIntensities[i] = Mathf.Lerp(_smoothedIntensities[i], rawIntensity, _attackSpeed * Time.deltaTime);
                else
                    _smoothedIntensities[i] -= _smoothedIntensities[i] * _releaseSpeed * Time.deltaTime;

                float normalizedIntensity = Mathf.Clamp01(_smoothedIntensities[i]); 
                int activeBlocks = Mathf.RoundToInt(normalizedIntensity * _blocksPerColumn);
                SetColumnActive(i, activeBlocks);
            }
        }
    }

    /// <summary>
    /// 辅助函数：设置某一列需要点亮的方块数量
    /// </summary>
    void SetColumnActive(int columnIndex, int activeBlocks)
    {
        for (int j = 0; j < _blocksPerColumn; j++)
        {
            if (columnIndex < _visualizerBlocks.Length && j < _visualizerBlocks[columnIndex].Length && _visualizerBlocks[columnIndex][j] != null)
            {
                bool shouldBeActive = (j < activeBlocks);
                _visualizerBlocks[columnIndex][j].SetActive(shouldBeActive);
            }
        }
    }
}