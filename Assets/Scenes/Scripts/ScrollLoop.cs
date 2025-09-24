using UnityEngine;

public class ScrollLoop : MonoBehaviour
{
    [Tooltip("前にどれだけ長い通りか（RoadのZスケールと合わせると簡単）")]
    public float length = 60f;
    [Tooltip("手前→奥の流れる速さ")]
    public float speed = 8f;

    float startZ;

    void Start()
    {
        startZ = transform.position.z;
    }

    void Update()
    {
        transform.Translate(0, 0, -speed * Time.deltaTime);

        // 後ろに抜けたら先頭へワープ
        if (transform.position.z <= startZ - length)
        {
            transform.position += new Vector3(0, 0, length);
        }
    }
}
