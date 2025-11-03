// Assets/Scenes/Scripts/Debug/ActiveWatchdog.cs
using UnityEngine;
using Diag = System.Diagnostics;   // ← 別名にして UnityEngine.Debug と衝突回避

public class ActiveWatchdog : MonoBehaviour
{
    void OnEnable()
    {
        UnityEngine.Debug.Log(
            $"[ActiveWatchdog] ENABLED: {name} frame={Time.frameCount}\n{new Diag.StackTrace(1, true)}"
        );
    }

    void OnDisable()
    {
        UnityEngine.Debug.Log(
            $"[ActiveWatchdog] DISABLED: {name} frame={Time.frameCount}\n{new Diag.StackTrace(1, true)}"
        );
    }
}
