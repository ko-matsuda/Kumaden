using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    [SerializeField] float speed = 8f;   // 1�b�ɉ����j�b�g�i�ނ��i���H�̑����j
    [SerializeField] Vector3 direction = new Vector3(0, 0, -1); // ��O�֗���

void Update()
    {
        if (Time.frameCount % 60 == 0) // 1秒に1回ログ
        {
            Debug.Log($"[WorldScroller] pos={transform.position}, speed={speed}, deltaTime={Time.deltaTime}, timeScale={Time.timeScale}");
        }
        transform.position += direction.normalized * speed * Time.deltaTime;
    }

    // ���x�𑼂̃X�N���v�g����ς��������p�i�C�Ӂj
    public void SetSpeed(float s) => speed = s;
}
