using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitVFXPool : MonoBehaviour
{
    public static HitVFXPool Instance { get; private set; }

    [Header("VFX の Prefab (GameObject でOK: 中に ParticleSystem があれば可)")]
    public GameObject vfxPrefab;

    [Header("事前に何個用意するか")]
    public int prewarm = 8;

    readonly Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;

        if (vfxPrefab == null)
        {
            Debug.LogError("[HitVFXPool] vfxPrefab が未設定です。");
            return;
        }

        for (int i = 0; i < prewarm; i++)
        {
            var go = Instantiate(vfxPrefab, transform);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    public void PlayAt(Vector3 position, Quaternion rotation, Vector3? scale = null)
    {
        if (vfxPrefab == null) { Debug.LogError("[HitVFXPool] vfxPrefab 未設定"); return; }

        var go = pool.Count > 0 ? pool.Dequeue() : Instantiate(vfxPrefab, transform);
        go.transform.SetPositionAndRotation(position, rotation);
        if (scale.HasValue) go.transform.localScale = scale.Value;
        go.SetActive(true);

        // 中の全 ParticleSystem を必ず再生
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }

        StartCoroutine(ReturnWhenDone(go));
    }

    IEnumerator ReturnWhenDone(GameObject go)
    {
        // 1フレ待ってパーティクルが再生し始めるのを待つ
        yield return null;

        var list = go.GetComponentsInChildren<ParticleSystem>(true);
        bool alive;
        do
        {
            alive = false;
            foreach (var ps in list)
            {
                if (ps != null && ps.IsAlive(true)) { alive = true; break; }
            }
            yield return null;
        } while (alive);

        go.SetActive(false);
        pool.Enqueue(go);
    }
}
