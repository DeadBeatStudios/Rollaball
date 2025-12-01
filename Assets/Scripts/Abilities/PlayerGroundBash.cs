using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerGroundBash : MonoBehaviour
{
    [Header("Bash Settings")]
    public float bashCooldown = 1.5f;
    public float upwardLiftForce = 8f;
    public float slamDownForce = 20f;
    public float shockwaveForce = 25f;
    public float shockwaveRadius = 8f;
    public float liftDelay = 0.1f;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask bashAffectsLayers;

    private Rigidbody rb;
    private PlayerInput playerInput;
    private float cooldownTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public void OnBash(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            TryStartBash();
    }

    private void TryStartBash()
    {
        if (cooldownTimer > 0f)
            return;

        StartCoroutine(BashSequence());
    }

    private IEnumerator BashSequence()
    {
        bool grounded = IsGrounded();

        if (grounded)
        {
            rb.AddForce(Vector3.up * upwardLiftForce, ForceMode.VelocityChange);
            yield return new WaitForSeconds(liftDelay);
        }

        rb.AddForce(Vector3.down * slamDownForce, ForceMode.VelocityChange);

        while (!IsGrounded())
            yield return null;

        DoShockwave();
        cooldownTimer = bashCooldown;
    }

    private void DoShockwave()
    {
        Vector3 origin = transform.position;
        Collider[] hits = Physics.OverlapSphere(origin, shockwaveRadius, bashAffectsLayers);

        foreach (var hit in hits)
        {
            if (hit.transform == transform)
                continue;

            IKnockbackReceiver recv = hit.GetComponentInParent<IKnockbackReceiver>();
            if (recv != null)
            {
                Vector3 dir = (hit.transform.position - origin).normalized;
                dir.y = 0.25f;

                recv.ApplyKnockback(dir * shockwaveForce, 0.25f);
            }
        }
    }

    private bool IsGrounded()
    {
        return Physics.SphereCast(
            transform.position,
            0.45f,
            Vector3.down,
            out _,
            0.2f,
            groundLayer,
            QueryTriggerInteraction.Ignore);
    }
}
