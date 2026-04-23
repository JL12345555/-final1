using UnityEngine;

public enum HoldSurfaceType
{
    Normal,
    Long,
    Slippery
}

public class HoldSurface2D : MonoBehaviour
{
    public HoldSurfaceType holdType = HoldSurfaceType.Normal;

    [Tooltip("滑点沿抓点本地 X 轴滑动。1 = 向右，-1 = 向左")]
    public float slipperyDirection = 1f;
}