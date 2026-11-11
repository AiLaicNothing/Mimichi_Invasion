using UnityEngine;

public class Basic_Range : Enemy
{
    [Header("Attack")]
    [SerializeField] private Transform firepoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float damage;
}
