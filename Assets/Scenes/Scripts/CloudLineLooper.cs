using UnityEngine;

public class CloudLineLooper : MonoBehaviour
{
    public float speed = 0.2f;   // 全雲共通の速さ
    public float startX = -200f; // 左端リセット位置
    public float endX = 200f;    // 右端を超えたら左に戻す

    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;

        if (transform.position.x > endX)
        {
            Vector3 pos = transform.position;
            pos.x = startX;
            transform.position = pos;
        }
    }
}
