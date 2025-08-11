using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseCutter : MonoBehaviour
{
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private LayerMask layerMask;

    
    private Camera _camera;

    private GrassInstanceDrawer _currentInstanceDrawer;
    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask))
            {
                if (_currentInstanceDrawer == null || _currentInstanceDrawer.transform.GetInstanceID() !=
                    hit.collider.transform.GetInstanceID())
                {
                    _currentInstanceDrawer = hit.collider.gameObject.GetComponent<GrassInstanceDrawer>();
                }

                if (_currentInstanceDrawer != null)
                {
                    _currentInstanceDrawer.CutCircleAtWorld(hit.point,radius);
                }
            }
        }
    }
}
