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

        if (collision.collider.CompareTag("Player"))
        {
            PlayerRespawn pr = collision.collider.GetComponent<PlayerRespawn>();
            if (pr != null)
                pr.HandleDeath(FlagPickup.FlagDropCause.SelfDestruct);
        }
        else if (collision.collider.CompareTag("Enemy"))
        {
            EnemyFallDetector efd = collision.collider.GetComponent<EnemyFallDetector>();
            if (efd != null)
                efd.ForceKillDEBUG();
        }
    }
}