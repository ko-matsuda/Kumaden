// PlayerLaneMover.cs  �� �u������
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // �VInput System
#endif

public class PlayerLaneMover : MonoBehaviour
{
    [Header("���[��X���W�i�����E�̏��j")]
    public float[] laneX = new float[] { -2.5f, 0f, 2.5f };

    [Header("�J�n���[���i0=��, 1=��, 2=�E�j")]
    public int currentLane = 1;

    [Header("�ړ��X�s�[�h")]
    public float moveSpeed = 12f;

    [Header("���͐ݒ�")]
    [Tooltip("ON=��ʂ�N���b�N/�^�b�v�����ꏊ�ɍł�߂����[���փX�i�b�v\nOFF=��ʂ̍������N���b�N�ō�/�E�����N���b�N�ŉE��1�i�V�t�g")]
    public bool clickSnapToNearestLane = true;

    [Tooltip("�����I�Ɏw�肵�Ȃ���� Camera.main ��g�p")]
    public Camera cam;

    Rigidbody rb;
    // タッチ追跡（フレーム落ち対策）
    private System.Collections.Generic.HashSet<int> _processedFingerIds = new System.Collections.Generic.HashSet<int>();


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // �J�n���[���փX�i�b�v
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
        // --- �L�[/�p�b�h�ł�����i���D�݂Łj ---
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

        // --- �N���b�N/�^�b�v ---
        if (TryGetPointerDown(out Vector2 screenPos))
        {
            // UI�̏�͖���
            if (!PointerIsOverUI())
            {
                if (clickSnapToNearestLane) SnapToNearestLane(screenPos);
                else HalfScreenShift(screenPos);
            }
        }

        // --- �ڕWX�֕�� ---
        float targetX = laneX[Mathf.Clamp(currentLane, 0, laneX.Length - 1)];
        Vector3 pos = transform.position;
        float newX = targetX;  // �ꔭ�ŖڕW���[����
        Vector3 newPos = new Vector3(newX, pos.y, pos.z);

        if (rb && rb.isKinematic) rb.MovePosition(newPos);
        else                      transform.position = newPos;
    }

    void Shift(int dir) // -1=�� / +1=�E
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

        // �v���C���[��Z�ʒu�̕��ʂɃX�N���[�����W�𓊉e����X�𓾂�
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

        // �ł�߂����[����I��
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
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPos = mouse.position.ReadValue();
            return true;
        }
        var ts = Touchscreen.current;
        if (ts != null)
        {
            foreach (var touch in ts.touches)
            {
                int fid = touch.touchId.ReadValue();
                // フェーズを問わず「まだ処理していない指」を捕捉
                if (!_processedFingerIds.Contains(fid))
                {
                    _processedFingerIds.Add(fid);
                    screenPos = touch.position.ReadValue();
                    return true;
                }
                // 指が離れたら追跡解除
                if (!touch.press.isPressed)
                    _processedFingerIds.Remove(fid);
            }
        }
        screenPos = default;
        return false;
#else
        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }
        // 全フェーズ対応：まだ処理していない指はフェーズ不問で必ず捕捉
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (!_processedFingerIds.Contains(t.fingerId))
            {
                // Began/Moved/Stationary/Ended全て対応
                // 重いフレームでBeganが消えてEndedになっていても捕捉できる
                _processedFingerIds.Add(t.fingerId);
                screenPos = t.position;
                return true;
            }
            // 指が離れたら追跡解除
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                _processedFingerIds.Remove(t.fingerId);
        }
        screenPos = default;
        return false;
#endif
    }

bool PointerIsOverUI()
    {
        if (EventSystem.current == null) return false;

        // レイキャストでUIをチェックするが、
        // パーティクルのCanvasRendererがブロックしている場合は無視する
        // シンプルに: IsPointerOverGameObjectを使わず、常にfalse返す
        // （UIボタン自体はButtonコンポーネントのクリックで另途処理される）
        return false;
    }
}
