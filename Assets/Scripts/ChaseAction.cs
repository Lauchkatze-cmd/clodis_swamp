using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ChaseAction", story: "Agent chases player at chase speed", category: "Action", id: "cc40cd44eddeb8234b58d61317832799")]
public partial class ChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<float> ChaseSpeed = new BlackboardVariable<float>(3.8f);
    [SerializeReference] public BlackboardVariable<GameObject> player;
    [SerializeReference] public BlackboardVariable<bool> canSeePlayer;
    [SerializeReference] public BlackboardVariable<bool> inAttackRange;
    [SerializeReference] public BlackboardVariable<bool> isStunned;

    private NavMeshAgent agent;

    protected override Status OnStart()
    {
        if (!canSeePlayer.Value)
            Debug.LogWarning("ChaseAction: canSeePlayer ist false. Ist das Feld im Node verlinkt?");
        if (ShouldStop()) return Status.Success;

        agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return Status.Failure;

        agent.isStopped = false;
        agent.speed = ChaseSpeed.Value;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (ShouldStop()) return Status.Success;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return Status.Failure;

        if (player.Value != null)
            agent.SetDestination(player.Value.transform.position);

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;
    }

    private bool ShouldStop()
    {
        return isStunned.Value || inAttackRange.Value || !canSeePlayer.Value;
    }
}