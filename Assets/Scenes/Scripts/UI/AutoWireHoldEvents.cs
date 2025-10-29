using UnityEngine;

[RequireComponent(typeof(HoldTickToPickup))]
public class AutoWireHoldEvents : MonoBehaviour
{
    void Awake()
    {
        var router = FindObjectOfType<HoldEventRouter>();
        var hit    = GetComponent<HoldTickToPickup>();
        if (router == null || hit == null) return;

        // いったん重複防止でクリア（必要なければ削除OK）
        hit.onPickup.RemoveAllListeners();
        hit.onHoldTick.RemoveAllListeners();
        hit.onRelease.RemoveAllListeners();

        // 動的イベントにルーターを接続
        hit.onPickup.AddListener(router.BeginHold);
        hit.onHoldTick.AddListener(router.HoldTick);
        hit.onRelease.AddListener(router.EndHold);
    }
}
