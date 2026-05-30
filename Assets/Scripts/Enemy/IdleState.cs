using UnityEngine;

public class IdleState : EnemyState
{
    public IdleState(EnemyAI enemyAI) : base(enemyAI)
    {
    }

    public override void EnterState()
    {
        enemyAI.SwitchEnemySprite(EnemyAI.EnemyStateType.Idle);
    }

    public override void ExitState()
    {
        
    }

    public override void UpdateState()
    {
        enemyAI.CheckForPlatformEdge();
    }
}
