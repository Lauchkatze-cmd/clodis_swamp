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
    [SerializeReference] public BlackboardVariable<int> AttackDamage = new BlackboardVariable<int>(1);

    private float nextAttackTime;
    private NavMeshAgent agent;

    protected override Status OnStart()
    {
        var agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true; // Korrigiert von player auf agent
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (player == null || player.Value == null)
        {
            return Status.Running;
        }

        // Zum Player ausrichten
        Vector3 direction = player.Value.transform.position - GameObject.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            GameObject.transform.rotation = Quaternion.Slerp(GameObject.transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        // Schaden zufügen nach Cooldown
        float cooldown = AttackCooldown != null ? AttackCooldown.Value : 1.2f;
        int damage = AttackDamage != null ? AttackDamage.Value : 1;

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + cooldown;
            var playerHealth = player.Value.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
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
