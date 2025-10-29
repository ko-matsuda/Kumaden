// そのままコピペしてください。
// もしコンパイルエラーで「namespace Kuma が無い」などが出たら、
// 最初の 'namespace Kuma {' と最後の '}' を削除するだけでOKです。

using UnityEngine;

namespace Kuma
{
    // ノーツAとノーツBを1本の帯(WorldRibbon)でつなぐだけの超シンプルな橋渡し
    public class LinkedHoldNote : MonoBehaviour
    {
        [Header("どの帯を使うか（WorldRibbon の Prefab）")]
        public WorldRibbon ribbonPrefab;

        [Header("自動で子から探す（Start/End を手で入れなくてOK）")]
        public bool autoFindNotes = true;

        [Header("手で指定したい場合だけ使う")]
        public NoteBehaviour startNote;
        public NoteBehaviour endNote;

        private WorldRibbon ribbon;

        void Awake()
        {
            // ノーツを自動検出：子にある NoteBehaviour を2つ拾う
            if (autoFindNotes || startNote == null || endNote == null)
            {
                var notes = GetComponentsInChildren<NoteBehaviour>(true);
                if (notes != null && notes.Length >= 2)
                {
                    // Zが小さい方をStart、大きい方をEndにするだけ（簡単ルール）
                    NoteBehaviour a = notes[0];
                    NoteBehaviour b = notes[1];
                    if (notes.Length > 2)
                    {
                        // いちおう一番手前＆一番奥を選ぶ
                        a = notes[0];
                        b = notes[0];
                        foreach (var n in notes)
                        {
                            if (n.transform.localPosition.z < a.transform.localPosition.z) a = n;
                            if (n.transform.localPosition.z > b.transform.localPosition.z) b = n;
                        }
                    }
                    startNote = a;
                    endNote = b;
                }
            }

            // 帯を生成
            if (ribbonPrefab != null)
            {
                ribbon = Instantiate(ribbonPrefab, transform);
                ribbon.SetExpose01(1f); // 最初から見える
            }
        }

        void Update()
        {
            if (ribbon == null || startNote == null || endNote == null) return;

            // 2つのノーツ位置を常に結ぶ
            ribbon.SetEndpoints(startNote.transform.position, endNote.transform.position);
            ribbon.SetExpose01(1f); // 常時表示（必要ならここを条件で切り替え）
        }

        // 帯を隠したい時に外部から呼べる
        public void Hide()
        {
            if (ribbon != null) ribbon.SetExpose01(0f);
        }
    }
}
