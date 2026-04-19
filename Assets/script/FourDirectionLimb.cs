using UnityEngine;

public class FourDirectionLimb : MonoBehaviour
{
    [Header("References")]
    public Transform pivot;
    public Transform endPoint;
    public HandGrip handGrip;
    public FootPlant footPlant;

    [Header("Limb Settings")]
    public float length = 1.2f;
    public float rotateSpeed = 360f;

    [Header("Start Pose")]
    public float startAngle = 0f;
    public float currentAngle = 0f;

    [Header("Rest / Sag")]
    public bool useRestAngle = false;     // 只给腿开，手关掉
    public float restAngle = -90f;
    public float restReturnSpeed = 180f;

    [Header("Keyboard Input")]
    public KeyCode upKey;
    public KeyCode downKey;
    public KeyCode leftKey;
    public KeyCode rightKey;

    [Header("External Input")]
    public bool useExternalInput = false;
    public Vector2 externalInput;

    [Header("Visual")]
    public float thickness = 0.2f;
    public float rotationOffset = -90f;
    public float inputDeadZone = 0.2f;

    void Start()
    {
        currentAngle = startAngle;
        SnapEndPointToCurrentAngle();
        UpdateLimbVisual(false);
    }

    void Update()
    {
        if (pivot == null || endPoint == null) return;

        bool handLocked = handGrip != null && handGrip.isGripping && handGrip.currentHold != null;
        bool footLocked = footPlant != null && footPlant.isPlanted && footPlant.currentFootHold != null;
        bool locked = handLocked || footLocked;

        Vector2 inputDir = GetInputDirection();
        bool hasInput = inputDir.magnitude > inputDeadZone;

        if (!locked)
        {
            if (hasInput)
            {
                HandleInputAndMoveEndPoint(inputDir);
            }
            else if (useRestAngle)
            {
                currentAngle = Mathf.MoveTowardsAngle(
                    currentAngle,
                    restAngle,
                    restReturnSpeed * Time.deltaTime
                );

                SnapEndPointToCurrentAngle();
            }
            else
            {
                // 没输入也没下垂时，保持当前角度，但要跟着 pivot 走
                SnapEndPointToCurrentAngle();
            }
        }

        UpdateLimbVisual(locked);
    }

    Vector2 GetInputDirection()
    {
        Vector2 inputDir = Vector2.zero;

        // 手柄输入
        if (useExternalInput && externalInput.magnitude > inputDeadZone)
        {
            inputDir += externalInput;
        }

        // 键盘输入仍然保留
        if (Input.GetKey(upKey)) inputDir += Vector2.up;
        if (Input.GetKey(downKey)) inputDir += Vector2.down;
        if (Input.GetKey(leftKey)) inputDir += Vector2.left;
        if (Input.GetKey(rightKey)) inputDir += Vector2.right;

        return inputDir;
    }

    void HandleInputAndMoveEndPoint(Vector2 inputDir)
    {
        inputDir.Normalize();

        float targetAngle = Mathf.Atan2(inputDir.y, inputDir.x) * Mathf.Rad2Deg;

        currentAngle = Mathf.MoveTowardsAngle(
            currentAngle,
            targetAngle,
            rotateSpeed * Time.deltaTime
        );

        SnapEndPointToCurrentAngle();
    }

    void SnapEndPointToCurrentAngle()
    {
        if (pivot == null || endPoint == null) return;

        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

        endPoint.position = pivot.position + dir * length;
    }

    void UpdateLimbVisual(bool locked)
    {
        if (pivot == null || endPoint == null) return;

        Vector3 dir = endPoint.position - pivot.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        float actualLength = dir.magnitude;
        Vector3 normalizedDir = dir.normalized;

        transform.position = pivot.position + normalizedDir * (actualLength * 0.5f);

        float visualAngle = Mathf.Atan2(normalizedDir.y, normalizedDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, visualAngle + rotationOffset);

        transform.localScale = new Vector3(thickness, actualLength * 0.5f, 1f);

        // 只有锁住时，才反向同步 currentAngle，避免自由状态下抖动
        if (locked)
        {
            currentAngle = Mathf.Atan2(normalizedDir.y, normalizedDir.x) * Mathf.Rad2Deg;
        }
    }
}