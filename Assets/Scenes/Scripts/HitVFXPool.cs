using UnityEngine;
using System.Collections.Generic;

public class HitVFXPool : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private int prewarm = 8;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private static HitVFXPool instance;

    public static HitVFXPool Instance => instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("[HitVFXPool] Multiple instances detected!");
        }

        // プール初期化
        for (int i = 0; i < prewarm; i++)
        {
            CreateVFX();
        }
        
        Debug.Log($"[HitVFXPool] Initialized with {prewarm} VFX objects");
    }

    private GameObject CreateVFX()
    {
        if (vfxPrefab == null)
        {
            Debug.LogError("[HitVFXPool] vfxPrefab is null!");
            return null;
        }

        GameObject vfx = Instantiate(vfxPrefab, transform);
        vfx.SetActive(false);
        pool.Enqueue(vfx);
        return vfx;
    }

    public void PlayVFX(Vector3 position, string judgement = "PERFECT")
    {
        Debug.Log($"[HitVFXPool] PlayVFX called at {position}, judgement={judgement}");
        
        GameObject vfx = pool.Count > 0 ? pool.Dequeue() : CreateVFX();
        
        if (vfx == null)
        {
            Debug.LogError("[HitVFXPool] Failed to get VFX object!");
            return;
        }

        vfx.transform.position = position;
        vfx.SetActive(true);

        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            switch (judgement)
            {
                case "PERFECT":
                    main.startColor = new Color(1f, 0.9f, 0.3f);
                    Debug.Log("[HitVFXPool] Set color to PERFECT (yellow)");
                    break;
                case "GOOD":
                    main.startColor = new Color(0.3f, 1f, 0.5f);
                    Debug.Log("[HitVFXPool] Set color to GOOD (green)");
                    break;
                default:
                    main.startColor = Color.white;
                    break;
            }

            ps.Play();
            Debug.Log($"[HitVFXPool] ParticleSystem.Play() called, isPlaying={ps.isPlaying}");
        }
        else
        {
            Debug.LogError("[HitVFXPool] VFX object has no ParticleSystem component!");
        }

        StartCoroutine(ReturnToPoolAfterDelay(vfx, 2f));
    }

    private System.Collections.IEnumerator ReturnToPoolAfterDelay(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (vfx != null)
        {
            vfx.SetActive(false);
            pool.Enqueue(vfx);
            Debug.Log("[HitVFXPool] VFX returned to pool");
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
