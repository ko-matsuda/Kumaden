using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class NoteBehaviourEvent : UnityEvent<NoteBehaviour> {}

public class HoldTickToPickup : MonoBehaviour
{
    [Header("Hit Settings")]
    public string playerTag = "Player";
    public float tickInterval = 0.08f;
    public bool hitOnEnter = true;

    // ★ AutoWire用に公開イベントを用意（名前を厳密に一致させる）
    public NoteBehaviourEvent onPickup = new NoteBehaviourEvent();
    public NoteBehaviourEvent onHoldTick = new NoteBehaviourEvent();
    public NoteBehaviourEvent onRelease = new NoteBehaviourEvent();

    // 以下は既存ロジックに合わせて（例）
    NoteBehaviour note;
    float tickTimer;

    void Awake()
    {
        note = GetComponent<NoteBehaviour>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hitOnEnter) return;
        if (other.CompareTag(playerTag) && note != null)
            onPickup.Invoke(note);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(playerTag) || note == null) return;

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            onHoldTick.Invoke(note);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && note != null)
            onRelease.Invoke(note);
    }
}
