using UnityEngine;

public class SearchState : EnemyState
{
    public SearchState(EnemyAI enemyAI) : base(enemyAI)
    {
    }

    public override void EnterState()
    {
        enemyAI.SwitchEnemySprite(EnemyAI.EnemyStateType.Search);
    }

    public override void ExitState()
    {

    }

    public override void UpdateState()
    {

    }
}
