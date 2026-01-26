using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Button), typeof(AudioSource))]
public class RetryButtonWithSE : MonoBehaviour
{
    [Header("クリック音")]
    public AudioClip clickSound;
    
    [Header("シーン設定")]
    [Tooltip("空の場合は現在のシーンをリロード")]
    public string sceneToLoad = "";
    
    private AudioSource audioSource;
    private Button button;
    private bool isProcessing = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        button = GetComponent<Button>();
        
        // AudioSourceの設定
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1.0f;
        
        // ボタンの元のOnClickを全てクリアして、独自の処理を設定
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnRetryButtonClick);
    }

    void OnRetryButtonClick()
    {
        if (!isProcessing)
        {
            StartCoroutine(PlaySEAndLoadScene());
        }
    }

    IEnumerator PlaySEAndLoadScene()
    {
        isProcessing = true;
        button.interactable = false;
        
        // SE再生
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
            Debug.Log($"[RetryButtonWithSE] SE再生開始: {clickSound.length}秒");
            
            // SEの長さ分待機
            yield return new WaitForSeconds(clickSound.length);
        }
        
        Debug.Log("[RetryButtonWithSE] シーンロード開始");
        
        // シーンロード
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            // 現在のシーンをリロード
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}