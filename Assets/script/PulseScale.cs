using UnityEngine;

public class PulseScale : MonoBehaviour
{
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public float speed = 2f;

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed - Mathf.PI / 2f) + 1f) / 2f;
        float scale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = new Vector3(scale, scale, scale);
    }
}