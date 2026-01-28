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
        Debug.Log($"[ButtonClickSE] Awake on {gameObject.name}");
        
        audioSource = GetComponent<AudioSource>();
        button = GetComponent<Button>();
        
        if (audioSource == null)
        {
            Debug.LogError($"[ButtonClickSE] AudioSource not found on {gameObject.name}");
            return;
        }
        
        if (button == null)
        {
            Debug.LogError($"[ButtonClickSE] Button not found on {gameObject.name}");
            return;
        }
        
        // AudioSourceの設定
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1.0f;
        
        // ボタンクリック時のイベントにSE再生を追加
        button.onClick.AddListener(PlayClickSound);
        
        Debug.Log($"[ButtonClickSE] Setup complete. clickSound={(clickSound != null ? clickSound.name : "null")}");
    }

    void PlayClickSound()
    {
        Debug.Log($"[ButtonClickSE] PlayClickSound called on {gameObject.name}");
        
        if (clickSound == null)
        {
            Debug.LogWarning($"[ButtonClickSE] clickSound is null on {gameObject.name}");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogWarning($"[ButtonClickSE] audioSource is null on {gameObject.name}");
            return;
        }
        
        Debug.Log($"[ButtonClickSE] Playing sound: {clickSound.name}");
        audioSource.PlayOneShot(clickSound);
    }
}