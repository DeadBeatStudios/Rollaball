using UnityEngine;

public class StunnedState : IEnemyState
{
    private EnemyAIContext ctx;

    public void OnEnter(EnemyAIContext context)
    {
        ctx = context;

        // Freeze all movement
        ctx.controller.SendIntent(new EnemyAIController.AIIntent()
        {
            moveDirection = Vector3.zero,
            freezeMovement = true
        });
    }

    public void OnUpdate()
    {
        // When knockback is over, return to idle
        if (ctx.knockback == null || !ctx.knockback.IsStaggered)
        {
            ctx.controller.ChangeState(new IdleState());
        }
    }

    public void OnFixedUpdate()
    {
        // Keep frozen
        ctx.controller.SendIntent(new EnemyAIController.AIIntent()
        {
            moveDirection = Vector3.zero,
            freezeMovement = true
        });
    }

    public void OnExit()
    {
        ctx.controller.SendIntent(new EnemyAIController.AIIntent());
    }
}
