using System;
using System.Diagnostics;

namespace NetEaseLyricsBar.Services
{
    /// <summary>
    /// 智能进度跟踪器（带延时补偿）
    /// 结合计时器和延时补偿，提供更准确的播放进度跟踪
    /// </summary>
    public class SmartProgressTracker
    {
        private DateTime _songStartTime;
        private double _baseProgress; // 基准进度（用于检测跳转）
        private DateTime _baseTime;   // 基准时间
        private bool _isTracking;
        private double _lastReportedProgress;
        private int _consecutiveAnomalies;

        // 配置参数
        private const double AnomalyThreshold = 2.0; // 异常检测阈值（秒）
        private const int MaxConsecutiveAnomalies = 3; // 最大连续异常次数

        /// <summary>
        /// 检测到进度跳转事件（用户拖动了进度条）
        /// </summary>
        public event EventHandler<double>? ProgressJumpDetected;

        /// <summary>
        /// 开始跟踪
        /// </summary>
        /// <param name="initialProgress">初始进度（秒）</param>
        public void StartTracking(double initialProgress = 0)
        {
            Debug.WriteLine($"[智能跟踪] ===== 开始跟踪 =====");
            Debug.WriteLine($"[智能跟踪] 初始进度: {initialProgress:F2}秒");

            _songStartTime = DateTime.Now;
            _baseProgress = initialProgress;
            _baseTime = DateTime.Now;
            _isTracking = true;
            _lastReportedProgress = initialProgress;
            _consecutiveAnomalies = 0;
        }

        /// <summary>
        /// 停止跟踪
        /// </summary>
        public void StopTracking()
        {
            Debug.WriteLine("[智能跟踪] 停止跟踪");
            _isTracking = false;
        }

        /// <summary>
        /// 获取当前进度（带延时补偿）
        /// </summary>
        /// <returns>当前播放进度（秒）</returns>
        public double GetCurrentProgress()
        {
            if (!_isTracking)
            {
                Debug.WriteLine("[智能跟踪] ⚠ 未在跟踪状态");
                return _lastReportedProgress;
            }

            // 计算当前时间相对于基准时间的时间差
            var timeDelta = (DateTime.Now - _baseTime).TotalSeconds;

            // 计算当前进度
            var currentProgress = _baseProgress + timeDelta;

            _lastReportedProgress = currentProgress;

            return currentProgress;
        }

        /// <summary>
        /// 更新基准进度（当检测到进度跳转时调用）
        /// </summary>
        /// <param name="newProgress">新的基准进度</param>
        public void UpdateBaseProgress(double newProgress)
        {
            if (!_isTracking)
                return;

            var oldProgress = GetCurrentProgress();
            var delta = Math.Abs(newProgress - oldProgress);

            Debug.WriteLine($"[智能跟踪] 更新基准进度: {oldProgress:F2} -> {newProgress:F2} (Δ={delta:F2})");

            // 检测是否是跳转（进度变化超过阈值）
            if (delta > AnomalyThreshold)
            {
                Debug.WriteLine($"[智能跟踪] ✓ 检测到进度跳转: {delta:F2}秒");

                _consecutiveAnomalies++;

                if (_consecutiveAnomalies >= MaxConsecutiveAnomalies)
                {
                    // 确认是跳转，触发事件
                    Debug.WriteLine($"[智能跟踪] ✓ 确认跳转，触发事件");
                    ProgressJumpDetected?.Invoke(this, newProgress);
                    _consecutiveAnomalies = 0;
                }
            }
            else
            {
                _consecutiveAnomalies = 0;
            }

            _baseProgress = newProgress;
            _baseTime = DateTime.Now;
        }

        /// <summary>
        /// 手动设置进度（用户拖动进度条后使用）
        /// </summary>
        /// <param name="newProgress">新进度</param>
        public void ManualSetProgress(double newProgress)
        {
            Debug.WriteLine($"[智能跟踪] 手动设置进度: {newProgress:F2}秒");

            _baseProgress = newProgress;
            _baseTime = DateTime.Now;
            _lastReportedProgress = newProgress;
            _consecutiveAnomalies = 0;
        }

        /// <summary>
        /// 重置跟踪器
        /// </summary>
        public void Reset()
        {
            Debug.WriteLine("[智能跟踪] 重置跟踪器");

            _songStartTime = DateTime.Now;
            _baseProgress = 0;
            _baseTime = DateTime.Now;
            _lastReportedProgress = 0;
            _consecutiveAnomalies = 0;
        }

        /// <summary>
        /// 获取跟踪状态信息
        /// </summary>
        /// <returns>状态描述</returns>
        public string GetStatusInfo()
        {
            var current = GetCurrentProgress();
            var elapsed = (DateTime.Now - _songStartTime).TotalSeconds;

            return $"跟踪中 | 进度: {current:F2}s | 已播放: {elapsed:F2}s";
        }

        /// <summary>
        /// 是否正在跟踪
        /// </summary>
        public bool IsTracking => _isTracking;

        /// <summary>
        /// 上次报告的进度
        /// </summary>
        public double LastReportedProgress => _lastReportedProgress;
    }
}
