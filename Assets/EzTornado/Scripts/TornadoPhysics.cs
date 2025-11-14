using System.Runtime.CompilerServices;
using UnityEngine;

public class TornadoPhysics : MonoBehaviour
{
    [Header("Tornado Radius")]
    public float pullRadius = 15f;
    public float innerLiftRadius = 5f;

    [Header("Forces")]
    public float pullForce = 50f;
    public float spinForce = 40f;
    public float liftForce = 25f;

    [Header("Options")]
    public bool useDistanceFalloff = true;

    [Header("Movement")]
    public bool enableMovement = true;
    public float moveRadius = 2f;
    public float moveSpeed = 0.5f;

    private Vector3 startPosition;
    private Rigidbody rb;

    private void Awake()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!enableMovement) return;

        float t = Time.time * moveSpeed;

        Vector3 offset = new Vector3(
            Mathf.Cos(t) * moveRadius,
            0f,
            Mathf.Sin(t) * moveRadius
         );

        Vector3 targetPosition = startPosition + offset;

        if (rb != null && rb.isKinematic)
        {
            rb.MovePosition(targetPosition);
        }
        else
        {
            transform.position = targetPosition;
        }
            
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody otherRb = other.GetComponent<Rigidbody>();
        if (otherRb != null) return;

        Vector3 toCenter = (transform.position - otherRb.worldCenterOfMass);
        float distance =toCenter.magnitude;

        if (distance > pullRadius) return;

        float distanceScale = useDistanceFalloff
            ? Mathf.Clamp01(1f - (distance / pullRadius)) : 1f;

        Vector3 pull = toCenter.normalized * pullForce * distanceScale;
        otherRb.AddForce(pull, ForceMode.Acceleration);

        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter).normalized;
        otherRb.AddForce(tangent * spinForce * distanceScale, ForceMode.Acceleration);

        if(distance < innerLiftRadius)
        {
            otherRb.AddForce(Vector3.up * liftForce * distanceScale, ForceMode.Acceleration);
        }
    }
}
