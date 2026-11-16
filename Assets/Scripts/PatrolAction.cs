using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PatrolAction", story: "Agent patrols automatically found waypoints", category: "Action", id: "ac30cd44eddeb8234b58d61317832768")]
public partial class PatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<float> PatrolSpeed = new BlackboardVariable<float>(2f);
    [SerializeReference] public BlackboardVariable<float> WaitTime = new BlackboardVariable<float>(1f);

    private List<Transform> waypoints = new List<Transform>();
    private float waitTimer;
    private int patrolIndex = 0;
    private Transform currentTarget;

    protected override Status OnStart()
    {
        var agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = PatrolSpeed.Value;
        }

        if (waypoints.Count == 0 && GameObject != null)
        {
            // Sucht jetzt weltweit in der Szene nach dem Ordner (z.B. "SwampGuard_South_Waypoints")
            string targetWaypointName = GameObject.name + "_Waypoints";
            GameObject waypointParent = GameObject.Find(targetWaypointName);

            if (waypointParent != null)
            {
                waypoints.Clear();
                foreach (Transform child in waypointParent.transform)
                {
                    waypoints.Add(child);
                }
                Debug.Log($"{waypoints.Count} Wegpunkte aus '{targetWaypointName}' für {GameObject.name} geladen.");
            }
            else
            {
                Debug.LogWarning($"Welt-Ordner '{targetWaypointName}' wurde nicht gefunden!");
            }
        }

        waitTimer = 0f;
        patrolIndex = 0;
        currentTarget = null;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || waypoints.Count == 0) 
        {
            return Status.Running;
        }

        Transform targetPoint = waypoints[patrolIndex];
        if (targetPoint == null)
        {
            patrolIndex = (patrolIndex + 1) % waypoints.Count;
            return Status.Running;
        }

        if (currentTarget != targetPoint)
        {
            currentTarget = targetPoint;
            agent.SetDestination(targetPoint.position);
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= WaitTime.Value)
            {
                waitTimer = 0f;
                patrolIndex = (patrolIndex + 1) % waypoints.Count;
                currentTarget = null;
            }
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        var agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }
}
