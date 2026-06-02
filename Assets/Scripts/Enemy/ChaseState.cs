using UnityEngine;

public class ChaseState : EnemyState
{
    
    float chaseStateSpeed = 5f;
    float timeBeforeGivingUpChase = 10f;

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
        enemyAI.SetLastSeenPlayerPlatforms();

        if (enemyAI.CanSeePlayer())
        {
            enemyAI.SetLastSeenPlayerTime();
            enemyAI.CheckForPlatformEdge();
        }
        else if (Time.time - enemyAI.timeWhenPlayerSeenLast > timeBeforeGivingUpChase)
        {
            enemyAI.SetState(EnemyAI.EnemyStateType.Search);
        }
        else
        {
            enemyAI.CheckForPlatformEdge();
        }

    }
}
