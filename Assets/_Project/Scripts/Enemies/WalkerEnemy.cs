using UnityEngine;

public class WalkerEnemy : EnemyAI
{
    protected override void HandleMovement()
    {
        if (player == null) return;

        Vector2 toPlayer = player.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer <= stoppingDistance)
        {
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            Vector2 direction = toPlayer.normalized;
            rb.linearVelocity = direction * speed;
            RotateTowards(direction);
        }
    }
}
