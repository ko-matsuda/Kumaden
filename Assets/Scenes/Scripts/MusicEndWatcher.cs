using UnityEngine;
using UnityEngine.Events;

public class MusicEndWatcher : MonoBehaviour
{
    public AudioSource bgm;
    public float verse1EndSec = 37.5f;
    public UnityEvent OnVerse1End;
    
    [Header("Auto Trigger (UnityEvent の代わり)")]
    public CookingResultSequence cookingResultSequence;

    bool fired;
    
    void Start()
    {
        // 自動で CookingResultSequence を探す
        if (cookingResultSequence == null)
        {
            cookingResultSequence = FindObjectOfType<CookingResultSequence>();
        }
    }
    
    void Update()
    {
        if (!bgm || fired) return;
        if (bgm.isPlaying && bgm.time >= verse1EndSec)
        {
            fired = true;
            
            // UnityEvent を呼ぶ
            OnVerse1End?.Invoke();
            
            // 自動で CookingResultSequence を呼ぶ（バックアップ）
            if (cookingResultSequence != null)
            {
                Debug.Log("[MusicEndWatcher] Triggering CookingResultSequence");
                cookingResultSequence.NotifyGameEnded();
            }
        }
    }


public void ResetForNewSong()
    {
        fired = false;
        Debug.Log("[MusicEndWatcher] Reset for new song");
    }
}