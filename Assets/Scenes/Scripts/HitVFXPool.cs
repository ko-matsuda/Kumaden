using System.Collections.Generic;
using UnityEngine;

public class HitVFXPool : MonoBehaviour
{
    public static HitVFXPool I;
    public GameObject vfxPrefab;
    public int prewarm = 8;

    readonly Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        I = this;
        if (vfxPrefab == null) return;
        for (int i = 0; i < prewarm; i++)
            pool.Enqueue(CreateOne());
    }

    GameObject CreateOne()
    {
        var go = Instantiate(vfxPrefab, transform);
        go.SetActive(false);
        return go;
    }

    public GameObject Spawn(Vector3 pos, Color color, AudioClip se = null, float pitch = 1f)
    {
        var go = pool.Count > 0 ? pool.Dequeue() : CreateOne();
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        go.SetActive(true);

        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
            ps.Clear(true);
            ps.Play(true);
        }

        var audio = go.GetComponent<AudioSource>();
        if (audio != null)
        {
            if (se != null) audio.clip = se;
            audio.pitch = pitch;
            audio.Play();
        }

        // Ž©“®‚Å–ß‚·
        HitVFXReturn r = go.GetComponent<HitVFXReturn>();
        if (r == null) r = go.AddComponent<HitVFXReturn>();
        r.pool = this;
        return go;
    }

    public void Despawn(GameObject go)
    {
        go.SetActive(false);
        pool.Enqueue(go);
    }
}

public class HitVFXReturn : MonoBehaviour
{
    public HitVFXPool pool;
    ParticleSystem ps;
    void Awake(){ ps = GetComponent<ParticleSystem>(); }
    void Update()
    {
        if (ps != null && !ps.IsAlive(true))
            pool.Despawn(gameObject);
    }
}
