public interface IEnemyState
{
    void OnEnter(EnemyAIContext context);
    void OnUpdate();
    void OnFixedUpdate();
    void OnExit();
}
