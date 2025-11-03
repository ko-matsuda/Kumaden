using UnityEngine;
using TMPro;

/// <summary>
/// UnityEvent<int> を TextMeshPro に安全に流し込むための超シンプルなアダプタ。
/// ・フォーマット文字列に {0} / {0:00} などを書くだけ
/// ・SetText のオーバーロードを選ぶ必要なし
/// </summary>
public sealed class TMPIntBinder : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private string format = "{0}"; // 例: "COMBO {0:00}" / "{0:00}"

    /// <summary>UnityEvent<int> の受け口。これだけに配線してください。</summary>
    public void Apply(int value)
    {
        if (target == null) return;
        // string.Format は {0:00} などの標準書式に対応
        target.text = string.Format(format, value);
    }

    // 便利：インスペクタで参照を自動補完（なければ自分のTMP_Textを拾う）
    private void Reset()
    {
        if (target == null) target = GetComponent<TMP_Text>();
    }
}
