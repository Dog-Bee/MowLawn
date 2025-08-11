using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private float angularSpeed = 360f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float moveDrag = 0;
    [SerializeField] private float stopDrag = 8;


    private Transform _playerTransform;
    private Vector3 _moveInput;

    private void Awake()
    {
        _playerTransform = transform;
    }

    private void Update()
    {
        ReadMoveInput();
        PlayerRotate();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void ReadMoveInput()
    {
        _moveInput = new Vector3(joystick.Horizontal, 0,joystick.Vertical);
        _moveInput = Vector3.ClampMagnitude(_moveInput, 1);
    }

    private void Move()
    {
       Vector3 velocity = rigidbody.velocity;
       bool hasInput = _moveInput.sqrMagnitude > 0;
       rigidbody.drag = hasInput ? moveDrag : stopDrag;
       if (hasInput)
       {
           Vector3 desiredSpeed = _moveInput.normalized * moveSpeed;
           Vector3 delta = desiredSpeed - velocity;
           rigidbody.AddForce(delta, ForceMode.VelocityChange);
       }  
      
    }

    private void PlayerRotate()
    {
        _playerTransform.Rotate(Vector3.up * Time.deltaTime * angularSpeed);
    }
}