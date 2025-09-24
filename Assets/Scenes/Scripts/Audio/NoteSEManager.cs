using UnityEngine;

public class NoteSEManager : MonoBehaviour
{
    public static NoteSEManager Instance;

    public AudioClip sePerfect;
    public AudioClip seGood;

    private AudioSource audioSource;

    void Awake()
    {
        // ƒVƒ“ƒOƒ‹ƒgƒ“‰»
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // AudioSource ‚ðŽæ“¾
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayPerfect()
    {
        if (sePerfect != null)
            audioSource.PlayOneShot(sePerfect);
    }

    public void PlayGood()
    {
        if (seGood != null)
            audioSource.PlayOneShot(seGood);
    }
}
