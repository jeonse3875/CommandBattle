using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LightEffect : MonoBehaviour
{
    public Image image_Light;
    private void Start()
    {
        image_Light.transform.DOScale(1f, 1f);
    }
}
