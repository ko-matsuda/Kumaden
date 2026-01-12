using UnityEngine;
using System.Collections;

/// <summary>
/// フィーバーモード管理
/// コンボ30以上で発動、MISS時に終了
/// 演出は画面両サイド+パーティクル+鈴SE、判定・ノーツ速度は変更しない
/// </summary>
public class FeverManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int feverThreshold = 30;
    [SerializeField] private float scoreMultiplier = 1.2f;

    [Header("UI演出")]
    [SerializeField] private GameObject leftSideEffect;
    [SerializeField] private GameObject rightSideEffect;
    
    [Header("パーティクル")]
    [SerializeField] private ParticleSystem leftSparkles;
    [SerializeField] private ParticleSystem rightSparkles;
    [SerializeField] private ParticleSystem feverStars;
    [SerializeField] private ParticleSystem playerAura;
    
    [Header("サウンド")]
    [SerializeField] private AudioSource feverSE; // 鈴系ループSE
    [SerializeField, Range(0.3f, 1.0f)] private float fadeOutDuration = 0.5f;

    // 状態
    private bool isFever = false;
    private ScoreManagerLite scoreManager;
    
    public bool IsFever => isFever;
    public float CurrentMultiplier => isFever ? scoreMultiplier : 1.0f;
    
    private void Start()
    {
        scoreManager = ScoreManagerLite.Instance;
        
        if (scoreManager == null)
        {
            Debug.LogError("[FeverManager] ScoreManagerLite.Instance not found!");
        }
        
        // 初期状態：全OFF
        SetEffectsActive(false);
        
        if (feverSE != null)
        {
            feverSE.loop = true;
            feverSE.Stop();
        }
    }
    
    private void Update()
    {
        if (scoreManager == null) return;
        
        int currentCombo = GetCurrentComboFromUI();
        bool shouldBeFever = currentCombo >= feverThreshold;
        
        // 状態変化時のみ処理
        if (shouldBeFever && !isFever)
        {
            EnterFever();
        }
        else if (!shouldBeFever && isFever)
        {
            ExitFever();
        }
    }
    
    // UIテキストからコンボ数を取得（暫定）
    private int GetCurrentComboFromUI()
    {
        if (scoreManager == null || scoreManager.comboText == null) return 0;
        
        string comboStr = scoreManager.comboText.text;
        if (string.IsNullOrEmpty(comboStr)) return 0;
        
        // "COMBO 30" -> 30
        string[] parts = comboStr.Split(' ');
        if (parts.Length >= 2 && int.TryParse(parts[1], out int combo))
        {
            return combo;
        }
        
        return 0;
    }
    
    private void EnterFever()
    {
        isFever = true;
        SetEffectsActive(true);
        
        // SE再生（既に再生中なら何もしない）
        if (feverSE != null && !feverSE.isPlaying)
        {
            feverSE.Play();
        }
        
        Debug.Log("[Fever] ★FEVER MODE START★");
    }
    
    private void ExitFever()
    {
        isFever = false;
        SetEffectsActive(false);
        
        // SEフェードアウト停止
        if (feverSE != null && feverSE.isPlaying)
        {
            StartCoroutine(FadeOutSE());
        }
        
        Debug.Log("[Fever] Exited");
    }
    
    private void SetEffectsActive(bool active)
    {
        // 両サイド演出ON/OFF
        if (leftSideEffect != null)
            leftSideEffect.SetActive(active);
        if (rightSideEffect != null)
            rightSideEffect.SetActive(active);
        
        // パーティクル制御（GameObjectで制御）
        if (leftSparkles != null)
        {
            if (active)
            {
                leftSparkles.gameObject.SetActive(true);
                leftSparkles.Play();
            }
            else
            {
                leftSparkles.Stop();
                leftSparkles.gameObject.SetActive(false);
            }
        }
        
        if (rightSparkles != null)
        {
            if (active)
            {
                rightSparkles.gameObject.SetActive(true);
                rightSparkles.Play();
            }
            else
            {
                rightSparkles.Stop();
                rightSparkles.gameObject.SetActive(false);
            }
        }
        
        if (feverStars != null)
        {
            if (active)
            {
                feverStars.gameObject.SetActive(true);
                feverStars.Play();
            }
            else
            {
                feverStars.Stop();
                feverStars.gameObject.SetActive(false);
            }
        }
        
        if (playerAura != null)
        {
            if (active)
            {
                playerAura.gameObject.SetActive(true);
                playerAura.Play();
            }
            else
            {
                playerAura.Stop();
                playerAura.gameObject.SetActive(false);
            }
        }
    }
    
    private IEnumerator FadeOutSE()
    {
        float startVolume = feverSE.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            feverSE.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        feverSE.Stop();
        feverSE.volume = startVolume; // 音量を元に戻す
    }
    
    // ステージ終了時の強制終了
    public void OnStageEnd()
    {
        if (isFever)
        {
            ExitFever();
        }
    }
}
