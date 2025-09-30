using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    [SerializeField] float speed = 8f;   // 1秒に何ユニット進むか（道路の速さ）
    [SerializeField] Vector3 direction = new Vector3(0, 0, -1); // 手前へ流す

    void Update()
    {
        transform.position += direction.normalized * speed * Time.deltaTime;
    }

    // 速度を他のスクリプトから変えたい時用（任意）
    public void SetSpeed(float s) => speed = s;
}
