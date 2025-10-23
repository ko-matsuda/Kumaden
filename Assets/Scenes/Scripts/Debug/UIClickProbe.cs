using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIClickProbe : MonoBehaviour
{
    [Header("ResultCanvas の GraphicRaycaster を入れる")]
    public GraphicRaycaster raycaster;   // ← ResultCanvas の GraphicRaycaster をドラッグ

    PointerEventData _ped;
    readonly List<RaycastResult> _hits = new List<RaycastResult>();

    void Awake()
    {
        if (EventSystem.current == null)
            Debug.LogError("[UIClickProbe] EventSystem がシーンにありません。UI は反応しません。");
        else
            Debug.Log("[UIClickProbe] EventSystem 検出 OK");

        // 念のため停止状態を解除（停止してるとボタンが実行されない）
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (raycaster == null)
            {
                Debug.LogError("[UIClickProbe] raycaster が未設定。ResultCanvas の GraphicRaycaster を割り当ててください。");
                return;
            }

            _hits.Clear();
            _ped = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            raycaster.Raycast(_ped, _hits);

            var sb = new StringBuilder();
            sb.AppendLine("=== [UIClickProbe] Click Raycast Results (上が最前面) ===");
            if (_hits.Count == 0)
            {
                sb.AppendLine("ヒットなし → GraphicRaycaster 不在 / 画面外 / BlocksRaycasts=OFF の可能性");
                Debug.Log(sb.ToString());
                return;
            }

            for (int i = 0; i < _hits.Count; i++)
            {
                var go = _hits[i].gameObject;
                var img = go.GetComponent<Graphic>();
                bool rt = img ? img.raycastTarget : false;

                // 親の CanvasGroup でブロックされていないか可視化
                var cgInfo = GetCanvasGroupState(go);

                sb.AppendLine($"{i + 1}. {GetPath(go)}" +
                              $"  [RaycastTarget:{rt}]  [CanvasGroup:{cgInfo}]");
            }

            // 先頭（実際にレイを奪っているUI）がボタンでないなら犯人確定
            var top = _hits[0].gameObject;
            var btn = top.GetComponentInParent<Button>();
            if (btn == null)
            {
                sb.AppendLine($"→ 最前面は Button ではありません：{GetPath(top)}");
                sb.AppendLine("  これがクリックを横取りしています。Raycast Target を OFF にしてください。");
            }
            else
            {
                // Button なのに反応しない場合、Interactable / 親 CanvasGroup の状態を出す
                bool interactable = btn.interactable;
                string cgState = GetCanvasGroupState(btn.gameObject);
                sb.AppendLine($"→ Button 検出：{GetPath(btn.gameObject)}  [interactable:{interactable}]  [CanvasGroup:{cgState}]");
                if (!interactable) sb.AppendLine("  Button.interactable = false → ON にしてください");
            }

            Debug.Log(sb.ToString());
        }
    }

    static string GetPath(GameObject go)
    {
        var path = go.name;
        var p = go.transform.parent;
        while (p != null)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }
        return path;
    }

    static string GetCanvasGroupState(GameObject go)
    {
        var list = go.GetComponentsInParent<CanvasGroup>(true);
        if (list == null || list.Length == 0) return "なし";
        var sb = new StringBuilder();
        for (int i = 0; i < list.Length; i++)
        {
            var cg = list[i];
            sb.Append($"[{cg.name}: interactable={cg.interactable}, blocks={cg.blocksRaycasts}, alpha={cg.alpha:0.##}]");
            if (i != list.Length - 1) sb.Append(" > ");
        }
        return sb.ToString();
    }
}
