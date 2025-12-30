using UnityEngine;

public class ClearPlayerPrefs : MonoBehaviour
{
    void Start()
    {
        PlayerPrefs.DeleteKey("HasSeenPrologue");
        PlayerPrefs.Save();
        Debug.Log("[ClearPlayerPrefs] HasSeenPrologue cleared!");
        
        // 自動削除
        Destroy(gameObject);
    }
}
