using System;
using UnityEngine;
using UnityEngine.Events;

namespace Kuma
{
    /// <summary>
    /// COMBOの集計とUI表示更新のラッチ制御。
    /// ・帯がアクティブの間だけTickを受理（_ribbonActive）
    /// ・StopUpdatingで「表示更新のみ停止」（内部値は保持）
    /// ・次帯のOnHoldEnterでResumeUpdating（再通知あり）
    /// </summary>
    public sealed class ComboProbe : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private int _tickIncrement = 1;          // Tick毎の増分（通常1）
        [SerializeField] private bool _resetOnExplicitMiss = true; // 明示ミス時のみリセット運用

        [Header("Latch / State")]
        [SerializeField] private bool _isUpdating = true;  // 表示更新ラッチ
        [SerializeField] private bool _ribbonActive = false; // 帯アクティブ判定

        [Header("Events")]
        public UnityEvent<int> OnShownComboChanged; // 表示用
        public UnityEvent<int> OnTotalComboChanged; // 内部統計用など

        private int _shownCombo; // 画面表示値
        private int _totalCombo; // 総カウント
        private int _lastTickFrame = -1; // 同フレーム二重加算ガード

        // ───────── 帯イベント（発火元に合わせて OnHold* 命名） ─────────

        public void OnHoldEnter()
        {
            _ribbonActive = true;
            ResumeUpdating();   // 表示更新を再開し、現値を即再通知
        }

        public void OnHoldTick()
        {
            if (!_ribbonActive) return; // 帯外のTickは無視

            int f = Time.frameCount;
            if (_lastTickFrame == f) return; // 同フレーム多重防止
            _lastTickFrame = f;

            // 内部値は常に積む
            _totalCombo += _tickIncrement;
            if (OnTotalComboChanged != null) OnTotalComboChanged.Invoke(_totalCombo);

            // 表示更新はラッチに従う
            if (_isUpdating)
            {
                _shownCombo += _tickIncrement;
                if (OnShownComboChanged != null) OnShownComboChanged.Invoke(_shownCombo);
            }
        }

        public void OnHoldExit()
        {
            // 帯終端時の安全策：表示更新のみ停止＋帯を非アクティブ化
            _ribbonActive = false;
            StopUpdating();
        }

        // ───────── ラッチ操作（外部からも呼べるAPI） ─────────

        /// <summary>表示更新のみ停止（内部値は保持）。冪等。</summary>
        public void StopUpdating()
        {
            _isUpdating = false;
        }

        /// <summary>表示更新を再開。現値を即座に再通知（UIの取りこぼし防止）。</summary>
        public void ResumeUpdating()
        {
            _isUpdating = true;
            if (OnShownComboChanged != null) OnShownComboChanged.Invoke(_shownCombo);
            if (OnTotalComboChanged != null) OnTotalComboChanged.Invoke(_totalCombo);
        }

        /// <summary>EndGate等からの外部強制停止用（帯も非アクティブに落とす）。</summary>
        public void ForceStopFromGate()
        {
            _ribbonActive = false;
            StopUpdating();
        }

        // ───────── 明示イベント時のリセット（ミス等） ─────────

        public void ResetAll()
        {
            _shownCombo = 0;
            _totalCombo = 0;
            if (OnShownComboChanged != null) OnShownComboChanged.Invoke(_shownCombo);
            if (OnTotalComboChanged != null) OnTotalComboChanged.Invoke(_totalCombo);
        }

        public void OnExplicitMiss()
        {
            if (_resetOnExplicitMiss) ResetAll();
        }
    }
}
