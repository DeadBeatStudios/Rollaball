using UnityEngine;

public class ReturnToGuardState : IEnemyState
{
    private EnemyAIContext ctx;
    private const float arriveDistance = 2.5f;
    private const float playerReactionDistance = 14f;

    public void OnEnter(EnemyAIContext context)
    {
        ctx = context;

        if (ctx.controller.guardPoint == null)
        {
            // Use home as guard if none assigned
            ctx.controller.guardPoint = null;
        }
    }

    public void OnUpdate()
    {
        Vector3 guardPos =
            ctx.controller.guardPoint != null
            ? ctx.controller.guardPoint.position
            : ctx.controller.homePosition;

        float d = Vector3.Distance(ctx.transform.position, guardPos);

        if (d <= arriveDistance)
        {
            ctx.controller.ChangeState(new IdleState());
            return;
        }

        // Flag becomes urgent
        if (ctx.flag != null && Vector3.Distance(ctx.transform.position, ctx.flag.transform.position) <= ctx.controller.flagPriorityRadius)
        {
            ctx.controller.ChangeState(new ChaseFlagState());
            return;
        }

        // Player comes close
        if (ctx.player != null)
        {
            float pd = Vector3.Distance(ctx.transform.position, ctx.player.position);
            if (pd <= playerReactionDistance)
            {
                ctx.controller.ChangeState(new ChasePlayerState());
                return;
            }
        }
    }

    public void OnFixedUpdate()
    {
        Vector3 guardPos =
            ctx.controller.guardPoint != null
            ? ctx.controller.guardPoint.position
            : ctx.controller.homePosition;

        Vector3 dir = (guardPos - ctx.transform.position);
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
