using UnityEngine;

public class ChasePlayerState : IEnemyState
{
    private EnemyAIContext ctx;

    private const float chaseStopDistance = 35f;
    private const float attackTriggerDistance = 7f;

    public void OnEnter(EnemyAIContext context)
    {
        ctx = context;
    }

    public void OnUpdate()
    {
        if (ctx.player == null)
        {
            ctx.controller.ChangeState(new IdleState());
            return;
        }

        // If carrying flag, carry to goal instead
        if (ctx.flag != null && ctx.flag.IsHeld && ctx.flag.CurrentHolder == ctx.transform)
        {
            ctx.controller.ChangeState(new CarryFlagState());
            return;
        }

        float d = Vector3.Distance(ctx.transform.position, ctx.player.position);

        // Range-based disengage
        if (d > chaseStopDistance)
        {
            ctx.controller.ChangeState(new IdleState());
            return;
        }

        // Defender guard logic: don't pursue too far
        if (ctx.controller.role == EnemyAIController.AIRole.Defender && GuardDisplaced())
        {
            ctx.controller.ChangeState(new ReturnToGuardState());
            return;
        }

        // Aggressive: attack if close
        if (d <= attackTriggerDistance)
        {
            ctx.controller.ChangeState(new AttackState());
            return;
        }
    }

    public void OnFixedUpdate()
    {
        if (ctx.player == null)
            return;

        Vector3 dir = (ctx.player.position - ctx.transform.position);
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
