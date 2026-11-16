using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(BehaviorGraphAgent))]
[RequireComponent(typeof(EnemyStatus))]
public class EnemySenses : MonoBehaviour
{
    [Header("Senses")]
    [SerializeField] private float viewDistance = 14f;
    [SerializeField] private float viewAngle = 120f;
    [SerializeField] private float hearingDistance = 3f;
    [SerializeField] private float eyeHeight = 1.5f;
    [SerializeField] private float attackRange = 2.5f;

    private BehaviorGraphAgent behaviorAgent;
    private EnemyStatus status;
    private Transform player;

    private void Awake()
    {
        behaviorAgent = GetComponent<BehaviorGraphAgent>();
        status = GetComponent<EnemyStatus>();
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    private void Update()
    {
        if (player == null || behaviorAgent == null) return;

        bool seesPlayer = CheckCanSeePlayer();

        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = player.position; b.y = 0f;
        bool inRange = seesPlayer && Vector3.Distance(a, b) <= attackRange;

        // Letzte bekannte Position wird nur aktualisiert, solange er den Spieler sieht.
        // Beim Sichtverlust bleibt sie also der letzte echte Sichtpunkt.
        if (seesPlayer)
        {
            behaviorAgent.SetVariableValue("lastKnownPosition", player.position);
            behaviorAgent.SetVariableValue("hasLastKnownPosition", true);
        }

        behaviorAgent.SetVariableValue("canSeePlayer", seesPlayer);
        behaviorAgent.SetVariableValue("inAttackRange", inRange);
        behaviorAgent.SetVariableValue("isStunned", status.IsStunned);
        behaviorAgent.SetVariableValue("player", player.gameObject);
    }

    private bool CheckCanSeePlayer()
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = player.position + Vector3.up * 0.75f;
        Vector3 toPlayer = targetPosition - eyePosition;
        float distance = toPlayer.magnitude;

        if (distance > viewDistance) return false;

        if (distance > hearingDistance)
        {
            Vector3 flatDirection = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (Vector3.Angle(transform.forward, flatDirection) > viewAngle * 0.5f) return false;
        }

        if (Physics.Raycast(eyePosition, toPlayer.normalized, out RaycastHit hit, distance + 0.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }
        return false;
    }
}