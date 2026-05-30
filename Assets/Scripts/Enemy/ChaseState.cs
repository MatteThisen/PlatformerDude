using UnityEngine;

public class ChaseState : EnemyState
{
    public ChaseState(EnemyAI enemyAI) : base(enemyAI)
    {
    }

    public override void EnterState()
    {
        enemyAI.SwitchEnemySprite(EnemyAI.EnemyStateType.Chase);
    }

    public override void ExitState()
    {

    }

    public override void UpdateState()
    {

    }
}
