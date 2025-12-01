using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KnockbackHandler : MonoBehaviour, IKnockbackReceiver
{
    [Header("Knockback Settings")]
    public float defaultStaggerTime = 0.25f;

    private Rigidbody rb;
    private float staggerTimer = 0f;

    public bool IsStaggered => staggerTimer > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (staggerTimer > 0f)
            staggerTimer -= Time.deltaTime;
    }

    public void ApplyKnockback(Vector3 force, float staggerTime)
    {
        rb.AddForce(force, ForceMode.Impulse);
        staggerTimer = Mathf.Max(staggerTime, defaultStaggerTime);
    }
}
