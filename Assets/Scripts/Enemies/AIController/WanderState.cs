using UnityEngine;

public class WanderState : IEnemyState
{
    private EnemyAIContext ctx;
    private float wanderTimer;
    private Vector3 moveDir;

    public void OnEnter(EnemyAIContext context)
    {
        ctx = context;

        wanderTimer = Random.Range(1.2f, 3f);

        // Pick random direction initially
        Vector2 rand = Random.insideUnitCircle.normalized;
        moveDir = new Vector3(rand.x, 0f, rand.y);
    }

    public void OnUpdate()
    {
        wanderTimer -= Time.deltaTime;

        if (ctx.flag != null && ctx.flag.IsHeld && ctx.flag.CurrentHolder == ctx.transform)
        {
            ctx.controller.ChangeState(new CarryFlagState());
            return;
        }

        if (FlagIsNearby())
        {
            ctx.controller.ChangeState(new ChaseFlagState());
            return;
        }

        if (PlayerInDetectRange())
        {
            ctx.controller.ChangeState(new ChasePlayerState());
            return;
        }

        if (ctx.controller.role == EnemyAIController.AIRole.Defender && GuardDisplaced())
        {
            ctx.controller.ChangeState(new ReturnToGuardState());
            return;
        }

        if (wanderTimer <= 0f)
        {
            ctx.controller.ChangeState(new IdleState());
        }
    }

    public void OnFixedUpdate()
    {
        // Keep wander within home radius
        Vector3 home = ctx.controller.homePosition;
        float distHome = Vector3.Distance(ctx.transform.position, home);

        if (distHome > ctx.controller.homeWanderRadius)
        {
            moveDir = (home - ctx.transform.position);
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.001f)
                moveDir.Normalize();
        }

        ctx.controller.SendIntent(new EnemyAIController.AIIntent()
        {
            moveDirection = moveDir
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
