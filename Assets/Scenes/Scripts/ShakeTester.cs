using UnityEngine;

public class ShakeTester : MonoBehaviour
{
    TinyCameraShake s;
    void Awake(){ s = GetComponent<TinyCameraShake>(); }

    void Start()
    {
        // Ä¶1•bŒã‚©‚ç1•b‚¨‚«‚É—h‚ç‚·iŒ©‚¦‚â‚·‚¢‚æ‚¤‚É‹­‚ßj
        InvokeRepeating(nameof(DoShake), 1f, 1f);
    }

    void DoShake()
    {
        s?.Kick(0.25f, 0.20f); // ‚©‚È‚è•ª‚©‚è‚â‚·‚¢‹­‚³
    }
}
