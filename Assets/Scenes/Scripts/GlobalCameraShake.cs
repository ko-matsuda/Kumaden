// GlobalCameraShake.cs  —— どこに置いてもOK（Scriptsフォルダなど）
using UnityEngine;

public static class GlobalCameraShake
{
    // 有効/無効（プロローグ中は false などに）
    public static bool Enabled = true;

    // 現在のオフセット（XY）
    static Vector2 current;
    static Vector2 velocity; // SmoothDamp用
    static float   decayTime;

    // 1フレーム毎に呼ばれて、カメラに渡す最終オフセットを返す
    public static Vector2 StepAndGetOffset()
    {
        if (!Enabled) { current = Vector2.zero; velocity = Vector2.zero; return Vector2.zero; }

        if (decayTime > 0f)
        {
            // 時間経過でなめらかに0へ
            current = Vector2.SmoothDamp(current, Vector2.zero, ref velocity, decayTime);
        }
        return current;
    }

    // 揺れを足す（power: 強さ / duration: 収束までの秒数）
    public static void Kick(float power, float duration)
    {
        if (!Enabled) return;
        // ランダム方向へ少し弾く
        var dir = Random.insideUnitCircle.normalized;
        current += dir * power;
        decayTime = Mathf.Max(0.0001f, duration);
    }

    // 目で分かる強さ（テスト用）
    public static void KickStrong() => Kick(0.12f, 0.18f);
}
