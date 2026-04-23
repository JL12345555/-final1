using UnityEngine;

public class ClimberPhysicsGripMotor : MonoBehaviour
{
    [Header("Body")]
    public Rigidbody2D bodyRb;

    [Header("Shoulder Pivots")]
    public Transform leftShoulderPivot;
    public Transform rightShoulderPivot;

    [Header("Hand Grips")]
    public HandGrip leftHandGrip;
    public HandGrip rightHandGrip;

    [Header("Grip Joints")]
    public DistanceJoint2D leftJoint;
    public DistanceJoint2D rightJoint;

    [Header("Foot Plants")]
    public FootPlant leftFootPlant;
    public FootPlant rightFootPlant;

    [Header("Gamepad Input Sources")]
    public LeftSideGamepadInput leftGamepadInput;
    public RightSideGamepadInput rightGamepadInput;

    [Header("Arm Lengths")]
    public float leftArmLength = 1.5f;
    public float rightArmLength = 1.5f;

    [Header("Forces")]
    public float handPullForce = 18f;
    public float footPushForce = 10f;
    public float maxVelocity = 8f;

    [Header("Input Dead Zone")]
    public float inputDeadZone = 0.2f;

    [Header("Vertical Limit")]
    public float minY = -4f;

    [Header("Stamina Drain Multipliers")]
    public float noFootDrainMultiplier = 1f;
    public float oneFootDrainMultiplier = 0.4f;
    public float twoFeetDrainMultiplier = 0.1f;

    void Start()
    {
        if (leftJoint != null) leftJoint.enabled = false;
        if (rightJoint != null) rightJoint.enabled = false;
    }

    void FixedUpdate()
    {
        UpdateJoint(leftHandGrip, leftJoint, leftShoulderPivot, leftArmLength);
        UpdateJoint(rightHandGrip, rightJoint, rightShoulderPivot, rightArmLength);

        UpdateStaminaDrain();

        ApplyHandForce();
        ApplyFootForce();
        ClampVelocity();
        ClampMinY();
    }

    bool IsGripping(HandGrip grip)
    {
        return grip != null && grip.isGripping && grip.currentHold != null;
    }

    bool IsPlanted(FootPlant foot)
    {
        return foot != null && foot.isPlanted && foot.currentFootHold != null;
    }

    void UpdateJoint(HandGrip grip, DistanceJoint2D joint, Transform shoulderPivot, float armLength)
    {
        if (joint == null || grip == null || shoulderPivot == null || bodyRb == null)
            return;

        bool gripping = IsGripping(grip);

        if (!gripping)
        {
            joint.enabled = false;
            joint.connectedBody = null;
            return;
        }

        Rigidbody2D holdRb = grip.currentHold.GetComponent<Rigidbody2D>();
        if (holdRb == null)
        {
            joint.enabled = false;
            joint.connectedBody = null;
            return;
        }

        joint.connectedBody = holdRb;

        // 身体这一侧的连接点 = 肩膀相对身体的局部位置
        joint.anchor = bodyRb.transform.InverseTransformPoint(shoulderPivot.position);

        // 抓点中心
        joint.connectedAnchor = Vector2.zero;

        // 只限制最大长度，不锁死当前位置
        joint.maxDistanceOnly = true;
        joint.distance = armLength;

        joint.enabled = true;
    }

    void UpdateStaminaDrain()
    {
        bool leftFootDown = IsPlanted(leftFootPlant);
        bool rightFootDown = IsPlanted(rightFootPlant);

        int plantedFeetCount = 0;
        if (leftFootDown) plantedFeetCount++;
        if (rightFootDown) plantedFeetCount++;

        float drainMultiplier = noFootDrainMultiplier;

        if (plantedFeetCount == 1)
        {
            drainMultiplier = oneFootDrainMultiplier;
        }
        else if (plantedFeetCount >= 2)
        {
            drainMultiplier = twoFeetDrainMultiplier;
        }

        if (leftHandGrip != null)
        {
            leftHandGrip.staminaDrainMultiplier = drainMultiplier;
        }

        if (rightHandGrip != null)
        {
            rightHandGrip.staminaDrainMultiplier = drainMultiplier;
        }
    }

    void ApplyHandForce()
    {
        Vector2 totalForce = Vector2.zero;

        // 左手：键盘 WASD + 左手柄 left stick
        if (IsGripping(leftHandGrip))
        {
            Vector2 inputDir = Vector2.zero;

            if (leftGamepadInput != null)
            {
                inputDir += leftGamepadInput.handMove;
            }

            if (Input.GetKey(KeyCode.W)) inputDir += Vector2.up;
            if (Input.GetKey(KeyCode.S)) inputDir += Vector2.down;
            if (Input.GetKey(KeyCode.A)) inputDir += Vector2.left;
            if (Input.GetKey(KeyCode.D)) inputDir += Vector2.right;

            if (inputDir.magnitude > inputDeadZone)
            {
                totalForce += inputDir.normalized * handPullForce;
            }
        }

        // 右手：键盘方向键 + 右手柄 left stick（或你绑定的 handMove）
        if (IsGripping(rightHandGrip))
        {
            Vector2 inputDir = Vector2.zero;

            if (rightGamepadInput != null)
            {
                inputDir += rightGamepadInput.handMove;
            }

            if (Input.GetKey(KeyCode.UpArrow)) inputDir += Vector2.up;
            if (Input.GetKey(KeyCode.DownArrow)) inputDir += Vector2.down;
            if (Input.GetKey(KeyCode.LeftArrow)) inputDir += Vector2.left;
            if (Input.GetKey(KeyCode.RightArrow)) inputDir += Vector2.right;

            if (inputDir.magnitude > inputDeadZone)
            {
                totalForce += inputDir.normalized * handPullForce;
            }
        }

        if (totalForce != Vector2.zero)
        {
            bodyRb.AddForce(totalForce, ForceMode2D.Force);
        }
    }

    void ApplyFootForce()
    {
        Vector2 totalForce = Vector2.zero;

        // 左脚：键盘 IJKL + 左手柄 right stick
        if (IsPlanted(leftFootPlant))
        {
            Vector2 inputDir = Vector2.zero;

            if (leftGamepadInput != null)
            {
                inputDir += leftGamepadInput.footMove;
            }

            if (Input.GetKey(KeyCode.I)) inputDir += Vector2.up;
            if (Input.GetKey(KeyCode.K)) inputDir += Vector2.down;
            if (Input.GetKey(KeyCode.J)) inputDir += Vector2.left;
            if (Input.GetKey(KeyCode.L)) inputDir += Vector2.right;

            if (inputDir.magnitude > inputDeadZone)
            {
                // 脚输入方向反过来：脚往这个方向蹬，身体往反方向走
                totalForce += (-inputDir.normalized) * footPushForce;
            }
        }

        // 右脚：键盘小键盘 + 右手柄 right stick（或你绑定的 footMove）
        if (IsPlanted(rightFootPlant))
        {
            Vector2 inputDir = Vector2.zero;

            if (rightGamepadInput != null)
            {
                inputDir += rightGamepadInput.footMove;
            }

            if (Input.GetKey(KeyCode.Keypad5)) inputDir += Vector2.up;
            if (Input.GetKey(KeyCode.Keypad2)) inputDir += Vector2.down;
            if (Input.GetKey(KeyCode.Keypad1)) inputDir += Vector2.left;
            if (Input.GetKey(KeyCode.Keypad3)) inputDir += Vector2.right;

            if (inputDir.magnitude > inputDeadZone)
            {
                totalForce += (-inputDir.normalized) * footPushForce;
            }
        }

        if (totalForce != Vector2.zero)
        {
            bodyRb.AddForce(totalForce, ForceMode2D.Force);
        }
    }

    void ClampVelocity()
    {
        if (bodyRb == null) return;

        Vector2 v = bodyRb.linearVelocity;
        v.x = Mathf.Clamp(v.x, -maxVelocity, maxVelocity);
        v.y = Mathf.Clamp(v.y, -maxVelocity, maxVelocity);
        bodyRb.linearVelocity = v;
    }

    void ClampMinY()
    {
        if (bodyRb == null) return;

        Vector2 pos = bodyRb.position;

        if (pos.y < minY)
        {
            pos.y = minY;
            bodyRb.position = pos;

            Vector2 vel = bodyRb.linearVelocity;
            if (vel.y < 0f)
            {
                vel.y = 0f;
                bodyRb.linearVelocity = vel;
            }
        }
    }
}