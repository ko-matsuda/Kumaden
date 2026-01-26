using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(AudioSource))]
public class ButtonClickSE : MonoBehaviour
{
    [Header("クリック音")]
    public AudioClip clickSound;
    
    private AudioSource audioSource;
    private Button button;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        button = GetComponent<Button>();
        
        // AudioSourceの設定
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1.0f;
        
        // ボタンクリック時のイベントにSE再生を追加
        button.onClick.AddListener(PlayClickSound);
    }

    void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}