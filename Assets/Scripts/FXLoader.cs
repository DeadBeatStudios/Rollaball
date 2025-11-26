using UnityEngine;

public static class FXLoader
{
    private const string BASE_PATH = "DeathFX/";

    public static GameObject LoadChunkExplosion()
    {
        return Resources.Load<GameObject>(BASE_PATH + "Explosion_ChunkSet");
    }

    // Example future functions:
    public static GameObject LoadEnemyExplosion()
    {
        return Resources.Load<GameObject>(BASE_PATH + "EnemyExplosion");
    }

    public static GameObject LoadBossExplosion()
    {
        return Resources.Load<GameObject>(BASE_PATH + "BossExplosion");
    }
}
