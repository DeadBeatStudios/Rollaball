using UnityEngine;

public class ChaseFlagState : IEnemyState
{
    private EnemyAIContext ctx;

    private const float chaseStopDistance = 30f;
    private const float attackTriggerDistance = 7f;

    public void OnEnter(EnemyAIContext context)
    {
        ctx = context;
    }

    public void OnUpdate()
    {
        if (ctx.flag == null)
        {
            ctx.controller.ChangeState(new IdleState());
            return;
        }

        // If THIS enemy now has the flag, carry it
        if (ctx.flag.IsHeld && ctx.flag.CurrentHolder == ctx.transform)
        {
            ctx.controller.ChangeState(new CarryFlagState());
            return;
        }

        // If someone else holds the flag (likely player) → chase player
        if (ctx.flag.IsHeld && ctx.flag.CurrentHolder != ctx.transform)
        {
            ctx.controller.ChangeState(new ChasePlayerState());
            return;
        }

        float d = Vector3.Distance(ctx.transform.position, ctx.flag.transform.position);

        // Range-based disengage
        if (ctx.controller.role != EnemyAIController.AIRole.FlagChaser && d > chaseStopDistance)
        {
            ctx.controller.ChangeState(new IdleState());
            return;
        }

        // Defender guard logic
        if (ctx.controller.role == EnemyAIController.AIRole.Defender && GuardDisplaced() && d > ctx.controller.flagPriorityRadius)
        {
            ctx.controller.ChangeState(new ReturnToGuardState());
            return;
        }

        // Aggressive: go into attack if close enough
        if (d <= attackTriggerDistance)
        {
            ctx.controller.ChangeState(new AttackState());
            return;
        }
    }

    public void OnFixedUpdate()
    {
        if (ctx.flag == null) return;

        Vector3 dir = (ctx.flag.transform.position - ctx.transform.position);
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

    private bool GuardDisplaced()
    {
        Vector3 guardPos =
            ctx.controller.guardPoint != null
            ? ctx.controller.guardPoint.position
            : ctx.controller.homePosition;

        float d = Vector3.Distance(ctx.transform.position, guardPos);
        return d > 3f;
    }
}
