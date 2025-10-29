using UnityEngine;

public class NoteToRibbonRelay : MonoBehaviour
{
    public WorldRibbon ribbon;        // 紐付け対象（WorldRibbon）
    public Transform player;          // プレイヤーTransform（PlayerRootなど）
    public NoteBehaviour note;        // このNote自身

    private bool active = false;

    void Update()
    {
        if (!active || ribbon == null || player == null || note == null)
            return;

        // 帯の両端を更新（ノートとプレイヤーの間）
        ribbon.SetEndpoints(note.transform.position, player.position);
    }

    // ノート拾い開始
    public void BeginHold(NoteBehaviour nb)
    {
        note = nb;
        active = true;
        if (ribbon != null)
        {
            ribbon.gameObject.SetActive(true);
            ribbon.SetExpose(1f);
        }
    }

    // ノート保持中
    public void HoldTick(NoteBehaviour nb)
    {
        if (active && ribbon != null)
        {
            ribbon.SetExpose(1f);
        }
    }

    // ノート離し
    public void EndHold(NoteBehaviour nb)
    {
        active = false;
        if (ribbon != null)
        {
            ribbon.SetExpose(0f);
            ribbon.gameObject.SetActive(false);
        }
    }
}
