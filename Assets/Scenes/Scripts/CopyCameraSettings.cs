// CopyCameraSettings.cs  —— WorldCam に付ける（Source に PlayerCam を指定）
using UnityEngine;

[ExecuteAlways]
public class CopyCameraSettings : MonoBehaviour
{
    public Camera source;          // ← PlayerCam をドラッグ
    private Camera self;

    void Awake()
    {
        self = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (!source) return;

        // 元カメラ（PlayerCam）をコピー
        transform.position = source.transform.position;
        transform.rotation = source.transform.rotation;

        // ★ シェイクを加算
        var off = GlobalCameraShake.StepAndGetOffset();
        if (off != Vector2.zero)
        {
            const float k = 1.5f; // 体感スケール。弱ければ 2～3 に
            transform.position += new Vector3(off.x * k, off.y * k, 0f);
        }

        // カメラ設定も同期
        if (self)
        {
            self.orthographic      = source.orthographic;
            self.fieldOfView       = source.fieldOfView;
            self.nearClipPlane     = source.nearClipPlane;
            self.farClipPlane      = source.farClipPlane;
            self.orthographicSize  = source.orthographicSize;
        }
    }
}
