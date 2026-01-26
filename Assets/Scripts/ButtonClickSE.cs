using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button), typeof(AudioSource))]
public class ButtonClickSE : MonoBehaviour
{
    [Header("クリック音")]
    public AudioClip clickSound;
    
    [Header("SE完了を待つ")]
    [Tooltip("trueの場合、SE再生完了後にボタンイベントを実行（シーン遷移ボタン用）")]
    public bool waitForSEComplete = false;
    
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
        
        if (waitForSEComplete)
        {
            // SE完了を待つ場合：元のOnClickを保存して、SE再生後に実行
            var originalOnClick = button.onClick;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => StartCoroutine(PlaySoundAndInvoke(originalOnClick)));
        }
        else
        {
            // 通常：SEを再生するだけ
            button.onClick.AddListener(PlayClickSound);
        }
    }

    void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    IEnumerator PlaySoundAndInvoke(Button.ButtonClickedEvent originalEvent)
    {
        if (isProcessing) yield break;
        isProcessing = true;
        
        // ボタンを一時的に無効化
        button.interactable = false;
        
        // SE再生
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
            // SEの長さ分待機
            yield return new WaitForSeconds(clickSound.length);
        }
        
        // 元のイベントを実行
        originalEvent?.Invoke();
        
        isProcessing = false;
    }
}