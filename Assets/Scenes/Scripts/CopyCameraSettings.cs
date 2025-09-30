using UnityEngine;

[ExecuteAlways]
public class CopyCameraSettings : MonoBehaviour
{
    public Camera source;          // WorldCam ‚ð‚±‚±‚É
    private Camera self;

    void Awake() { self = GetComponent<Camera>(); }

    void LateUpdate()
    {
        if (!source) return;
        transform.position = source.transform.position;
        transform.rotation = source.transform.rotation;

        self.orthographic   = source.orthographic;
        self.fieldOfView    = source.fieldOfView;
        self.nearClipPlane  = source.nearClipPlane;
        self.farClipPlane   = source.farClipPlane;
        self.orthographicSize = source.orthographicSize;
    }
}
