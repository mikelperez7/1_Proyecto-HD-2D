using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Calcula rutas mediante Unity NavMesh.
///
/// No mueve directamente al enemigo.
/// Proporciona a EnemyChase la dirección hacia
/// el siguiente punto de la ruta.
///
/// Si la ruta termina en un borde o en una zona no conectada,
/// permite continuar en dirección al jugador para que el Rigidbody
/// pueda abandonar físicamente una plataforma y caer.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyNavigation : MonoBehaviour
{
    [Header("Navegación")]

    [Tooltip("Tiempo entre cálculos de ruta.")]
    [SerializeField] private float repathInterval = 0.15f;

    [Tooltip("Distancia máxima para encontrar una posición válida sobre el NavMesh.")]
    [SerializeField] private float sampleDistance = 6f;

    [Tooltip("Distancia necesaria para considerar alcanzado un waypoint.")]
    [SerializeField] private float waypointReachDistance = 0.45f;

    [Tooltip("Si el enemigo aparece fuera del NavMesh, lo coloca una vez sobre la posición navegable más cercana.")]
    [SerializeField] private bool snapEnemyToNavMesh = true;

    [Tooltip("Distancia adicional utilizada para buscar posiciones navegables.")]
    [SerializeField] private float extendedSampleDistance = 12f;

    [Header("Caídas")]

    [Tooltip("Permite que el enemigo abandone físicamente una plataforma cuando la ruta NavMesh termina en un borde.")]
    [SerializeField] private bool allowEdgeDrop = true;

    [Tooltip("Distancia máxima al último punto de la ruta para permitir continuar hacia el jugador.")]
    [SerializeField] private float edgeDropDistance = 1.2f;

    [Header("Depuración")]

    [SerializeField] private bool drawPath = true;

    [SerializeField] private bool showNavigationWarnings = true;


    private Transform target;

    private NavMeshPath currentPath;

    private int currentCorner;

    private float repathTimer;

    private bool hasValidPath;

    private bool pathIsPartial;

    private bool allowingEdgeDrop;


    public bool HasValidPath
    {
        get { return hasValidPath; }
    }


    /// <summary>
    /// Indica que el enemigo está abandonando una superficie
    /// navegable para continuar físicamente hacia el jugador.
    ///
    /// EnemyChase utiliza esto para no bloquear el movimiento
    /// contra el lateral de la plataforma.
    /// </summary>
    public bool IsEdgeDropActive
    {
        get
        {
            return allowingEdgeDrop;
        }
    }


    private void Awake()
    {
        currentPath =
            new NavMeshPath();
    }


    private void Start()
    {
        BuscarJugador();

        if (snapEnemyToNavMesh)
        {
            IntentarColocarEnNavMesh();
        }

        repathTimer =
            0f;

        CalcularRuta();
    }


    private void Update()
    {
        BuscarJugador();

        if (target == null)
        {
            hasValidPath = false;
            allowingEdgeDrop = false;
            return;
        }


        repathTimer -=
            Time.deltaTime;


        if (repathTimer <= 0f)
        {
            repathTimer =
                repathInterval;

            CalcularRuta();
        }


        ActualizarWaypoint();
    }


    private void BuscarJugador()
    {
        if (target != null)
            return;


        GameObject player =
            GameObject.Find("Player");


        if (player != null)
        {
            target =
                player.transform;
        }
    }


    private bool BuscarPosicionNavMesh(
        Vector3 position,
        out NavMeshHit hit
    )
    {
        if (
            NavMesh.SamplePosition(
                position,
                out hit,
                sampleDistance,
                NavMesh.AllAreas
            )
        )
        {
            return true;
        }


        if (
            NavMesh.SamplePosition(
                position,
                out hit,
                extendedSampleDistance,
                NavMesh.AllAreas
            )
        )
        {
            return true;
        }


        hit =
            new NavMeshHit();


        return false;
    }


    private void IntentarColocarEnNavMesh()
    {
        NavMeshHit hit;


        bool found =
            BuscarPosicionNavMesh(
                transform.position,
                out hit
            );


        if (!found)
        {
            if (showNavigationWarnings)
            {
                Debug.LogWarning(
                    "[EnemyNavigation] " +
                    "No se encontró NavMesh cerca del enemigo. " +
                    "Posición: " +
                    transform.position
                );
            }


            return;
        }


        float distance =
            Vector3.Distance(
                transform.position,
                hit.position
            );


        if (distance > 0.05f)
        {
            transform.position =
                hit.position;


            Debug.Log(
                "[EnemyNavigation] " +
                "Enemy_Test colocado sobre el NavMesh. " +
                "Distancia corregida: " +
                distance.ToString("F2")
            );
        }
    }


    private void CalcularRuta()
    {
        if (target == null)
        {
            hasValidPath = false;
            pathIsPartial = false;
            allowingEdgeDrop = false;
            return;
        }


        NavMeshHit enemyHit;


        bool enemyFound =
            BuscarPosicionNavMesh(
                transform.position,
                out enemyHit
            );


        if (!enemyFound)
        {
            hasValidPath = false;
            pathIsPartial = false;
            allowingEdgeDrop = false;


            if (showNavigationWarnings)
            {
                Debug.LogWarning(
                    "[EnemyNavigation] " +
                    "El enemigo no está cerca de un NavMesh válido. " +
                    "Posición: " +
                    transform.position
                );
            }


            return;
        }


        NavMeshHit playerHit;


        bool playerFound =
            BuscarPosicionNavMesh(
                target.position,
                out playerHit
            );


        if (!playerFound)
        {
            hasValidPath = false;
            pathIsPartial = false;
            allowingEdgeDrop = false;


            if (showNavigationWarnings)
            {
                Debug.LogWarning(
                    "[EnemyNavigation] " +
                    "No se encontró NavMesh cerca del Player. " +
                    "Posición: " +
                    target.position
                );
            }


            return;
        }


        currentPath.ClearCorners();


        bool pathFound =
            NavMesh.CalculatePath(
                enemyHit.position,
                playerHit.position,
                NavMesh.AllAreas,
                currentPath
            );


        if (!pathFound)
        {
            hasValidPath = false;
            pathIsPartial = false;
            allowingEdgeDrop = false;


            if (showNavigationWarnings)
            {
                Debug.LogWarning(
                    "[EnemyNavigation] " +
                    "NavMesh.CalculatePath no pudo crear una ruta."
                );
            }


            return;
        }


        // ================================================================
        // RUTA COMPLETA
        // ================================================================

        if (
            currentPath.status ==
            NavMeshPathStatus.PathComplete
        )
        {
            if (
                currentPath.corners == null ||
                currentPath.corners.Length < 2
            )
            {
                hasValidPath = false;
                pathIsPartial = false;
                allowingEdgeDrop = false;
                return;
            }


            currentCorner =
                1;


            hasValidPath =
                true;


            pathIsPartial =
                false;


            allowingEdgeDrop =
                false;


            return;
        }


        // ================================================================
        // RUTA PARCIAL
        // ================================================================

        if (
            currentPath.status ==
            NavMeshPathStatus.PathPartial
        )
        {
            if (
                currentPath.corners != null &&
                currentPath.corners.Length >= 2
            )
            {
                currentCorner =
                    1;


                hasValidPath =
                    true;


                pathIsPartial =
                    true;


                /*
                 * No activamos todavía la caída.
                 *
                 * Primero el enemigo debe llegar al último
                 * punto navegable.
                 */

                allowingEdgeDrop =
                    false;


                return;
            }
        }


        // ================================================================
        // RUTA INVÁLIDA
        // ================================================================

        hasValidPath =
            false;


        pathIsPartial =
            false;


        allowingEdgeDrop =
            false;


        if (showNavigationWarnings)
        {
            Debug.LogWarning(
                "[EnemyNavigation] " +
                "La ruta no está disponible. Estado: " +
                currentPath.status
            );
        }
    }


    private void ActualizarWaypoint()
    {
        if (!hasValidPath)
            return;


        if (
            currentPath.corners == null ||
            currentPath.corners.Length == 0
        )
        {
            hasValidPath = false;
            return;
        }


        if (
            currentCorner >=
            currentPath.corners.Length
        )
        {
            currentCorner =
                currentPath.corners.Length - 1;
        }


        Vector3 waypoint =
            currentPath.corners[currentCorner];


        Vector3 horizontalWaypoint =
            new Vector3(
                waypoint.x,
                transform.position.y,
                waypoint.z
            );


        float distance =
            Vector3.Distance(
                transform.position,
                horizontalWaypoint
            );


        /*
         * Hemos llegado al waypoint actual.
         */

        if (
            distance <=
            waypointReachDistance
        )
        {
            if (
                currentCorner <
                currentPath.corners.Length - 1
            )
            {
                currentCorner++;
            }
            else
            {
                /*
                 * Hemos llegado al último punto de una ruta parcial.
                 *
                 * A partir de aquí queremos que el enemigo continúe
                 * físicamente hacia el jugador.
                 */

                if (
                    pathIsPartial &&
                    allowEdgeDrop
                )
                {
                    allowingEdgeDrop =
                        true;
                }
            }
        }
    }


    public Vector3 GetDirection()
    {
        if (!hasValidPath)
            return Vector3.zero;


        // ================================================================
        // CAÍDA DESDE BORDE
        // ================================================================

        if (
            allowingEdgeDrop &&
            pathIsPartial &&
            allowEdgeDrop &&
            target != null
        )
        {
            Vector3 directionToPlayer =
                target.position -
                transform.position;


            directionToPlayer.y =
                0f;


            if (
                directionToPlayer.sqrMagnitude >
                0.001f
            )
            {
                return
                    directionToPlayer.normalized;
            }
        }


        if (
            currentPath.corners == null ||
            currentPath.corners.Length == 0
        )
        {
            return Vector3.zero;
        }


        if (
            currentCorner >=
            currentPath.corners.Length
        )
        {
            return Vector3.zero;
        }


        Vector3 waypoint =
            currentPath.corners[currentCorner];


        Vector3 direction =
            waypoint -
            transform.position;


        direction.y =
            0f;


        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            /*
             * Último recurso para una ruta parcial.
             */

            if (
                pathIsPartial &&
                allowEdgeDrop &&
                target != null
            )
            {
                Vector3 fallbackDirection =
                    target.position -
                    transform.position;


                fallbackDirection.y =
                    0f;


                if (
                    fallbackDirection.sqrMagnitude >
                    0.001f
                )
                {
                    allowingEdgeDrop =
                        true;


                    return
                        fallbackDirection.normalized;
                }
            }


            return Vector3.zero;
        }


        return
            direction.normalized;
    }


    public float GetDistanceToTarget()
    {
        if (!hasValidPath)
            return Mathf.Infinity;


        if (
            currentPath.corners == null ||
            currentPath.corners.Length < 2
        )
        {
            return Mathf.Infinity;
        }


        float distance =
            0f;


        for (
            int i = currentCorner;
            i < currentPath.corners.Length - 1;
            i++
        )
        {
            distance +=
                Vector3.Distance(
                    currentPath.corners[i],
                    currentPath.corners[i + 1]
                );
        }


        if (
            allowingEdgeDrop &&
            target != null
        )
        {
            distance +=
                Vector3.Distance(
                    transform.position,
                    target.position
                );
        }


        return distance;
    }


    public void ResetNavigation()
    {
        target = null;

        currentPath.ClearCorners();

        currentCorner = 0;

        repathTimer = 0f;

        hasValidPath = false;

        pathIsPartial = false;

        allowingEdgeDrop = false;
    }


    private void OnDrawGizmos()
    {
        if (!drawPath)
            return;


        if (!hasValidPath)
            return;


        if (currentPath == null)
            return;


        Vector3[] corners =
            currentPath.corners;


        if (
            corners == null ||
            corners.Length < 2
        )
        {
            return;
        }


        Gizmos.color =
            Color.cyan;


        for (
            int i = 0;
            i < corners.Length - 1;
            i++
        )
        {
            Gizmos.DrawLine(
                corners[i],
                corners[i + 1]
            );


            Gizmos.DrawSphere(
                corners[i],
                0.12f
            );
        }
    }
}