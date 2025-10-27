using UnityEngine;
using WR = WorldRibbon;

public class WorldRibbonSpawner : MonoBehaviour
{
    [Header("Prefab & Ribbon")]
    public GameObject notePrefab;
    public WR ribbon;

    [Header("Spawn Settings")]
    public float interval = 1.5f;
    public float timer = 0f;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = interval;
            SpawnNote();
        }
    }

    void SpawnNote()
    {
        if (!notePrefab || !ribbon) return;

        GameObject n = Instantiate(notePrefab, transform.position, Quaternion.identity);
        ribbon.expose01 = 1f; // ← SetExpose01の代替
        // 色設定は省略（自作WorldRibbonにcolorStart/colorEndが無いため）
    }
}
