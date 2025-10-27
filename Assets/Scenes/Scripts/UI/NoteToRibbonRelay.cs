using UnityEngine;
using System.Reflection;

namespace Kuma
{
    public class NoteToRibbonRelay : MonoBehaviour
    {
        [Header("Refs")]
        public Transform player;         // PlayerRoot
        public WorldRibbon ribbon;       // WorldRibbon_*（対応レーン）
        
        [Header("Tune")]
        public float y = 0.05f;          // 帯の高さ
        public float halfLength = 0.6f;  // プレイヤー前後に伸ばす半分の長さ

        [Header("Fallback")]
        [Tooltip("Note からレーンが取れないときに使う。-1=自動取得")]
        public int forcedLane = -1;      // 0=L,1=M,2=R

        // ==== UnityEvent 受け口（必ず NoteBehaviour 引数）====
        public void BeginHold(object note) { if (!ribbon || !player) return; ribbon.SetExpose01(1f); UpdateLine(note); }
        public void HoldTick (object note) { if (!ribbon || !player) return; UpdateLine(note); }
        public void EndHold  (object note) { if (!ribbon) return; ribbon.SetExpose01(0f); ribbon.Clear(); }

        void UpdateLine(object noteObj)
        {
            int lane = GetLaneIndexFrom(noteObj);
            float x = LaneToX(lane);
            float z0 = player.position.z - halfLength;
            float z1 = player.position.z + halfLength;
            ribbon.SetEndpoints(new Vector3(x, y, z0), new Vector3(x, y, z1));
        }

        int GetLaneIndexFrom(object noteObj)
        {
            if (forcedLane >= 0) return forcedLane;

            if (noteObj == null) return 1; // M

            var t = noteObj.GetType();

            // Property/Field いろいろ試す（LaneIndex / laneIndex / lane / Lane など）
            string[] names = { "LaneIndex", "laneIndex", "lane", "Lane" };
            foreach (var n in names)
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null && p.PropertyType == typeof(int)) return Mathf.Clamp((int)p.GetValue(noteObj), 0, 2);

                var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null && f.FieldType == typeof(int)) return Mathf.Clamp((int)f.GetValue(noteObj), 0, 2);
            }
            return 1; // 取れなければ真ん中
        }

        float LaneToX(int lane)
        {
            switch (lane)
            {
                case 0: return -1.6f; // L
                case 2: return  1.6f; // R
                default: return 0f;   // M
            }
        }
    }
}
