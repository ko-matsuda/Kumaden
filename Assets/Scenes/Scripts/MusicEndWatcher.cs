using UnityEngine;
using UnityEngine.Events;

public class MusicEndWatcher : MonoBehaviour
{
    public AudioSource bgm;
    public float verse1EndSec = 37.5f;
    public UnityEvent OnVerse1End;

    bool fired;
    void Update(){
        if (!bgm || fired) return;
        if (bgm.isPlaying && bgm.time >= verse1EndSec) {
            fired = true;
            OnVerse1End?.Invoke();
        }
    }
}
