using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSMonitor : MonoBehaviour
{
    [SerializeField] private float smoothing = 0.1f;
    [SerializeField] private int fontSize = 28;

    private float _dt;
    private GUIStyle _guiStyle;

    private void Update()
    {
        _dt += (Time.unscaledDeltaTime - _dt) * smoothing;
    }

    void OnGUI()
    {
        if (_guiStyle == null)
        {
            _guiStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
            };
        }
        int fps = Mathf.RoundToInt(1 / Mathf.Max(0.001f, _dt));
        GUI.color = fps<20? Color.red : Color.green;
        GUI.Label(new Rect(15, 15, 256, 256), $"FPS: {fps}",_guiStyle);
    }
}
