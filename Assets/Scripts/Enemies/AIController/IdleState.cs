using UnityEngine;

public class IdleState : IEnemyState
{
    private EnemyAIContext ctx;
    private float idleTimer;

    public void OnEnter(EnemyAIContext context)
    {
        ctx = context;
        idleTimer = Random.Range(0.6f, 1.8f);

        ctx.controller.SendIntent(new EnemyAIController.AIIntent()
        {
            moveDirection = Vector3.zero
        });
    }

    public void OnUpdate()
    {
        idleTimer -= Time.deltaTime;

        // If this enemy is holding flag → carry it to goal
        if (ctx.flag != null && ctx.flag.IsHeld && ctx.flag.CurrentHolder == ctx.transform)
        {
            ctx.controller.ChangeState(new CarryFlagState());
            return;
        }

        // Flag nearby → chase flag
        if (FlagIsNearby())
        {
            ctx.controller.ChangeState(new ChaseFlagState());
            return;
        }

        // Player in range → chase (for basic & flag chaser, or defender near guard)
        if (PlayerInDetectRange())
        {
            ctx.controller.ChangeState(new ChasePlayerState());
            return;
        }

        if (idleTimer <= 0f)
        {
            // Defender tries to return to guard/home, others wander
            if (ctx.controller.role == EnemyAIController.AIRole.Defender && GuardDisplaced())
            {
                ctx.controller.ChangeState(new ReturnToGuardState());
            }
            else
            {
                ctx.controller.ChangeState(new WanderState());
            }
        }
    }

    public void OnFixedUpdate()
    {
        // No movement in idle
        ctx.controller.SendIntent(new EnemyAIController.AIIntent()
        {
            moveDirection = Vector3.zero
        });
    }

    public void OnExit()
    {
        ctx.controller.SendIntent(new EnemyAIController.AIIntent());
    }

    private bool FlagIsNearby()
    {
        if (ctx.flag == null) return false;
        float d = Vector3.Distance(ctx.transform.position, ctx.flag.transform.position);
        return d <= ctx.controller.flagPriorityRadius;
    }

    private bool PlayerInDetectRange()
    {
        if (ctx.player == null) return false;

        float d = Vector3.Distance(ctx.transform.position, ctx.player.position);
        if (ctx.controller.role == EnemyAIController.AIRole.Defender)
        {
            // Defender only reacts if near its guard/home
            if (!GuardDisplaced())
                return d <= ctx.controller.playerDetectRadius;
            return false;
        }

        return d <= ctx.controller.playerDetectRadius;
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
