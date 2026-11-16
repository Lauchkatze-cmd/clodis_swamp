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
    [SerializeReference] public BlackboardVariable<GameObject> player = new BlackboardVariable<GameObject>();

    private NavMeshAgent agent;

    protected override Status OnStart()
    {
        if (GameObject == null) return Status.Failure;

        agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return Status.Failure;
        }

        agent.isStopped = false;
        agent.speed = ChaseSpeed.Value;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return Status.Failure;
        }

        if (player.Value != null)
        {
            agent.SetDestination(player.Value.transform.position);
        }
        else
        {
            // Falls das Player-Objekt fehlt, kurz warten statt fehlschlagen
            return Status.Running;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }
}
