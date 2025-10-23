using UnityEngine;

[DisallowMultipleComponent]
public class OverlayKiller : MonoBehaviour
{
    [SerializeField] bool log = false;
    void Start()
    {
        var groups = FindObjectsOfType<CanvasGroup>(true);
        foreach (var g in groups)
        {
            if (g.blocksRaycasts || g.interactable)
            {
                if (log) Debug.Log($"[OverlayKiller] disable {g.name}");
                g.blocksRaycasts = false;
                g.interactable   = false;
            }
        }
    }
}
