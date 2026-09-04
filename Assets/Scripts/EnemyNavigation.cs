using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Calcula rutas mediante Unity NavMesh.
///
/// No mueve directamente al enemigo.
/// Proporciona a EnemyChase la dirección hacia
/// el siguiente punto de la ruta.
///
/// El Rigidbody continúa siendo responsable del movimiento físico.
/// El sistema añade una separación anticipada respecto a paredes
/// y obstáculos para evitar que el enemigo se quede pegado a ellos.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyNavigation : MonoBehaviour
{
    [Header("Navegación")]
    [Tooltip("Tiempo entre cálculos de ruta.")]
    [SerializeField] private float repathInterval = 0.15f;

    [Tooltip("Distancia máxima para encontrar una posición válida sobre el NavMesh.")]
    [SerializeField] private float sampleDistance = 1.5f;

    [Tooltip("Distancia necesaria para considerar alcanzado un waypoint.")]
    [SerializeField] private float waypointReachDistance = 0.45f;

    [Tooltip("Si el enemigo aparece fuera del NavMesh, lo coloca una vez sobre la posición navegable más cercana.")]
    [SerializeField] private bool snapEnemyToNavMesh = true;

    [Tooltip("Distancia adicional utilizada como búsqueda de emergencia.")]
    [SerializeField] private float extendedSampleDistance = 4f;

    [Header("Caídas")]
    [Tooltip("Permite que el enemigo abandone físicamente una plataforma cuando la ruta NavMesh termina en un borde.")]
    [SerializeField] private bool allowEdgeDrop = true;

    [Tooltip("Distancia máxima al último punto de la ruta para permitir continuar hacia el jugador.")]
    [SerializeField] private float edgeDropDistance = 1.2f;

    [Header("Margen de paredes")]
    [Tooltip("Distancia adicional que el enemigo intenta mantener respecto a paredes y obstáculos.")]
    [SerializeField] private float wallClearance = 0.30f;

    [Tooltip("Distancia que se comprueba por delante del enemigo para anticipar una pared.")]
    [SerializeField] private float wallLookAhead = 0.80f;

    [Tooltip("Intensidad con la que se corrige la dirección cuando se detecta un obstáculo.")]
    [SerializeField] private float wallAvoidanceStrength = 1.0f;

    [Tooltip("Capas que contienen paredes y obstáculos físicos.")]
    [SerializeField] private LayerMask wallLayerMask = Physics.DefaultRaycastLayers;

    [Header("Contacto con paredes")]
    [Tooltip("Tiempo durante el que se considera válido el último contacto con una pared.")]
    [SerializeField] private float wallContactMemory = 0.15f;

    [Header("Estabilidad de evasión")]
    [Tooltip("Tiempo durante el que el enemigo mantiene el lado elegido al rodear un obstáculo.")]
    [SerializeField] private float avoidanceCommitTime = 0.35f;

    [Tooltip("Distancia máxima de cambio de obstáculo antes de olvidar el lado de evasión anterior.")]
    [SerializeField] private float avoidanceNormalChangeThreshold = 0.35f;

    [Header("Depuración")]
    [SerializeField] private bool drawPath = true;

    [SerializeField] private bool showNavigationWarnings = true;

    private Transform target;

    private Rigidbody rb;

    private NavMeshPath currentPath;

    private int currentCorner;

    private float repathTimer;

    private bool hasValidPath;

    private bool pathIsPartial;

    private bool allowingEdgeDrop;

    private Vector3 wallAvoidanceNormal;

    private float lastWallContactTime =
        -Mathf.Infinity;

    // -1 = izquierda
    //  1 = derecha
    //  0 = sin decisión guardada
    private int committedAvoidanceSide;

    private float avoidanceCommitUntil =
        -Mathf.Infinity;

    private Vector3 committedAvoidanceNormal =
        Vector3.zero;

    public bool HasValidPath
    {
        get
        {
            return hasValidPath;
        }
    }

    public bool IsEdgeDropActive
    {
        get
        {
            return allowingEdgeDrop;
        }
    }

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody>();

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
            pathIsPartial = false;
            allowingEdgeDrop = false;

            LimpiarEvasionComprometida();

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
        /*
         * Primera búsqueda: muy cerca de la posición real.
         */
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

        /*
         * Segunda búsqueda: emergencia.
         */
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

            if (rb != null)
            {
                rb.linearVelocity =
                    Vector3.zero;

                rb.angularVelocity =
                    Vector3.zero;
            }

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
            /*
             * Mientras el enemigo está cayendo puede encontrarse
             * temporalmente fuera del NavMesh.
             */
            if (!allowingEdgeDrop)
            {
                hasValidPath =
                    false;

                pathIsPartial =
                    false;
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
            if (!pathIsPartial)
            {
                hasValidPath =
                    false;
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
                currentPath.corners.Length == 0
            )
            {
                hasValidPath =
                    false;

                pathIsPartial =
                    false;

                allowingEdgeDrop =
                    false;

                LimpiarEvasionComprometida();

                return;
            }

            pathIsPartial =
                false;

            allowingEdgeDrop =
                false;

            hasValidPath =
                true;

            ActualizarCornerTrasNuevaRuta();

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
            pathIsPartial =
                true;

            hasValidPath =
                true;

            if (
                currentPath.corners != null &&
                currentPath.corners.Length >= 2
            )
            {
                ActualizarCornerTrasNuevaRuta();

                if (
                    EsquinaActualEsUltima() &&
                    DistanciaAlCornerActual() <=
                    edgeDropDistance
                )
                {
                    allowingEdgeDrop =
                        allowEdgeDrop;
                }

                return;
            }

            /*
             * No existe un segundo punto navegable.
             * Permitimos abandonar físicamente la superficie.
             */
            currentCorner =
                0;

            allowingEdgeDrop =
                allowEdgeDrop;

            return;
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

        LimpiarEvasionComprometida();

        if (showNavigationWarnings)
        {
            Debug.LogWarning(
                "[EnemyNavigation] " +
                "La ruta no está disponible. Estado: " +
                currentPath.status
            );
        }
    }

    private void ActualizarCornerTrasNuevaRuta()
    {
        if (
            currentPath == null ||
            currentPath.corners == null ||
            currentPath.corners.Length == 0
        )
        {
            currentCorner =
                0;

            return;
        }

        /*
         * Buscamos el primer corner que todavía no se ha alcanzado.
         */
        int startCorner =
            0;

        for (
            int i = 0;
            i < currentPath.corners.Length;
            i++
        )
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    currentPath.corners[i]
                );

            if (
                distance >
                waypointReachDistance
            )
            {
                startCorner =
                    i;

                break;
            }

            startCorner =
                i;
        }

        /*
         * El corner 0 normalmente representa el punto de partida.
         */
        if (
            currentPath.corners.Length > 1 &&
            startCorner == 0
        )
        {
            startCorner =
                1;
        }

        currentCorner =
            Mathf.Clamp(
                startCorner,
                0,
                currentPath.corners.Length - 1
            );
    }

    private void ActualizarWaypoint()
    {
        if (!hasValidPath)
            return;

        if (
            currentPath == null ||
            currentPath.corners == null ||
            currentPath.corners.Length == 0
        )
        {
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

        float distance =
            DistanciaAlCornerActual();

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

                return;
            }

            if (
                pathIsPartial &&
                allowEdgeDrop
            )
            {
                allowingEdgeDrop =
                    true;
            }
        }
        else if (
            pathIsPartial &&
            allowEdgeDrop &&
            EsquinaActualEsUltima() &&
            distance <= edgeDropDistance
        )
        {
            allowingEdgeDrop =
                true;
        }
    }

    private bool EsquinaActualEsUltima()
    {
        if (
            currentPath == null ||
            currentPath.corners == null ||
            currentPath.corners.Length == 0
        )
        {
            return false;
        }

        return
            currentCorner ==
            currentPath.corners.Length - 1;
    }

    private float DistanciaAlCornerActual()
    {
        if (
            currentPath == null ||
            currentPath.corners == null ||
            currentPath.corners.Length == 0
        )
        {
            return Mathf.Infinity;
        }

        int corner =
            Mathf.Clamp(
                currentCorner,
                0,
                currentPath.corners.Length - 1
            );

        Vector3 cornerPosition =
            currentPath.corners[corner];

        Vector3 horizontalCorner =
            new Vector3(
                cornerPosition.x,
                transform.position.y,
                cornerPosition.z
            );

        return Vector3.Distance(
            transform.position,
            horizontalCorner
        );
    }

    private Vector3 ObtenerDireccionRuta()
    {
        if (
            currentPath == null ||
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
            currentCorner =
                currentPath.corners.Length - 1;
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
            if (
                currentCorner <
                currentPath.corners.Length - 1
            )
            {
                currentCorner++;

                return ObtenerDireccionRuta();
            }

            return Vector3.zero;
        }

        return
            direction.normalized;
    }

    private bool HayObstaculoDelante(
        Vector3 direction,
        out RaycastHit hit
    )
    {
        hit =
            new RaycastHit();

        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            return false;
        }

        direction.y =
            0f;

        direction.Normalize();

        /*
         * Nuestro Box Collider mide 1x1x1.
         *
         * Utilizamos aproximadamente la mitad de su anchura
         * como radio físico para el cast y añadimos el margen
         * configurable.
         */
        float castRadius =
            0.5f +
            Mathf.Max(
                0f,
                wallClearance
            );

        /*
         * Colocamos el origen ligeramente por encima del suelo
         * para que el cast no detecte la propia superficie como
         * obstáculo.
         */
        Vector3 origin =
            transform.position;

        origin.y +=
            0.5f;

        return
            Physics.SphereCast(
                origin,
                castRadius,
                direction,
                out hit,
                wallLookAhead,
                wallLayerMask,
                QueryTriggerInteraction.Ignore
            );
    }

    private Vector3 ObtenerDireccionAlrededorDelObstaculo(
        Vector3 desiredDirection,
        RaycastHit hit
    )
    {
        Vector3 normal =
            hit.normal;

        normal.y =
            0f;

        if (
            normal.sqrMagnitude <=
            0.001f
        )
        {
            return desiredDirection.normalized;
        }

        normal.Normalize();

        Vector3 left =
            Vector3.Cross(
                Vector3.up,
                normal
            ).normalized;

        Vector3 right =
            -left;

        // ================================================================
        // 1. Si ya estamos rodeando este mismo obstáculo,
        //    mantenemos el lado elegido.
        // ================================================================

        bool sameObstacle =
            committedAvoidanceSide != 0 &&
            committedAvoidanceNormal.sqrMagnitude > 0.001f &&
            Vector3.Dot(
                committedAvoidanceNormal,
                normal
            ) >=
            (1f - avoidanceNormalChangeThreshold);

        if (
            sameObstacle &&
            Time.time < avoidanceCommitUntil
        )
        {
            Vector3 committedDirection =
                committedAvoidanceSide < 0
                    ? left
                    : right;

            if (
                DireccionEsNavegable(
                    committedDirection
                )
            )
            {
                return committedDirection;
            }
        }

        // ================================================================
        // 2. Elegimos el lado que más conserva la dirección original.
        // ================================================================

        float leftScore =
            Vector3.Dot(
                left,
                desiredDirection
            );

        float rightScore =
            Vector3.Dot(
                right,
                desiredDirection
            );

        Vector3 firstDirection;
        Vector3 secondDirection;

        int firstSide;

        if (leftScore >= rightScore)
        {
            firstDirection =
                left;

            secondDirection =
                right;

            firstSide =
                -1;
        }
        else
        {
            firstDirection =
                right;

            secondDirection =
                left;

            firstSide =
                1;
        }

        // ================================================================
        // 3. Primera opción.
        // ================================================================

        if (
            DireccionEsNavegable(
                firstDirection
            )
        )
        {
            committedAvoidanceSide =
                firstSide;

            committedAvoidanceNormal =
                normal;

            avoidanceCommitUntil =
                Time.time +
                avoidanceCommitTime;

            return firstDirection;
        }

        // ================================================================
        // 4. Segunda opción.
        // ================================================================

        if (
            DireccionEsNavegable(
                secondDirection
            )
        )
        {
            committedAvoidanceSide =
                -firstSide;

            committedAvoidanceNormal =
                normal;

            avoidanceCommitUntil =
                Time.time +
                avoidanceCommitTime;

            return secondDirection;
        }

        // ================================================================
        // 5. Último recurso:
        //    pequeña separación respecto al obstáculo.
        // ================================================================

        Vector3 separation =
            normal *
            wallAvoidanceStrength;

        separation.y =
            0f;

        Vector3 correctedDirection =
            desiredDirection +
            separation;

        if (
            correctedDirection.sqrMagnitude >
            0.001f
        )
        {
            return
                correctedDirection.normalized;
        }

        return
            desiredDirection.normalized;
    }

    private bool DireccionEsNavegable(
        Vector3 direction
    )
    {
        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            return false;
        }

        direction.Normalize();

        /*
         * Comprobamos un punto situado por delante.
         */
        Vector3 testPosition =
            transform.position +
            direction *
            Mathf.Max(
                wallLookAhead,
                0.5f
            );

        testPosition.y =
            transform.position.y;

        NavMeshHit hit;

        if (
            !NavMesh.SamplePosition(
                testPosition,
                out hit,
                0.8f,
                NavMesh.AllAreas
            )
        )
        {
            return false;
        }

        /*
         * Comprobamos además que el recorrido sobre el NavMesh
         * desde nuestra posición hasta ese punto no esté bloqueado.
         */
        NavMeshHit rayHit;

        bool blocked =
            NavMesh.Raycast(
                transform.position,
                hit.position,
                out rayHit,
                NavMesh.AllAreas
            );

        return !blocked;
    }

    private Vector3 ObtenerDireccionConMargen(
        Vector3 routeDirection
    )
    {
        if (
            routeDirection.sqrMagnitude <=
            0.001f
        )
        {
            return Vector3.zero;
        }

        routeDirection.Normalize();

        RaycastHit obstacleHit;

        if (
            HayObstaculoDelante(
                routeDirection,
                out obstacleHit
            )
        )
        {
            return
                ObtenerDireccionAlrededorDelObstaculo(
                    routeDirection,
                    obstacleHit
                ).normalized;
        }

        if (
            Time.time -
            lastWallContactTime <=
            wallContactMemory
        )
        {
            if (
                wallAvoidanceNormal.sqrMagnitude >
                0.001f
            )
            {
                Vector3 normal =
                    wallAvoidanceNormal.normalized;

                normal.y =
                    0f;

                float intoWall =
                    Vector3.Dot(
                        routeDirection,
                        -normal
                    );

                if (
                    intoWall >
                    0.05f
                )
                {
                    Vector3 correctedDirection =
                        routeDirection +
                        normal *
                        wallAvoidanceStrength;

                    correctedDirection.y =
                        0f;

                    if (
                        correctedDirection.sqrMagnitude >
                        0.001f
                    )
                    {
                        return
                            correctedDirection.normalized;
                    }
                }
            }
        }

        return routeDirection;
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
                directionToPlayer.Normalize();

                RaycastHit obstacleHit;

                /*
                 * Si está libre, mantenemos el comportamiento original:
                 * el enemigo puede abandonar el borde para ir directamente
                 * hacia el jugador.
                 */
                if (
                    !HayObstaculoDelante(
                        directionToPlayer,
                        out obstacleHit
                    )
                )
                {
                    return directionToPlayer;
                }

                /*
                 * Si existe una estructura delante,
                 * NO usamos la dirección directa al jugador.
                 * Dejamos que la ruta y la evasión decidan.
                 */
            }
        }

        // ================================================================
        // RUTA NORMAL
        // ================================================================

        Vector3 routeDirection =
            ObtenerDireccionRuta();

        if (
            routeDirection.sqrMagnitude <=
            0.001f
        )
        {
            /*
             * Último recurso únicamente durante una caída.
             */
            if (
                allowingEdgeDrop &&
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
                    return
                        fallbackDirection.normalized;
                }
            }

            return Vector3.zero;
        }

        return
            ObtenerDireccionConMargen(
                routeDirection
            );
    }

    public float GetDistanceToTarget()
    {
        if (!hasValidPath)
            return Mathf.Infinity;

        if (
            currentPath == null ||
            currentPath.corners == null ||
            currentPath.corners.Length == 0
        )
        {
            return Mathf.Infinity;
        }

        float distance =
            0f;

        if (
            currentCorner <
            currentPath.corners.Length - 1
        )
        {
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

    private void LimpiarEvasionComprometida()
    {
        committedAvoidanceSide =
            0;

        committedAvoidanceNormal =
            Vector3.zero;

        avoidanceCommitUntil =
            -Mathf.Infinity;
    }

    public void ResetNavigation()
    {
        target =
            null;

        currentPath.ClearCorners();

        currentCorner =
            0;

        repathTimer =
            0f;

        hasValidPath =
            false;

        pathIsPartial =
            false;

        allowingEdgeDrop =
            false;

        wallAvoidanceNormal =
            Vector3.zero;

        lastWallContactTime =
            -Mathf.Infinity;

        LimpiarEvasionComprometida();
    }

    private void OnCollisionStay(
        Collision collision
    )
    {
        Vector3 accumulatedNormal =
            Vector3.zero;

        bool foundWall =
            false;

        foreach (
            ContactPoint contact
            in collision.contacts
        )
        {
            Vector3 normal =
                contact.normal;

            /*
             * Las superficies con una normal principalmente
             * horizontal se consideran paredes/estructuras.
             */
            if (
                Mathf.Abs(normal.y) <
                0.5f
            )
            {
                accumulatedNormal +=
                    normal;

                foundWall =
                    true;
            }
        }

        if (foundWall)
        {
            if (
                accumulatedNormal.sqrMagnitude >
                0.001f
            )
            {
                wallAvoidanceNormal =
                    accumulatedNormal.normalized;

                lastWallContactTime =
                    Time.time;
            }
        }
    }

    private void OnCollisionExit(
        Collision collision
    )
    {
        /*
         * Conservamos brevemente la última normal de contacto
         * para evitar perder la separación justo al pasar por
         * una esquina formada por varios colliders.
         */
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

        /*
         * Normal de separación de la última pared detectada.
         */
        if (
            wallAvoidanceNormal.sqrMagnitude >
            0.001f &&
            Time.time -
            lastWallContactTime <=
            wallContactMemory
        )
        {
            Gizmos.color =
                Color.yellow;

            Gizmos.DrawRay(
                transform.position,
                wallAvoidanceNormal.normalized
            );
        }

        /*
         * Mostramos también la dirección de ruta actual.
         */
        if (hasValidPath)
        {
            Vector3 direction =
                ObtenerDireccionRuta();

            if (
                direction.sqrMagnitude >
                0.001f
            )
            {
                Gizmos.color =
                    Color.green;

                Gizmos.DrawRay(
                    transform.position,
                    direction.normalized
                );
            }
        }
    }
}