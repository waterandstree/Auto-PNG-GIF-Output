using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestoryByTime : MonoBehaviour
{
    public bool startActive = true;
    public float time = 5f;

    // Use this for initialization
    private void Start()
    {
        if (startActive) Destroy(gameObject, time);
    }

    private void Update()
    {
        if (startActive) Destroy(gameObject, time);
    }
}