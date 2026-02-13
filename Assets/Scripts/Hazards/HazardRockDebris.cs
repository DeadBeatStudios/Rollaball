using UnityEngine;

public class HazardRockDebris : MonoBehaviour
{
    private float hazardEndTime;
    private bool isHazardous = true;

    public void Initialize(float hazardDuration, float lifetime)
    {
        hazardEndTime = Time.time + hazardDuration;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (isHazardous && Time.time >= hazardEndTime)
        {
            isHazardous = false;
            // 💡 Consider: Change color/material here to show it's safe
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isHazardous) return;

        Collider other = collision.collider;
        if (other == null) return;

        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            // Reporter-only: no respawn/spawner/flag logic here.
            DeathEvents.ReportDeath(other.gameObject, DeathEvents.DeathCause.Hazard, gameObject);

            // Optional: prevent multi-kills from the same debris piece
            isHazardous = false;
        }
    }
}
