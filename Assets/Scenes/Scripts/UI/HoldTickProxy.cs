using UnityEngine;

namespace Kuma
{
    // 帯(コライダー)から、固定のノーツNoteBehaviourに対して
    // HoldEventRouterのBegin/Hold/Endを呼ぶだけのブリッジ
    public class HoldTickProxy : MonoBehaviour
    {
        public HoldEventRouter router;     // シーンの HoldEventRouter
        public NoteBehaviour bindNote;     // “このノーツに対して”コンボ継続させる

        public void OnPickup()   { if (router && bindNote) router.BeginHold(bindNote); }
        public void OnHoldTick() { if (router && bindNote) router.HoldTick(bindNote); }
        public void OnRelease()  { if (router && bindNote) router.EndHold(bindNote); }
    }
}
