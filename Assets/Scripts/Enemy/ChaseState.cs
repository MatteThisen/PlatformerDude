using UnityEngine;
using UnityEngine.Timeline;

public class ChaseState : EnemyState
{
    
    float chaseStateSpeed = 5f;
    float timeBeforeGivingUpChase = 5f;
    float sightAngle = 360f;

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

        if (enemyAI.CanSeePlayer(sightAngle))
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
