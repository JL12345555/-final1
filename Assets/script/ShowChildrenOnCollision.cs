using System.Collections.Generic;
using UnityEngine;

public class ShowChildrenOnCollision : MonoBehaviour
{
    [Header("Optional: leave empty to use all direct children")]
    public GameObject[] childrenToToggle;

    private readonly HashSet<Collider2D> insideColliders = new HashSet<Collider2D>();

    void Start()
    {
        if (childrenToToggle == null || childrenToToggle.Length == 0)
        {
            childrenToToggle = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                childrenToToggle[i] = transform.GetChild(i).gameObject;
            }
        }

        SetChildrenVisible(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        insideColliders.Add(other);
        SetChildrenVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        insideColliders.Remove(other);

        if (insideColliders.Count == 0)
            SetChildrenVisible(false);
    }

    private void SetChildrenVisible(bool visible)
    {
        foreach (GameObject child in childrenToToggle)
        {
            if (child != null)
                child.SetActive(visible);
        }
    }
}