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

    private Transform candidateFootHold;
    private float plantedTime = -999f;

    private enum FootHoldType
    {
        Normal,
        Long,
        Slippery
    }

    private FootHoldType candidateHoldType = FootHoldType.Normal;
    private FootHoldType currentHoldType = FootHoldType.Normal;

    private BoxCollider2D currentBoxHold;
    private Vector3 localFootPoint;

    void Update()
    {
        Vector2 inputDir = GetInputDirection();

        // 还没踩住时，碰到候选点就自动吸附
        if (candidateFootHold != null && !isPlanted)
        {
            isPlanted = true;
            currentFootHold = candidateFootHold;
            currentHoldType = candidateHoldType;
            plantedTime = Time.time;

            if (currentHoldType == FootHoldType.Long || currentHoldType == FootHoldType.Slippery)
            {
                currentBoxHold = currentFootHold.GetComponent<BoxCollider2D>();

                if (currentBoxHold != null)
                {
                    // 关键：记录脚碰到表面的最近位置，而不是中心点
                    Vector3 closestWorldPoint = currentBoxHold.ClosestPoint(transform.position);
                    localFootPoint = currentFootHold.InverseTransformPoint(closestWorldPoint);
                }
                else
                {
                    currentHoldType = FootHoldType.Normal;
                    currentBoxHold = null;
                }
            }
            else
            {
                currentBoxHold = null;
            }
        }

        if (isPlanted && currentFootHold != null)
        {
            // 不同点类型，不同锁定方式
            switch (currentHoldType)
            {
                case FootHoldType.Normal:
                    transform.position = currentFootHold.position;
                    break;

                case FootHoldType.Long:
                case FootHoldType.Slippery:
                    if (currentBoxHold == null)
                    {
                        ReleaseFoot();
                        return;
                    }

                    transform.position = currentFootHold.TransformPoint(localFootPoint);
                    break;
            }

            // 腿拉太长自动脱离
            if (hipPivot != null)
            {
                float dist = Vector2.Distance(hipPivot.position, transform.position);
                if (dist > maxLegStretch)
                {
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
        if (other.CompareTag("HandHold"))
        {
            candidateFootHold = other.transform;
            candidateHoldType = FootHoldType.Normal;
        }
        else if (other.CompareTag("LongHandHold"))
        {
            candidateFootHold = other.transform;
            candidateHoldType = FootHoldType.Long;
        }
        else if (other.CompareTag("SlipperyHandHold"))
        {
            candidateFootHold = other.transform;
            candidateHoldType = FootHoldType.Slippery;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if ((other.CompareTag("HandHold") ||
             other.CompareTag("LongHandHold") ||
             other.CompareTag("SlipperyHandHold")) &&
            candidateFootHold == other.transform)
        {
            candidateFootHold = null;
            candidateHoldType = FootHoldType.Normal;
        }
    }

    public void ReleaseFoot()
    {
        isPlanted = false;
        currentFootHold = null;
        candidateFootHold = null;
        candidateHoldType = FootHoldType.Normal;
        currentHoldType = FootHoldType.Normal;
        currentBoxHold = null;
    }
}