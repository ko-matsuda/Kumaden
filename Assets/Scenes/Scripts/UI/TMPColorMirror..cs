using UnityEngine;
using TMPro;

/// <summary>
/// Source の TextMeshProUGUI から「色/グラデ/アルファ/マテリアル」を
/// Target（=このオブジェクトのTMP）へコピーして“見た目をミラー”します。
/// Inspector で Source をドラッグするだけ。色コード入力は不要。
/// </summary>
[ExecuteAlways]
public class TMPColorMirror : MonoBehaviour
{
    [Header("色をコピーする元（ResultCanvas側の該当ラベルをドラッグ）")]
    public TextMeshProUGUI source;

    [Header("コピー先（未設定なら自分のTMPを自動取得）")]
    public TextMeshProUGUI target;

    [Header("オプション")]
    public bool liveSyncInEditor = true;  // エディタ上で変更を即反映
    public bool liveSyncInPlay   = true;  // 再生中も変更を追従
    public bool copyMaterial     = true;  // MaterialPresetも合わせたい場合ON

    void Reset()
    {
        target = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (!target) target = GetComponent<TextMeshProUGUI>();
        Apply();
    }

    void Update()
    {
        if (!source || !target) return;

        #if UNITY_EDITOR
        if (!Application.isPlaying && liveSyncInEditor) Apply();
        #endif

        if (Application.isPlaying && liveSyncInPlay) Apply();
    }

    [ContextMenu("Apply Now")]
    public void Apply()
    {
        if (!source || !target) return;

        // 基本色
        target.color = source.color;
        target.alpha = source.alpha;

        // 頂点グラデ
        target.enableVertexGradient = source.enableVertexGradient;
        if (source.enableVertexGradient)
        {
            target.colorGradient = source.colorGradient;
        }

        // マテリアル（フォントの描画設定ごと揃えたい場合）
        if (copyMaterial && source.fontSharedMaterial != null)
        {
            target.fontSharedMaterial = source.fontSharedMaterial;
        }

        // フォントアセットを揃えたい場合は以下を有効化
        // target.font = source.font;
        // target.fontStyle = source.fontStyle;
    }
}
