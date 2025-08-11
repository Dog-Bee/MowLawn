using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "PlayerConfig/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [field: Header("Player Config"), SerializeField] public float walkSpeed { get; private set; } = 2.5f;
    [field: SerializeField] public float runSpeed { get; private set; } = 5f;
    [field: SerializeField] public float rotateSpeed { get; private set; } = 20f;
    
    [field: Header("Combat"), SerializeField] public float attackCooldown { get; private set; } = 0.45f;
}
