using UnityEngine;
using UnityEngine.Rendering;

public class SearchState : EnemyState
{

    float searchStateSpeed = 4f;
    float timeBeforeGivingUpSearch = 5f;
    float sightAngle = 180f;

    public SearchState(EnemyAI enemyAI) : base(enemyAI)
    {
    }


    public override void EnterState()
    {
        enemyAI.SwitchEnemySprite(EnemyAI.EnemyStateType.Search);
        enemyAI.speed = searchStateSpeed;
    }

    public override void ExitState()
    {

    }

    public override void UpdateState()
    {
        if (enemyAI.CanSeePlayer(sightAngle))
        {
            enemyAI.SetState(EnemyAI.EnemyStateType.Chase);
        }
        else if (enemyAI.TimeInState > timeBeforeGivingUpSearch)
        {
            enemyAI.SetState(EnemyAI.EnemyStateType.Idle);
        }
        else
        {
            enemyAI.CheckForPlatformEdge();
        }
    }
}
