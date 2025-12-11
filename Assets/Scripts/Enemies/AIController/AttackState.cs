using UnityEngine;

public class AttackState : IEnemyState
{
    private EnemyAIContext ctx;
    private Transform target;

    private float dashDistance = 4f;
    private float bashDistance = 7f;

    private bool attackInProgress;

    public void OnEnter(EnemyAIContext context)
    {
        ctx = context;
        attackInProgress = false;

        // Decide who to attack:
        // 1) Flag holder (if someone else has it)
        if (ctx.flag != null && ctx.flag.IsHeld && ctx.flag.CurrentHolder != ctx.transform)
        {
            target = ctx.flag.CurrentHolder;
        }
        else
        {
            // 2) Fall back to player
            target = ctx.player;
        }

        if (target == null)
        {
            ctx.controller.ChangeState(new IdleState());
            return;
        }
    }

    public void OnUpdate()
    {
        if (target == null)
        {
            ctx.controller.ChangeState(new IdleState());
            return;
        }

        float d = Vector3.Distance(ctx.transform.position, target.position);

        if (!attackInProgress)
        {
            // Aggressive: try dash first if close
            if (d <= dashDistance && ctx.controller.dash != null && ctx.controller.dash.CooldownRemaining <= 0f)
            {
                Vector3 dir = (target.position - ctx.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    dir.Normalize();

                if (ctx.controller.dash.TryStartDash(dir))
                {
                    attackInProgress = true;
                    return;
                }
            }

            // Try ground bash
            if (d <= bashDistance && ctx.controller.groundBash != null && ctx.controller.groundBash.CooldownRemaining <= 0f)
            {
                if (ctx.controller.groundBash.TryStartGroundBash())
                {
                    attackInProgress = true;
                    return;
                }
            }

            // If no attack triggered, go back to chase
            ReturnToChase();
            return;
        }

        // Attack in progress; wait until both actions are done
        if (IsAttackFinished())
        {
            ReturnToChase();
        }
    }

    public void OnFixedUpdate()
    {
        // While attacking, freeze navigation intent so dash/bash have full control
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

    private bool IsAttackFinished()
    {
        bool dashDone = ctx.controller.dash == null || !ctx.controller.dash.IsDashing;
        bool bashDone = ctx.controller.groundBash == null || !ctx.controller.groundBash.IsBashing;
        return dashDone && bashDone;
    }

    private void ReturnToChase()
    {
        // If still holding flag, go back to carry; else chase player or flag
        if (ctx.flag != null && ctx.flag.IsHeld && ctx.flag.CurrentHolder == ctx.transform)
        {
            ctx.controller.ChangeState(new CarryFlagState());
        }
        else if (ctx.flag != null && ctx.flag.IsHeld && ctx.flag.CurrentHolder != ctx.transform)
        {
            ctx.controller.ChangeState(new ChasePlayerState());
        }
        else if (ctx.flag != null)
        {
            ctx.controller.ChangeState(new ChaseFlagState());
        }
        else
        {
            ctx.controller.ChangeState(new ChasePlayerState());
        }
    }
}
