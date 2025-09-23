// PlayerLaneMover.cs  ← 置き換え
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // 新Input System
#endif

public class PlayerLaneMover : MonoBehaviour
{
    [Header("レーンX座標（左→右の順）")]
    public float[] laneX = new float[] { -2.5f, 0f, 2.5f };

    [Header("開始レーン（0=左, 1=中, 2=右）")]
    public int currentLane = 1;

    [Header("移動スピード")]
    public float moveSpeed = 12f;

    [Header("入力設定")]
    [Tooltip("ON=画面をクリック/タップした場所に最も近いレーンへスナップ\nOFF=画面の左半分クリックで左/右半分クリックで右へ1段シフト")]
    public bool clickSnapToNearestLane = true;

    [Tooltip("明示的に指定しなければ Camera.main を使用")]
    public Camera cam;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 開始レーンへスナップ
        if (laneX != null && laneX.Length > 0)
        {
            currentLane = Mathf.Clamp(currentLane, 0, laneX.Length - 1);
            var p = transform.position;
            p.x = laneX[currentLane];
            transform.position = p;
        }
    }

    void Update()
    {
        // --- キー/パッドでも動く（お好みで） ---
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Shift(-1);
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Shift(+1);
        }
        var gp = Gamepad.current;
        if (gp != null)
        {
            if (gp.dpad.left.wasPressedThisFrame || gp.leftStick.left.wasPressedThisFrame) Shift(-1);
            if (gp.dpad.right.wasPressedThisFrame || gp.leftStick.right.wasPressedThisFrame) Shift(+1);
        }
#else
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Shift(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Shift(+1);
#endif

        // --- クリック/タップ ---
        if (TryGetPointerDown(out Vector2 screenPos))
        {
            // UIの上は無視
            if (!PointerIsOverUI())
            {
                if (clickSnapToNearestLane) SnapToNearestLane(screenPos);
                else HalfScreenShift(screenPos);
            }
        }

        // --- 目標Xへ補間 ---
        float targetX = laneX[Mathf.Clamp(currentLane, 0, laneX.Length - 1)];
        Vector3 pos = transform.position;
        float newX = targetX;  // 一発で目標レーンへ
        Vector3 newPos = new Vector3(newX, pos.y, pos.z);

        if (rb && rb.isKinematic) rb.MovePosition(newPos);
        else                      transform.position = newPos;
    }

    void Shift(int dir) // -1=左 / +1=右
    {
        currentLane = Mathf.Clamp(currentLane + dir, 0, laneX.Length - 1);
    }

    void HalfScreenShift(Vector2 screenPos)
    {
        float mid = Screen.width * 0.5f;
        if (screenPos.x < mid) Shift(-1);
        else                   Shift(+1);
    }

    void SnapToNearestLane(Vector2 screenPos)
    {
        var c = cam ? cam : Camera.main;
        if (c == null) { HalfScreenShift(screenPos); return; }

        // プレイヤーのZ位置の平面にスクリーン座標を投影してXを得る
        var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
#if ENABLE_INPUT_SYSTEM
        Vector3 wp;
        {
            var ray = c.ScreenPointToRay(screenPos);
            if (!plane.Raycast(ray, out float enter)) return;
            wp = ray.GetPoint(enter);
        }
#else
        var ray = c.ScreenPointToRay(screenPos);
        if (!plane.Raycast(ray, out float enter)) return;
        Vector3 wp = ray.GetPoint(enter);
#endif
        float clickX = wp.x;

        // 最も近いレーンを選ぶ
        int best = 0;
        float bestDist = float.PositiveInfinity;
        for (int i = 0; i < laneX.Length; i++)
        {
            float d = Mathf.Abs(laneX[i] - clickX);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        currentLane = best;
    }

    bool TryGetPointerDown(out Vector2 screenPos)
    {
        // 新Input System
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPos = mouse.position.ReadValue();
            return true;
        }
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = ts.primaryTouch.position.ReadValue();
            return true;
        }
        screenPos = default;
        return false;
#else
        // 旧Input System
        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            screenPos = Input.GetTouch(0).position;
            return true;
        }
        screenPos = default;
        return false;
#endif
    }

    bool PointerIsOverUI()
    {
        if (EventSystem.current == null) return false;
#if ENABLE_INPUT_SYSTEM
        // タッチがあるときはそのfingerIdで判定
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed)
        {
            // Input Systemでは fingerId が取れないため簡易判定のみ
            return EventSystem.current.IsPointerOverGameObject();
        }
        return EventSystem.current.IsPointerOverGameObject();
#else
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return EventSystem.current.IsPointerOverGameObject();
#endif
    }
}
