using UnityEngine;

public class IdleState : EnemyState
{

    float idleStateSpeed = 3f;

    public IdleState(EnemyAI enemyAI) : base(enemyAI)
    {
    }

    public override void EnterState()
    {
       enemyAI.SwitchEnemySprite(EnemyAI.EnemyStateType.Idle);
       enemyAI.speed = idleStateSpeed;
    }

    public override void ExitState()
    {
        
    }

    public override void UpdateState()
    {
        enemyAI.CheckForPlatformEdge();
        if (enemyAI.CanSeePlayer())
        {
            enemyAI.SetState(EnemyAI.EnemyStateType.Chase);
        }
        //Debug.Log($"[IdleState] Can see player: {canSeePlayer}");
    }
}
