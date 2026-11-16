using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AttackAction", story: "Agent attacks Player in range", category: "Action", id: "ac30cd44eddeb8234b58d61317832764")]
public partial class AttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> player;
    [SerializeReference] public BlackboardVariable<float> AttackCooldown = new BlackboardVariable<float>(1.2f);
    [SerializeReference] public BlackboardVariable<float> AttackDamage = new BlackboardVariable<float>(1f);
    [SerializeReference] public BlackboardVariable<bool> canSeePlayer;
    [SerializeReference] public BlackboardVariable<bool> inAttackRange;
    [SerializeReference] public BlackboardVariable<bool> isStunned;

    private float nextAttackTime;

    protected override Status OnStart()
    {
        if (ShouldStop()) return Status.Success;

        var agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (ShouldStop()) return Status.Success;
        if (player.Value == null) return Status.Failure;

        Vector3 direction = player.Value.transform.position - GameObject.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            GameObject.transform.rotation = Quaternion.Slerp(GameObject.transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + AttackCooldown.Value;
            var playerHealth = player.Value.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(Mathf.RoundToInt(AttackDamage.Value));
        }
        return Status.Running;
    }

    protected override void OnEnd()
    {
        var agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;
    }

    private bool ShouldStop()
    {
        return isStunned.Value || !canSeePlayer.Value || !inAttackRange.Value;
    }
}