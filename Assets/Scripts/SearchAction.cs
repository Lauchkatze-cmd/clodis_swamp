using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SearchAction", story: "Agent searches at last known position", category: "Action", id: "ac30cd44eddeb8234b58d61317832767")]
public partial class SearchAction : Action
{
    [SerializeReference] public BlackboardVariable<bool> hasLastKnownPosition;
    [SerializeReference] public BlackboardVariable<Vector3> lastKnownPosition;
    [SerializeReference] public BlackboardVariable<float> SearchDuration = new BlackboardVariable<float>(4f);
    [SerializeReference] public BlackboardVariable<bool> canSeePlayer;
    [SerializeReference] public BlackboardVariable<bool> isStunned;

    private float searchTimer;

    protected override Status OnStart()
    {
        if (canSeePlayer.Value || isStunned.Value) return Status.Success;

        var agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = 2f;
            agent.SetDestination(lastKnownPosition.Value);
        }
        searchTimer = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (canSeePlayer.Value || isStunned.Value) return Status.Success;

        var agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
            {
                searchTimer += Time.deltaTime;
                GameObject.transform.Rotate(0f, 90f * Time.deltaTime, 0f);

                if (searchTimer >= SearchDuration.Value)
                {
                    hasLastKnownPosition.Value = false;
                    return Status.Success;
                }
            }
        }
        return Status.Running;
    }

    protected override void OnEnd() { }
}