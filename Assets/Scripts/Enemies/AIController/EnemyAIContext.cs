using UnityEngine;

public class EnemyAIContext
{
    public EnemyAIController controller;
    public EnemyPhysicsController physics;
    public Transform transform;
    public FlagPickup flag;
    public Transform goal;
    public Transform player;
    public KnockbackHandler knockback;

    public EnemyAIContext(EnemyAIController c, EnemyPhysicsController p, KnockbackHandler k)
    {
        controller = c;
        physics = p;
        transform = c.transform;
        flag = c.flag;
        goal = c.goalTarget;
        player = c.playerTarget;
        knockback = k;
    }

    public void RefreshDynamicTargets()
    {
        flag = controller.flag;
        goal = controller.goalTarget;
        player = controller.playerTarget;
    }
}
