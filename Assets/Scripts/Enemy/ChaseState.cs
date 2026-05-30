using UnityEngine;

public class ChaseState : EnemyState
{
    
    float chaseStateSpeed = 5f;

    public ChaseState(EnemyAI enemyAI) : base(enemyAI)
    {
    }


    public override void EnterState()
    {
        enemyAI.SwitchEnemySprite(EnemyAI.EnemyStateType.Chase);
        enemyAI.speed = chaseStateSpeed;
    }

    public override void ExitState()
    {

    }

    public override void UpdateState()
    {
        enemyAI.SetLastSeenPlayerPosition();
        enemyAI.SetLastSeenPlayerPlatforms();
        enemyAI.CheckForPlatformEdge();
    }
}
