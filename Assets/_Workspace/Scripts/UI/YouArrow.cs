using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YouArrow : MonoBehaviour
{
    private float rotateSpeed = 120f;
    void Start()
    {
        Destroy(transform.parent.gameObject, 5f);
    }

    void Update()
    {
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }
}
