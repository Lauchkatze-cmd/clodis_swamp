using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(BehaviorGraphAgent))]
public class EnemySenses : MonoBehaviour
{
    [Header("Senses")]
    [SerializeField] private float viewDistance = 14f;
    [SerializeField] private float viewAngle = 120f;
    [SerializeField] private float hearingDistance = 3f;
    [SerializeField] private float eyeHeight = 1.5f;
    [SerializeField] private float attackRange = 2.5f; // Ab welcher Distanz wird zugeschlagen?

    private BehaviorGraphAgent behaviorAgent;
    private Transform player;
    
    // Um den Übergang von Chase zu Search zu erkennen
    private bool lastSeenState = false;

    private void Awake()
    {
        behaviorAgent = GetComponent<BehaviorGraphAgent>();
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (player == null || behaviorAgent == null) return;

        // 1. Sichtprüfung ausführen
        bool seesPlayer = CheckCanSeePlayer();

        // 2. Distanz für den Angriff prüfen
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool inRange = seesPlayer && (distanceToPlayer <= attackRange);

        // 3. Letzte bekannte Position merken (wichtig für Search, falls Sicht verloren geht)
        if (seesPlayer)
        {
            behaviorAgent.SetVariableValue("lastKnownPosition", player.position);
            behaviorAgent.SetVariableValue("hasLastKnownPosition", true);
        }
        else if (lastSeenState && !seesPlayer)
        {
            // Spieler wurde gerade eben aus den Augen verloren -> Suche aktivieren
            behaviorAgent.SetVariableValue("hasLastKnownPosition", true);
            behaviorAgent.SetVariableValue("lastKnownPosition", player.position);
        }

        lastSeenState = seesPlayer;

        // Werte in den Blackboard-Graph schreiben
        behaviorAgent.SetVariableValue("canSeePlayer", seesPlayer);
        behaviorAgent.SetVariableValue("inAttackRange", inRange);
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
