using UnityEngine;

public class HandGrip : MonoBehaviour
{
    [Header("Keyboard Grip")]
    public KeyCode gripKey = KeyCode.Space;

    [Header("External Grip")]
    public bool useExternalGrip = false;
    public bool externalGripHeld = false;

    [Header("State")]
    public bool isGripping = false;
    public Transform currentHold;

    [Header("Stamina")]
    public float maxStamina = 10f;
    public float currentStamina = 10f;
    public float drainPerSecond = 1f;
    public float staminaDrainMultiplier = 1f;

    private Transform candidateHold;

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        bool gripHeld = useExternalGrip ? externalGripHeld : Input.GetKey(gripKey);

        if (gripHeld && candidateHold != null && !isGripping)
        {
            isGripping = true;
            currentHold = candidateHold;
        }

        if (isGripping && currentHold != null)
        {
            currentStamina -= drainPerSecond * staminaDrainMultiplier * Time.deltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                ReleaseGrip();
                return;
            }

            transform.position = currentHold.position;
        }

        if (!gripHeld && isGripping)
        {
            ReleaseGrip();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HandHold"))
        {
            candidateHold = other.transform;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("HandHold") && candidateHold == other.transform)
        {
            candidateHold = null;
        }
    }

    public void ReleaseGrip()
    {
        isGripping = false;
        currentHold = null;
    }
}