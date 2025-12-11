using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    // --------------------------------------------------------------
    //  AI ROLES
    // --------------------------------------------------------------
    public enum AIRole
    {
        BasicChaser,
        Defender,
        FlagChaser
    }

    // --------------------------------------------------------------
    //  AI INTENT
    // --------------------------------------------------------------
    public struct AIIntent
    {
        public Vector3 moveDirection;
        public bool freezeMovement;
        public bool forceStop;
        public bool dash;
        public bool groundBash;
    }

    // --------------------------------------------------------------
    //  REFERENCES
    // --------------------------------------------------------------
    [Header("References")]
    public EnemyPhysicsController physicsController;
    public EnemyDash dash;
    public EnemyGroundBash groundBash;

    [Header("Flag & Targets")]
    public FlagPickup flag;
    public Transform goalTarget;
    public Transform playerTarget;

    [Header("AI Role")]
    public AIRole role;

    [Header("Points")]
    public Transform guardPoint;
    public Transform resetPoint;

    [Header("Tuning")]
    public float flagPriorityRadius = 12f;
    public float playerDetectRadius = 18f;
    public float homeWanderRadius = 12f;

    [HideInInspector] public Vector3 homePosition;

    // --------------------------------------------------------------
    //  STATE MACHINE
    // --------------------------------------------------------------
    private IEnemyState currentState;
    private EnemyAIContext context;
    private AIIntent currentIntent;

    private KnockbackHandler knockback;

    public string CurrentStateName => currentState != null ? currentState.GetType().Name : "None";

    private void Awake()
    {
        if (physicsController == null)
            physicsController = GetComponent<EnemyPhysicsController>();

        if (dash == null)
            dash = GetComponent<EnemyDash>();

        if (groundBash == null)
            groundBash = GetComponent<EnemyGroundBash>();

        knockback = GetComponent<KnockbackHandler>();

        homePosition = transform.position;

        // ----------------------------------------------------------
        // AUTO-FIND FLAG
        // ----------------------------------------------------------
        if (flag == null)
        {
            flag = FindAnyObjectByType<FlagPickup>();
            if (flag != null)
                Debug.Log($"{name}: AI auto-found FlagPickup → {flag.name}");
            else
                Debug.LogWarning($"{name}: No FlagPickup found in the scene!");
        }

        // ----------------------------------------------------------
        // AUTO-FIND GOAL TARGET
        // ----------------------------------------------------------
        if (goalTarget == null)
        {
            var goal = FindAnyObjectByType<GoalTrigger>();
            if (goal != null)
            {
                goalTarget = goal.transform;
                Debug.Log($"{name}: AI auto-assigned GoalTarget → {goalTarget.name}");
            }
            else
            {
                Debug.LogWarning($"{name}: No GoalTrigger found in the scene!");
            }
        }

        // ----------------------------------------------------------
        // AUTO-FIND PLAYER
        // ----------------------------------------------------------
        if (playerTarget == null)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerTarget = player.transform;
                Debug.Log($"{name}: AI auto-found Player → {player.name}");
            }
            else
            {
                Debug.LogWarning($"{name}: No PlayerController found in scene!");
            }
        }

        // ----------------------------------------------------------
        // AUTO-ASSIGN GUARD POINT (Defender default)
        // ----------------------------------------------------------
        if (guardPoint == null)
        {
            // Default: defenders guard the goal
            guardPoint = goalTarget;
            if (guardPoint != null)
                Debug.Log($"{name}: GuardPoint auto-assigned → {guardPoint.name}");
        }

        // Build AI context
        context = new EnemyAIContext(this, physicsController, knockback);
    }


    private void Start()
    {
        ChangeState(new IdleState());
    }

    private void Update()
    {
        // Hard override: Stunned takes priority
        if (knockback != null && knockback.IsStaggered)
        {
            if (!(currentState is StunnedState))
                ChangeState(new StunnedState());
        }

        context.RefreshDynamicTargets();

        currentState?.OnUpdate();
    }

    private void FixedUpdate()
    {
        currentState?.OnFixedUpdate();
    }

    // --------------------------------------------------------------
    //  STATE CHANGE
    // --------------------------------------------------------------
    public void ChangeState(IEnemyState newState)
    {
        if (newState == null)
            return;

        currentState?.OnExit();

        // Clear old movement intent when switching states
        currentIntent = new AIIntent();

        currentState = newState;
        currentState.OnEnter(context);
    }

    // --------------------------------------------------------------
    //  INTENT
    // --------------------------------------------------------------
    public void SendIntent(AIIntent intent)
    {
        currentIntent = intent;
    }

    private void LateUpdate()
    {
        ApplyIntent();
    }

    private void ApplyIntent()
    {
        if (physicsController == null)
            return;

        if (currentIntent.freezeMovement || currentIntent.forceStop)
        {
            physicsController.ClearAIMMove();
            return;
        }

        physicsController.SetAIMMoveDirection(currentIntent.moveDirection);
    }

    // --------------------------------------------------------------
    //  EXTERNAL HELPERS
    // --------------------------------------------------------------
    public void ForceReset()
    {
        ChangeState(new ResetState());
    }
}
