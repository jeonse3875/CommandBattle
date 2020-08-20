using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageTMP : MonoBehaviour
{
    private float moveSpeed = 2f;
    private float destroyTime = 2f;
    private TextMeshPro tMP;
    private Color color;
    public string message;
    public SpriteRenderer swordIcon;
    private float timer = 0f;

    void Start()
    {
        tMP = GetComponent<TextMeshPro>();
        color = tMP.color;
        tMP.text = message;
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);
        color.a = Mathf.Lerp(1f, 0f, timer / destroyTime);
        tMP.color = color;
        swordIcon.color = color;
        timer += Time.deltaTime;
    }
}
