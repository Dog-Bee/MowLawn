using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YRotator : MonoBehaviour
{
    private Transform _transform;
    // Start is called before the first frame update
    void Start()
    {
        _transform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        _transform.Rotate(Vector3.up, Time.deltaTime * 90f);
    }
}
