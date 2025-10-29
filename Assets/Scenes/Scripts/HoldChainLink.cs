using UnityEngine;

namespace Kuma
{
    /// <summary>
    /// ノーツ同士を“つなぐ”ためのマーカー。
    /// 先頭ノーツに付けて、Inspector で router と next を割り当てます。
    /// </summary>
    public class HoldChainLink : MonoBehaviour
    {
        [SerializeField] private NoteBehaviour note;           // 自動で拾う用
        [SerializeField] private HoldEventRouter router;        // シーン上の HoldEventRouter をドラッグで割り当て
        public NoteBehaviour next;                              // 次に“つなぐ”相手

        private void Reset()
        {
            if (!note) note = GetComponent<NoteBehaviour>();
        }

        /// <summary>
        /// HoldTickToPickup の On Pickup (NoteBehaviour) から呼ぶ
        /// </summary>
        public void OnPickup(NoteBehaviour n)
        {
            if (!note) note = GetComponent<NoteBehaviour>();

            // 参照が無ければ何もしない
            if (router == null || next == null || n != note) return;

            // 帯を切らずに次ノーツへ橋渡し
            router.BridgeTo(note, next);
        }
    }
}
