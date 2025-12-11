using UnityEngine;

public class ResetState : IEnemyState
{
    private EnemyAIContext ctx;
    private bool hasReset;

    public void OnEnter(EnemyAIContext context)
    {
        ctx = context;
        hasReset = false;

        // Freeze while resetting
        ctx.controller.SendIntent(new EnemyAIController.AIIntent()
        {
            moveDirection = Vector3.zero,
            freezeMovement = true
        });
    }

    public void OnUpdate()
    {
        if (!hasReset)
        {
            Vector3 targetPos =
                ctx.controller.resetPoint != null
                ? ctx.controller.resetPoint.position
                : ctx.controller.homePosition;

            ctx.transform.position = targetPos;
            hasReset = true;
        }

        // After reset, go to appropriate state
        if (ctx.controller.role == EnemyAIController.AIRole.Defender)
            ctx.controller.ChangeState(new ReturnToGuardState());
        else
            ctx.controller.ChangeState(new IdleState());
    }

    public void OnFixedUpdate()
    {
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
