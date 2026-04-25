using UnityEngine;

public class FootPlant : MonoBehaviour
{
    [Header("State")]
    public bool isPlanted = false;
    public Transform currentFootHold;

    [Header("References")]
    public Transform hipPivot;

    [Header("Settings")]
    public float maxLegStretch = 1.8f;
    public float detachInputThreshold = 0.7f;
    public float minLockTime = 0.12f;

    [Header("External Input")]
    public bool useExternalInput = false;
    public Vector2 externalInput;

    [Header("Keyboard Input")]
    public KeyCode upKey;
    public KeyCode downKey;
    public KeyCode leftKey;
    public KeyCode rightKey;

    [Header("Debug")]
    public bool debugLog = false;

    private Transform candidateFootHold;
    private Collider2D candidateFootCollider;

    private float plantedTime = -999f;

    private enum FootHoldType
    {
        Normal,
        Long,
        Slippery
    }

    private FootHoldType candidateHoldType = FootHoldType.Normal;
    private FootHoldType currentHoldType = FootHoldType.Normal;

    private Collider2D currentHoldCollider;
    private Vector3 localFootPoint;

    void Update()
    {
        Vector2 inputDir = GetInputDirection();

        // 还没踩住时，碰到候选点就自动吸附
        if (candidateFootHold != null && candidateFootCollider != null && !isPlanted)
        {
            isPlanted = true;
            currentFootHold = candidateFootHold;
            currentHoldCollider = candidateFootCollider;
            currentHoldType = candidateHoldType;
            plantedTime = Time.time;

            if (debugLog)
            {
                Debug.Log($"{name} planted on {currentFootHold.name}, type = {currentHoldType}, collider = {currentHoldCollider.GetType().Name}");
            }

            if (currentHoldType == FootHoldType.Long || currentHoldType == FootHoldType.Slippery)
            {
                // 关键：直接用 OnTriggerEnter/Stay 碰到的 collider，不再 GetComponent<BoxCollider2D>
                Vector3 closestWorldPoint = currentHoldCollider.ClosestPoint(transform.position);
                localFootPoint = currentFootHold.InverseTransformPoint(closestWorldPoint);
            }
        }

        if (isPlanted && currentFootHold != null)
        {
            switch (currentHoldType)
            {
                case FootHoldType.Normal:
                    // 普通点仍然吸中心
                    transform.position = currentFootHold.position;
                    break;

                case FootHoldType.Long:
                case FootHoldType.Slippery:
                    if (currentHoldCollider == null)
                    {
                        ReleaseFoot();
                        return;
                    }

                    // 长点 / 斜边 / PolygonCollider2D：吸在实际碰到的位置
                    transform.position = currentFootHold.TransformPoint(localFootPoint);
                    break;
            }

            // 腿拉太长自动脱离
            if (hipPivot != null)
            {
                float dist = Vector2.Distance(hipPivot.position, transform.position);

                if (dist > maxLegStretch)
                {
                    if (debugLog)
                    {
                        Debug.Log($"{name} released because leg stretched too far. Distance = {dist}");
                    }

                    ReleaseFoot();
                    return;
                }
            }

            // 玩家明显想把脚收回来时自动脱离
            if (Time.time - plantedTime > minLockTime)
            {
                Vector2 footToHip = Vector2.zero;

                if (hipPivot != null)
                {
                    footToHip = ((Vector2)hipPivot.position - (Vector2)transform.position).normalized;
                }

                if (inputDir.magnitude > 0.2f)
                {
                    float dot = Vector2.Dot(inputDir.normalized, footToHip);

                    if (dot > detachInputThreshold)
                    {
                        if (debugLog)
                        {
                            Debug.Log($"{name} released by input. Dot = {dot}");
                        }

                        ReleaseFoot();
                        return;
                    }
                }
            }
        }
    }

    Vector2 GetInputDirection()
    {
        if (useExternalInput)
        {
            return externalInput;
        }

        Vector2 inputDir = Vector2.zero;

        if (Input.GetKey(upKey)) inputDir += Vector2.up;
        if (Input.GetKey(downKey)) inputDir += Vector2.down;
        if (Input.GetKey(leftKey)) inputDir += Vector2.left;
        if (Input.GetKey(rightKey)) inputDir += Vector2.right;

        return inputDir;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TrySetCandidate(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 防止脚一开始就在 collider 里面，Enter 没触发
        if (!isPlanted && candidateFootHold == null)
        {
            TrySetCandidate(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (candidateFootCollider == other)
        {
            candidateFootHold = null;
            candidateFootCollider = null;
            candidateHoldType = FootHoldType.Normal;

            if (debugLog)
            {
                Debug.Log($"{name} candidate cleared from {other.name}");
            }
        }
    }

    void TrySetCandidate(Collider2D other)
    {
        if (isPlanted) return;

        FootHoldType holdType;
        Transform holdRoot;

        if (!TryGetHoldInfo(other, out holdType, out holdRoot))
            return;

        candidateFootHold = holdRoot;
        candidateFootCollider = other;
        candidateHoldType = holdType;

        if (debugLog)
        {
            Debug.Log($"{name} found candidate {candidateFootHold.name}, type = {candidateHoldType}, collider = {other.GetType().Name}");
        }
    }

    bool TryGetHoldInfo(Collider2D other, out FootHoldType holdType, out Transform holdRoot)
    {
        holdType = FootHoldType.Normal;
        holdRoot = null;

        // 先检查 collider 自己
        if (other.CompareTag("HandHold"))
        {
            holdType = FootHoldType.Normal;
            holdRoot = other.transform;
            return true;
        }

        if (other.CompareTag("LongHandHold"))
        {
            holdType = FootHoldType.Long;
            holdRoot = other.transform;
            return true;
        }

        if (other.CompareTag("SlipperyHandHold"))
        {
            holdType = FootHoldType.Slippery;
            holdRoot = other.transform;
            return true;
        }

        // 再检查父物体，防止 collider 在子物体上，Tag 在父物体上
        Transform parent = other.transform.parent;

        while (parent != null)
        {
            if (parent.CompareTag("HandHold"))
            {
                holdType = FootHoldType.Normal;
                holdRoot = parent;
                return true;
            }

            if (parent.CompareTag("LongHandHold"))
            {
                holdType = FootHoldType.Long;
                holdRoot = parent;
                return true;
            }

            if (parent.CompareTag("SlipperyHandHold"))
            {
                holdType = FootHoldType.Slippery;
                holdRoot = parent;
                return true;
            }

            parent = parent.parent;
        }

        return false;
    }

    public void ReleaseFoot()
    {
        isPlanted = false;
        currentFootHold = null;
        currentHoldCollider = null;

        candidateFootHold = null;
        candidateFootCollider = null;

        candidateHoldType = FootHoldType.Normal;
        currentHoldType = FootHoldType.Normal;
    }
}