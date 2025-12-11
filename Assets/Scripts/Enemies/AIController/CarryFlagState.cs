using UnityEngine;

public class CarryFlagState : IEnemyState
{
    private EnemyAIContext ctx;
    private Transform goal;

    public void OnEnter(EnemyAIContext context)
    {
        ctx = context;
        goal = ctx.goal;

        if (goal == null)
        {
            Debug.LogWarning("CarryFlagState: No goal target assigned!");
            ctx.controller.ChangeState(new IdleState());
            return;
        }
    }

    public void OnUpdate()
    {
        if (ctx.flag == null)
        {
            ctx.controller.ChangeState(new IdleState());
            return;
        }

        // If flag is dropped → go back to chasing flag
        if (!ctx.flag.IsHeld)
        {
            ctx.controller.ChangeState(new ChaseFlagState());
            return;
        }

        // If somehow another holder appears
        if (ctx.flag.CurrentHolder != ctx.transform)
        {
            ctx.controller.ChangeState(new ChasePlayerState());
            return;
        }

        // Close enough to goal; scoring will be handled by other systems
        float d = Vector3.Distance(ctx.transform.position, goal.position);
        if (d <= 2f)
        {
            ctx.controller.ChangeState(new IdleState());
        }
    }

    public void OnFixedUpdate()
    {
        if (goal == null) return;

        Vector3 dir = (goal.position - ctx.transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            dir.Normalize();

        ctx.controller.SendIntent(new EnemyAIController.AIIntent()
        {
            moveDirection = dir
        });
    }

    public void OnExit()
    {
        ctx.controller.SendIntent(new EnemyAIController.AIIntent());
    }
}
