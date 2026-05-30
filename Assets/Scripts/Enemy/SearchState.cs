using UnityEngine;
using UnityEngine.Rendering;

public class SearchState : EnemyState
{

    bool hasCheckedLastSeenPosition = false;
    float searchStateSpeed = 4f;

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
        if (hasCheckedLastSeenPosition)
        {
            enemyAI.SetState(EnemyAI.EnemyStateType.Idle);
        }
        else
        {

        }
    }
}
