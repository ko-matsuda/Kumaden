using System;
using UnityEngine;
using UnityEngine.Events;

namespace Kuma
{
    /// <summary>
    /// ノーツ（食材）カウンタ。帯がアクティブの間だけ、Tickごとに 1:1 で加算。
    /// 表示更新は isUpdating ラッチで停止可能（内部値は保持）。
    /// </summary>
    public sealed class HudCounterBinder : MonoBehaviour
    {
        public enum IngredientType
        {
            Milk = 0,
            Flour = 1,
            Egg = 2
        }

        [Header("Config")]
        [SerializeField] private IngredientType _tickAddsTo = IngredientType.Milk;

        [Header("Latch (UI更新のみ停止)")]
        [SerializeField] private bool _isUpdating = true;

        [Header("State")]
        [SerializeField] private bool _ribbonActive = false;

        [Header("Totals")]
        [SerializeField] private int _milk;
        [SerializeField] private int _flour;
        [SerializeField] private int _egg;

        [Header("Events")]
        public UnityEvent<int> OnMilkChanged;
        public UnityEvent<int> OnFlourChanged;
        public UnityEvent<int> OnEggChanged;
        public UnityEvent OnAnyChanged;

        private int _lastTickFrame = -1;

        // ───── 帯イベント（発火元に合わせて OnHold* 命名） ─────

        public void OnHoldEnter()
        {
            _ribbonActive = true;
            ResumeUpdating();
            NotifyAll(); // 現在値を即反映（UI取りこぼし防止）
        }

        public void OnHoldTick()
        {
            if (!_ribbonActive) return; // 帯外のTickは無視

            int currentFrame = Time.frameCount;
            if (_lastTickFrame == currentFrame) return; // 同フレーム多重Tick防止
            _lastTickFrame = currentFrame;

            // 内部カウント（表示更新の有無と独立）
            switch (_tickAddsTo)
            {
                case IngredientType.Milk:  _milk += 1; break;
                case IngredientType.Flour: _flour += 1; break;
                case IngredientType.Egg:   _egg += 1; break;
            }

            // 表示更新はラッチに従う
            if (_isUpdating)
            {
                switch (_tickAddsTo)
                {
                    case IngredientType.Milk:  if (OnMilkChanged  != null) OnMilkChanged.Invoke(_milk);   break;
                    case IngredientType.Flour: if (OnFlourChanged != null) OnFlourChanged.Invoke(_flour); break;
                    case IngredientType.Egg:   if (OnEggChanged   != null) OnEggChanged.Invoke(_egg);     break;
                }
                if (OnAnyChanged != null) OnAnyChanged.Invoke();
            }
        }

        public void OnHoldExit()
        {
            _ribbonActive = false;
            StopUpdating(); // 帯終端で表示更新のみ停止（内部値は保持）
        }

        // ───── 外部ゲートからの強制停止も受けられるように ─────

        public void ForceStopFromGate()
        {
            _ribbonActive = false;
            StopUpdating();
        }

        // ───── ラッチ操作 ─────

        public void StopUpdating()
        {
            _isUpdating = false;
        }

        public void ResumeUpdating()
        {
            _isUpdating = true;
        }

        // ───── 補助 ─────

        public void SetTickAddsTo(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex > 2) return;
            _tickAddsTo = (IngredientType)typeIndex;
        }

        private void NotifyAll()
        {
            if (OnMilkChanged  != null) OnMilkChanged.Invoke(_milk);
            if (OnFlourChanged != null) OnFlourChanged.Invoke(_flour);
            if (OnEggChanged   != null) OnEggChanged.Invoke(_egg);
            if (OnAnyChanged   != null) OnAnyChanged.Invoke();
        }
    }
}
