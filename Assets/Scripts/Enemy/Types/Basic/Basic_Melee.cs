using UnityEngine;

public class Basic_Melee : Enemy
{
    [Header("Attack")]
    [SerializeField] private float damage;
    [SerializeField] private float range;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float timeBtwAttack;
    private float timer;

    private void Update()
    {
        
    }

    private void DoAttack()
    {

    }

    public void DoDamage()
    {

    }
}
