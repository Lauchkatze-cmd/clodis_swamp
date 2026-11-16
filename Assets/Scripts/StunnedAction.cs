using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StunnedAction", story: "Agent stops moving and rotates while stunned", category: "Action", id: "ac30cd44eddeb8234b58d61317832763")]
public partial class StunnedAction : Action
{
    protected override Status OnStart()
    {
        var agent = GameObject.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        GameObject.transform.Rotate(0f, 180f * Time.deltaTime, 0f);
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}
