using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    [SerializeField] float speed = 8f;
    [SerializeField] Vector3 direction = new Vector3(0, 0, -1);

    void Update()
    {
        transform.position += direction.normalized * speed * Time.deltaTime;
    }

    public void SetSpeed(float s) => speed = s;
}
