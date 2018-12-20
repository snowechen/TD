﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    [SerializeField]
    private float fadeTime = 0.5f;

    private Image image;

    private Color color;

    private void Awake()
    {
        image = GetComponent<Image>();
        color = image.color;
    }

    public IEnumerator FadeIn()
    {
        float t = 0;

        while (t < fadeTime)
        {
            color.a = Mathf.Lerp(1.0f, 0.0f, t / fadeTime);
            image.color = color;
            t += Time.deltaTime;
            yield return null;
        }

        color.a = 0.0f;
        image.color = color;
    }

    public IEnumerator FadeOut()
    {
        float t = 0;

        while (t < fadeTime)
        {
            color.a = Mathf.Lerp(0.0f, 1.0f, t / fadeTime);
            image.color = color;
            t += Time.deltaTime;
            yield return null;
        }
       
        color.a = 1.0f;
        image.color = color;
    }
}

