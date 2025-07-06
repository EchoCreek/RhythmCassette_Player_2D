using UnityEngine;
using System.Linq;

/// <summary>
/// 环形音频可视化器主类。
/// 该脚本负责从 AudioSource 获取音频数据，并将其转换为动态的环形视觉效果。
/// 它包含了布局生成、音频分析、节奏检测、动画平滑以及多种视觉增强功能。
/// </summary>
public class CircularAudioVisualizer : MonoBehaviour
{
    /// <summary>
    /// 定义数据源洗牌（重映射）的两种不同风格。
    /// </summary>
    public enum ShufflingStyle { Rotate, RandomShuffle }

    //======================================================================
    // 核心设置
    // 这是可视化器工作所必需的最基本组件。
    //======================================================================
    [Header("【核心设置】")]
    [Tooltip("提供音频数据的音源组件")]
    [SerializeField] private AudioSource _audioSource;
    [Tooltip("用于构成可视化效果的单个方块的预制件")]
    [SerializeField] private GameObject _blockPrefab;

    //======================================================================
    // 视觉设置
    // 定义可视化器的基本外观，如颜色方案和几何密度。
    //======================================================================
    [Header("【视觉设置】")]
    [Tooltip("可视化柱的颜色循环数组")]
    [SerializeField] private Color[] _columnColors = new Color[12];
    [Tooltip("环形中可视化柱的总数")]
    [SerializeField, Range(1, 100)] private int _numberOfColumns = 12;
    [Tooltip("每个可视化柱由多少个小方块堆叠而成")]
    [SerializeField, Range(1, 20)] private int _blocksPerColumn = 4;

    //======================================================================
    // 布局设置
    // 控制环形布局的具体尺寸和形状。
    //======================================================================
    [Header("【布局设置】")]
    [Tooltip("最内圈方块的起始半径")]
    [SerializeField] private float _innerRadius = 80f;
    [Tooltip("每一圈方块之间的半径增量")]
    [SerializeField] private float _radiusStep = 50f;
    [Tooltip("每个单独方块的高度（沿半径方向的长度）")]
    [SerializeField] private float _barHeight = 40f;
    [Tooltip("每一行（圈）的角度缩放因子，可用于创建内圈窄、外圈宽的效果")]
    [SerializeField] private float[] _rowAngularFactors = { 1f, 0.95f, 0.9f, 0.85f };

    //======================================================================
    // 动画设置
    // 调整可视化柱对音频强度变化的反应方式。
    //======================================================================
    [Header("【动画设置】")]
    [Tooltip("是否启用自动灵敏度调整")]
    [SerializeField] private bool _useAutoSensitivity = true;
    [Tooltip("手动设置的灵敏度乘数（仅在关闭自动灵敏度时生效）")]
    [SerializeField] private float _manualSensitivity = 1.0f;
    [Tooltip("自动灵敏度模式下，最大强度的衰减速度")]
    [SerializeField] private float _decaySpeed = 0.5f;
    [Tooltip("柱子高度上升时的速度")]
    [SerializeField] private float _attackSpeed = 25f;
    [Tooltip("柱子高度下降时的速度")]
    [SerializeField] private float _releaseSpeed = 1.5f;
    [Tooltip("音频强度的基线灵敏度，用于抬高低音量时的整体表现")]
    [SerializeField] private float _baselineSensitivity = 0.5f;

    //======================================================================
    // 视觉增强
    // 提供额外的视觉效果，使动画更平滑或更具动感。
    //======================================================================
    [Header("【视觉增强】")]
    [Tooltip("颜色循环滚动的速度")]
    [SerializeField] private float _colorCycleSpeed = 1.0f;
    [Tooltip("常规状态下，柱子高度变化的平滑时间，值越小反应越快")]
    [SerializeField, Range(0.01f, 0.5f)] private float _heightSmoothingTime = 0.08f;
    [Tooltip("鼓点触发时的专用平滑时间，值越小反应越快")]
    [SerializeField, Range(0.01f, 0.2f)]
    private float _beatSmoothingTime = 0.03f;

    //======================================================================
    // 可选修正
    // 针对持续高音量音乐的修正，防止视觉效果长时间“顶满”而失去动态。
    //======================================================================
    [Header("【可选修正】")]
    [Tooltip("是否启用条件性回落机制")]
    [SerializeField] private bool _enableConditionalFalloff = false;
    [Tooltip("触发回落机制的高度阈值（归一化后）")]
    [SerializeField, Range(0.1f, 1.0f)] private float _falloffThreshold = 0.8f;
    [Tooltip("高度持续超过阈值多长时间后，开始激活回落")]
    [SerializeField, Range(0.1f, 2.0f)] private float _falloffTimeLimit = 0.5f;
    [Tooltip("回落基线跟随当前高度的平滑时间")]
    [SerializeField, Range(0.01f, 2.0f)] private float _falloffBaselineMemory = 0.5f;

    //======================================================================
    // 数据洗牌
    // 动态改变频谱数据与可视化柱之间的映射关系，增加视觉变化。
    //======================================================================
    [Header("【数据洗牌】")]
    [Tooltip("是否启用数据源重映射（洗牌）")]
    [SerializeField] private bool _enableDataSourceShuffling = true;
    [Tooltip("执行一次洗牌操作的时间间隔（秒）")]
    [SerializeField] private float _shufflingInterval = 0.2f;
    [Tooltip("洗牌的方式：旋转或完全随机")]
    [SerializeField] private ShufflingStyle _shufflingStyle = ShufflingStyle.Rotate;

    //======================================================================
    // 节奏适配
    // 根据音乐的动态范围（平缓或激昂）自动调整动画参数。
    //======================================================================
    [Header("【节奏适配】")]
    [Tooltip("是否启用节奏适配，自动调整平滑度和洗牌间隔")]
    [SerializeField] private bool _enableRhythmAdaptation = true;
    [Tooltip("音乐平缓时允许的最小平滑时间")]
    [SerializeField, Range(0.03f, 0.15f)] private float _minSmoothingTime = 0.03f;
    [Tooltip("音乐激昂时允许的最大平滑时间")]
    [SerializeField, Range(0.03f, 0.15f)] private float _maxSmoothingTime = 0.15f;
    [Tooltip("音乐平缓时允许的最小洗牌间隔")]
    [SerializeField, Range(0.2f, 0.5f)] private float _minShuffleInterval = 0.2f;
    [Tooltip("音乐激昂时允许的最大洗牌间隔")]
    [SerializeField, Range(0.2f, 0.5f)] private float _maxShuffleInterval = 0.5f;
    [Tooltip("节奏变化检测的响应速度")]
    [SerializeField] private float _rhythmChangeSpeed = 1.5f;

    //======================================================================
    // 鼓点检测
    // 用于识别音乐中的鼓点（如底鼓），并触发特定的视觉反馈。
    //======================================================================
    [Header("【鼓点检测】")]
    [Tooltip("【推荐】设为 True 以启用基于频段的精准检测")]
    [SerializeField] private bool _useFrequencyBandDetection = true;

    [Header("鼓点检测 (频段)")]
    [Tooltip("要监听的最低频率 (Hz)，底鼓通常在 60-120Hz")]
    [SerializeField, Range(20, 500)]
    private float _beatFrequencyMin = 60f;
    [Tooltip("要监听的最高频率 (Hz)")]
    [SerializeField, Range(20, 500)]
    private float _beatFrequencyMax = 120f;
    [Tooltip("将侦测到的鼓点能量转化为视觉高度的放大倍率")]
    [SerializeField, Range(50, 5000)]
    private float _beatEnergyBoostMultiplier = 1000f;
    [Tooltip("能量超过动态阈值的倍数，才算作一次鼓点")]
    [SerializeField] private float _beatThreshold = 1.3f;
    [Tooltip("动态阈值跟随能量变化的衰减速度")]
    [SerializeField] private float _beatDecaySpeed = 0.2f;
    [Tooltip("（此参数已弃用，功能被EnergyBoostMultiplier替代）")]
    [SerializeField] private float _beatBoostMultiplier = 1.5f;
    [Tooltip("鼓点视觉效果的持续时间（秒）")]
    [SerializeField] private float _beatDuration = 0.15f;
    [Tooltip("防止前一帧能量值衰减到零，避免计算错误")]
    [SerializeField] private float _minEnergyFloor = 0.0001f;
    [Tooltip("动态阈值的最低限制，防止在静音时产生误判")]
    [SerializeField] private float _minBeatEnergy = 0.001f;

    // --- 私有成员变量 ---
    private CurvedVisualizerBlock[][] _visualizerBlocks;    // 存储所有可视化方块脚本的二维数组
    private float[] _spectrumData;                          // 存储原始频谱数据
    private float[] _smoothedIntensities;                   // 经过攻击/释放处理后的平滑强度值
    private float _maxObservedIntensity;                    // 用于自动灵敏度调节的观测到的最大强度
    private float _colorOffset;                             // 用于颜色循环的偏移量
    private float[] _currentVisualHeights;                  // 应用了SmoothDamp后的最终视觉高度
    private float[] _heightVelocities;                      // SmoothDamp所需的速度变量
    private float[] _stuckTimers;                           // 用于条件性回落功能，记录每个柱子持续高位的时间
    private float[] _falloffBaselines;                      // 条件性回落的基线值
    private float[] _falloffBaselineVelocities;             // 条件性回落基线的SmoothDamp速度变量
    private int[] _dataSourceMap;                           // 数据源映射表，用于洗牌
    private float _shufflingTimer;                          // 洗牌计时器
    private System.Random _random = new System.Random();    // 用于随机洗牌的随机数生成器
    private float _intensityVarianceSmoothed;               // 用于节奏适配的平滑后的强度方差

    // --- 鼓点检测相关变量 ---
    private float[] _waveform = new float[1024];            // 存储音频波形数据（用于旧版检测）
    private float _prevEnergy = 0f;                         // 用于计算动态阈值的前一帧能量
    private float _beatTimer = 0f;                          // 鼓点效果持续时间的计时器
    private bool _beatDetected = false;                     // 当前帧是否处于鼓点触发状态
    private float _lastBeatEnergy = 0f;                     // 最后一次检测到的鼓点能量值

    /// <summary>
    /// 初始化函数，在脚本实例被加载时调用。
    /// </summary>
    void Start()
    {
        _audioSource.volume = 0.75f;
        GenerateLayout(); // 创建可视化布局
        // 初始化各种数组和变量
        _spectrumData = new float[512];
        _smoothedIntensities = new float[_numberOfColumns];
        _currentVisualHeights = new float[_numberOfColumns];
        _heightVelocities = new float[_numberOfColumns];
        _stuckTimers = new float[_numberOfColumns];
        _falloffBaselines = new float[_numberOfColumns];
        _falloffBaselineVelocities = new float[_numberOfColumns];
        _dataSourceMap = Enumerable.Range(0, _numberOfColumns).ToArray(); // 初始化映射表
    }

    /// <summary>
    /// 每帧调用，是可视化的主循环。
    /// </summary>
    void Update()
    {
        // 如果音频未播放，则平滑地将所有柱子归零并退出
        if (!_audioSource.isPlaying)
        {
            for (int i = 0; i < _numberOfColumns; i++)
                _smoothedIntensities[i] = Mathf.Lerp(_smoothedIntensities[i], 0f, Time.deltaTime * 5f);
            return;
        }

        // 获取音频频谱数据
        _audioSource.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris);

        // --- 自动灵敏度调节 ---
        if (_useAutoSensitivity)
        {
            float currentFrameMax = _spectrumData.Take(_numberOfColumns).Max();
            _maxObservedIntensity = Mathf.Max(_maxObservedIntensity, currentFrameMax);
        }

        // 计算基线强度
        float averageVolume = _spectrumData.Average();
        float baselineIntensity = averageVolume * _baselineSensitivity;

        // --- 节奏适配逻辑 ---
        if (_enableRhythmAdaptation)
        {
            // 计算标准差作为音乐动态范围的衡量标准
            float variance = _spectrumData.Select(s => (s - averageVolume) * (s - averageVolume)).Sum() / _spectrumData.Length;
            float stdDev = Mathf.Sqrt(variance);
            _intensityVarianceSmoothed = Mathf.Lerp(_intensityVarianceSmoothed, stdDev, Time.deltaTime * _rhythmChangeSpeed);
            // 将平滑后的标准差映射到0-1范围，用于插值计算平滑时间和洗牌间隔
            float t = Mathf.InverseLerp(0.001f, 0.01f, _intensityVarianceSmoothed);
            _heightSmoothingTime = Mathf.Lerp(_maxSmoothingTime, _minSmoothingTime, t);
            _shufflingInterval = Mathf.Lerp(_maxShuffleInterval, _minShuffleInterval, t);
        }

        // --- 数据洗牌逻辑 ---
        if (_enableDataSourceShuffling)
        {
            _shufflingTimer += Time.deltaTime;
            if (_shufflingTimer >= _shufflingInterval)
            {
                _shufflingTimer = 0;
                PerformShuffle();
            }
        }

        // --- 整合后的鼓点计时逻辑 ---
        if (DetectBeat(_spectrumData))
        {
            _beatDetected = true;
            _beatTimer = _beatDuration; // 重置鼓点计时器
        }

        if (_beatTimer > 0)
        {
            _beatTimer -= Time.deltaTime;
            if (_beatTimer <= 0)
            {
                _beatDetected = false; // 鼓点效果结束
            }
        }
        
        // --- 循环更新每一根可视化柱 ---
        for (int i = 0; i < _numberOfColumns; i++)
            UpdateColumnVisuals(i, baselineIntensity);

        // --- 自动灵敏度的衰减处理 ---
        if (_useAutoSensitivity)
        {
            _maxObservedIntensity -= _decaySpeed * Time.deltaTime;
            _maxObservedIntensity = Mathf.Max(_maxObservedIntensity, 0.001f); // 防止衰减到0
        }
    }

    /// <summary>
    /// 根据映射表获取指定柱子对应的原始频谱强度。
    /// </summary>
    float GetRawIntensityForColumn(int columnIndex)
    {
        int mappedIndex = _dataSourceMap[columnIndex];
        float fraction = (float)mappedIndex / _numberOfColumns;
        int spectrumIndex = Mathf.FloorToInt(fraction * _spectrumData.Length);
        spectrumIndex = Mathf.Clamp(spectrumIndex, 0, _spectrumData.Length - 1);
        return _spectrumData[spectrumIndex];
    }

    /// <summary>
    /// 检测当前音频帧是否包含鼓点。
    /// </summary>
    /// <param name="spectrum">当前帧的频谱数据。</param>
    /// <returns>如果检测到鼓点则返回 true。</returns>
    bool DetectBeat(float[] spectrum)
    {
        float energy;

        // 根据设置选择不同的能量计算方式
        if (_useFrequencyBandDetection)
        {
            // === 基于频段的能量计算 ===
            int targetIndexMin = FrequencyToIndex(spectrum.Length, _beatFrequencyMin);
            int targetIndexMax = FrequencyToIndex(spectrum.Length, _beatFrequencyMax);
            
            targetIndexMin = Mathf.Clamp(targetIndexMin, 0, spectrum.Length - 1);
            targetIndexMax = Mathf.Clamp(targetIndexMax, targetIndexMin, spectrum.Length - 1);

            energy = 0f;
            if (targetIndexMax > targetIndexMin)
            {
                // 计算指定频段内的平均能量
                for (int i = targetIndexMin; i <= targetIndexMax; i++)
                {
                    energy += spectrum[i];
                }
                energy /= (targetIndexMax - targetIndexMin + 1);
            }
            else
            {
                energy = spectrum[targetIndexMin];
            }
        }
        else
        {
            // === 保留原有的、基于波形的总能量计算 ===
            _audioSource.GetOutputData(_waveform, 0);
            energy = _waveform.Select(x => x * x).Average();
        }

        // --- 动态阈值判断逻辑 ---
        _prevEnergy = Mathf.Lerp(_prevEnergy, energy, _beatDecaySpeed); 
        _prevEnergy = Mathf.Max(_prevEnergy, _minEnergyFloor);

        float dynamicThreshold = Mathf.Max(_prevEnergy * _beatThreshold, _minBeatEnergy);
        bool isBeat = energy > dynamicThreshold;

        // 如果是鼓点，就记录下它的能量值，用于后续计算视觉效果强度
        if (isBeat)
        {
            _lastBeatEnergy = energy;
        }

        return isBeat;
    }

    /// <summary>
    /// 辅助函数，用于将频率(Hz)转换为频谱数据中的索引。
    /// </summary>
    private int FrequencyToIndex(int spectrumSize, float frequency)
    {
        float maxFrequency = AudioSettings.outputSampleRate / 2.0f; // 奈奎斯特频率
        float frequencyPerIndex = maxFrequency / spectrumSize;
        return Mathf.FloorToInt(frequency / frequencyPerIndex);
    }

    /// <summary>
    /// 执行数据源映射的洗牌操作。
    /// </summary>
    void PerformShuffle()
    {
        if (_shufflingStyle == ShufflingStyle.Rotate)
        {
            // 旋转数组
            int last = _dataSourceMap[_dataSourceMap.Length - 1];
            System.Array.Copy(_dataSourceMap, 0, _dataSourceMap, 1, _dataSourceMap.Length - 1);
            _dataSourceMap[0] = last;
        }
        else // RandomShuffle
        {
            // Fisher-Yates 洗牌算法
            for (int i = _dataSourceMap.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_dataSourceMap[i], _dataSourceMap[j]) = (_dataSourceMap[j], _dataSourceMap[i]);
            }
        }
    }

    /// <summary>
    /// 更新单个可视化柱的视觉表现（高度、颜色等）。
    /// </summary>
    /// <param name="columnIndex">柱子的索引。</param>
    /// <param name="baselineIntensity">当前帧的基线强度。</param>
    void UpdateColumnVisuals(int columnIndex, float baselineIntensity)
    {
        // --- 步骤1: 获取并处理原始强度 ---
        float rawIntensity = GetRawIntensityForColumn(columnIndex);
        if (!_useAutoSensitivity) rawIntensity *= _manualSensitivity;
        rawIntensity = Mathf.Max(rawIntensity, baselineIntensity); // 应用基线

        // --- 步骤2: 应用攻击与释放，得到平滑强度 ---
        if (rawIntensity > _smoothedIntensities[columnIndex])
            _smoothedIntensities[columnIndex] = Mathf.Lerp(_smoothedIntensities[columnIndex], rawIntensity, _attackSpeed * Time.deltaTime);
        else
            _smoothedIntensities[columnIndex] -= _smoothedIntensities[columnIndex] * _releaseSpeed * Time.deltaTime;

        _smoothedIntensities[columnIndex] = Mathf.Max(0, _smoothedIntensities[columnIndex]);

        // --- 步骤3: 归一化强度值 ---
        float absoluteNormalized = _useAutoSensitivity ?
            Mathf.Clamp01(_smoothedIntensities[columnIndex] / _maxObservedIntensity) :
            Mathf.Clamp01(_smoothedIntensities[columnIndex]);

        float finalNormalized = absoluteNormalized;

        // --- 步骤4: (可选)应用条件性回落 ---
        if (_enableConditionalFalloff)
        {
            if (absoluteNormalized > _falloffThreshold)
                _stuckTimers[columnIndex] += Time.deltaTime;
            else
                _stuckTimers[columnIndex] = 0;

            if (_stuckTimers[columnIndex] > _falloffTimeLimit)
            {
                // 当柱子卡在高位时，动态提升其“地面”，使得只有超过这个“地面”的部分才被显示
                _falloffBaselines[columnIndex] = Mathf.SmoothDamp(
                    _falloffBaselines[columnIndex],
                    _smoothedIntensities[columnIndex],
                    ref _falloffBaselineVelocities[columnIndex],
                    _falloffBaselineMemory
                );
                float suppressed = _smoothedIntensities[columnIndex] - _falloffBaselines[columnIndex];
                finalNormalized = _useAutoSensitivity ?
                    Mathf.Clamp01(suppressed / _maxObservedIntensity) :
                    Mathf.Clamp01(suppressed);
            }
            else
            {
                // 不满足条件时，逐渐降低“地面”
                _falloffBaselines[columnIndex] = Mathf.Lerp(_falloffBaselines[columnIndex], 0f, Time.deltaTime);
            }
        }

        // --- 步骤5: 计算基础高度 ---
        float baseHeight = finalNormalized * _blocksPerColumn;

        // --- 步骤6: (全新)叠加式鼓点增强 ---
        if (_beatTimer > 0)
        {
            // 使用 beatTimer 创建一个从1到0平滑过渡的脉冲因子
            float pulseFactor = _beatTimer / _beatDuration;

            // 基于上次检测到的鼓点能量，计算要叠加的脉冲高度
            float beatPulseAmount = _lastBeatEnergy * _beatEnergyBoostMultiplier * pulseFactor;
            
            // 将脉冲高度直接叠加在常规高度之上
            baseHeight += beatPulseAmount;
            
            // 确保总高度不超过上限
            baseHeight = Mathf.Min(baseHeight, _blocksPerColumn);
        }

        // --- 步骤7: 应用最终的高度平滑(SmoothDamp) ---
        // 根据是否为鼓点时间，选择不同的平滑时间，实现快速响应
        float currentSmoothingTime = (_beatTimer > 0) ? _beatSmoothingTime : _heightSmoothingTime;

        _currentVisualHeights[columnIndex] = Mathf.SmoothDamp(
            _currentVisualHeights[columnIndex],
            baseHeight,
            ref _heightVelocities[columnIndex],
            currentSmoothingTime
        );

        // --- 步骤8: 应用视觉更新 ---
        SetColumnColor(columnIndex);
        SetColumnHeight(columnIndex, _currentVisualHeights[columnIndex]);
    }

    /// <summary>
    /// 在编辑器中提供一个按钮，用于手动生成或刷新可视化布局。
    /// </summary>
    [ContextMenu("Generate Layout")]
    void GenerateLayout()
    {
        // 清理旧的布局
        for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);
        if (_blockPrefab == null) { Debug.LogError("Block Prefab is not set!"); return; }

        // 初始化存储数组
        _visualizerBlocks = new CurvedVisualizerBlock[_numberOfColumns][];
        float angleIncrement = 360f / _numberOfColumns;

        // 循环创建每一个柱子
        for (int i = 0; i < _numberOfColumns; i++)
        {
            _visualizerBlocks[i] = new CurvedVisualizerBlock[_blocksPerColumn];
            float columnStartAngle = i * angleIncrement;

            // 循环创建柱子内的每一个方块
            for (int j = 0; j < _blocksPerColumn; j++)
            {
                // 计算每个方块的位置和尺寸参数
                float blockInnerRadius = _innerRadius + (j * _radiusStep);
                float blockOuterRadius = blockInnerRadius + _barHeight;
                float factor = (j < _rowAngularFactors.Length) ? _rowAngularFactors[j] : 1f;
                float angularWidth = angleIncrement * factor;
                float anglePadding = (angleIncrement - angularWidth) / 2f;
                float blockStartAngle = columnStartAngle + anglePadding;
                float blockEndAngle = blockStartAngle + angularWidth;

                // 实例化预制件并设置参数
                GameObject blockObj = Instantiate(_blockPrefab, transform);
                CurvedVisualizerBlock blockScript = blockObj.GetComponent<CurvedVisualizerBlock>();
                blockScript.InnerRadius = blockInnerRadius;
                blockScript.OuterRadius = blockOuterRadius;
                blockScript.StartAngle_Degrees = blockStartAngle;
                blockScript.EndAngle_Degrees = blockEndAngle;

                if (i < _columnColors.Length)
                    blockScript.SetActiveColor(_columnColors[i]);

                _visualizerBlocks[i][j] = blockScript;
            }
        }
    }

    /// <summary>
    /// 设置指定柱子的颜色。
    /// </summary>
    void SetColumnColor(int columnIndex)
    {
        if (_columnColors.Length == 0) return;
        // 使用 colorOffset 实现颜色循环
        int colorIndex = (columnIndex + (int)_colorOffset) % _columnColors.Length;
        Color newColor = _columnColors[colorIndex];
        for (int j = 0; j < _blocksPerColumn; j++)
            _visualizerBlocks[columnIndex][j]?.SetActiveColor(newColor);
    }

    /// <summary>
    /// 设置指定柱子的高度。
    /// </summary>
    /// <param name="columnIndex">柱子索引。</param>
    /// <param name="height">目标高度（0到_blocksPerColumn之间）。</param>
    void SetColumnHeight(int columnIndex, float height)
    {
        for (int j = 0; j < _blocksPerColumn; j++)
        {
            // 计算每个方块的激活程度（0到1）
            float level = Mathf.Clamp01(height - j);
            _visualizerBlocks[columnIndex][j]?.SetState(level);
        }
    }
}