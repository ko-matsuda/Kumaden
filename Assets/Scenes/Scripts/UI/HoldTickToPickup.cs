using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class HoldTickToPickup : MonoBehaviour
{
    [Header("Hit Settings")]
    public string playerTag = "Player";
    public float tickInterval = 0.08f;
    public bool hitOnEnter = true;

    [Header("Events")]
    public UnityEvent onPickup;
    public UnityEvent onHoldTick;
    public UnityEvent onRelease;

    bool _holding;
    Coroutine _loop;
    Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col && !_col.isTrigger) _col.isTrigger = true;
    }

    void OnDisable() { StopHoldLoop(); }

    void OnTriggerEnter(Collider other)
    {
        if (!other || !other.gameObject.CompareTag(playerTag)) return;
        _holding = true;
        if (hitOnEnter && onPickup != null) onPickup.Invoke();
        StartHoldLoop();
    }

    void OnTriggerStay(Collider other)
    {
        if (!other || !other.gameObject.CompareTag(playerTag)) return;
        if (!_holding)
        {
            _holding = true;
            StartHoldLoop();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other || !other.gameObject.CompareTag(playerTag)) return;
        _holding = false;
        StopHoldLoop();
        if (onRelease != null) onRelease.Invoke();
    }

    void StartHoldLoop()
    {
        if (_loop != null) return;
        _loop = StartCoroutine(HoldLoop());
    }

    void StopHoldLoop()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    IEnumerator HoldLoop()
    {
        var wait = new WaitForSeconds(tickInterval);
        while (_holding)
        {
            if (onHoldTick != null) onHoldTick.Invoke();
            yield return wait;
        }
    }
}
