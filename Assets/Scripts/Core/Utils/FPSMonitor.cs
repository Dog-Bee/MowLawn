using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
        
        
        #if UNITY_EDITOR
        int triangles = UnityStats.triangles;
        int vertices = UnityStats.vertices;
        GUI.Label(new Rect(15,45,256,256),$"Triangles: {FormatCompact(triangles)}", _guiStyle);
        GUI.Label(new Rect(15,75,256,256),$"Vertices: {FormatCompact(vertices)}", _guiStyle);
        #endif
        
    }

    private string FormatCompact(long value)
    {
        if (value >= 1_000_000)
        {
            return TrimZero((value / 1_000_000f).ToString("0.#")) + "m";
        }

        if (value >= 1_000)
        {
            return TrimZero((value / 1_000f).ToString("0.#")) + "k";
        }
        
        return value.ToString("0");
    }

    private string TrimZero(string s)
    {
        if(s.EndsWith(".0")) return s.Substring(0, s.Length - 2);
        return s;
    }
}
