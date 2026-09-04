using UnityEngine;

/// <summary>
/// Control de persecución y combate del enemigo.
///
/// El movimiento físico continúa realizándose mediante Rigidbody.
/// EnemyNavigation proporciona la dirección de navegación.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyNavigation))]
public class EnemyChase : MonoBehaviour
{
    [Header("Persecución")]

    [Tooltip("Distancia a la que el enemigo deja de intentar acercarse.")]
    [SerializeField] private float stoppingDistance = 0f;

    [Tooltip("Multiplicador de velocidad cuando el jugador está lejos.")]
    [SerializeField] private float cautiousSpeedMultiplier = 0.5f;

    [Tooltip("Distancia a la que el enemigo pasa a comportamiento agresivo.")]
    [SerializeField] private float aggressiveDistance = 1.8f;

    [Tooltip("Multiplicador de velocidad en comportamiento agresivo.")]
    [SerializeField] private float aggressiveSpeedMultiplier = 0.9f;


    [Header("Daño de Contacto")]

    [SerializeField] private float contactDamage = 30f;

    [SerializeField] private float contactDamageCooldown = 1f;


    [Header("Knockback del Jugador")]

    [SerializeField] private float playerKnockback = 1f;


    [Header("Knockback del Enemigo")]

    [SerializeField] private float enemyKnockback = 3.5f;

    [SerializeField] private float knockbackUpward = 1.5f;

    [SerializeField] private float enemyKnockbackDuration = 0.2f;


    private Rigidbody rb;

    private EnemyNavigation navigation;

    private Transform player;

    private PlayerHealth playerHealth;

    private float contactCooldownTimer;

    private Vector3 knockbackVelocity;

    private float knockbackTimer;

    private bool isKnockedBack;

    private Vector3 wallNormal;

    private bool touchingWall;

    private Collider currentWallCollider;


    private void Awake()
    {
        rb =
            GetComponent<Rigidbody>();


        navigation =
            GetComponent<EnemyNavigation>();


        ConfigurarRigidbody();
    }


    private void Start()
    {
        BuscarJugador();
    }


    private void Update()
    {
        BuscarJugador();


        if (contactCooldownTimer > 0f)
        {
            contactCooldownTimer -=
                Time.deltaTime;
        }


        ActualizarKnockback();
    }


    private void FixedUpdate()
    {
        if (rb == null)
            return;


        EnemyHealth health =
            GetComponent<EnemyHealth>();


        if (
            health != null &&
            !health.IsAlive
        )
        {
            return;
        }


        if (isKnockedBack)
        {
            AplicarKnockback();
            return;
        }


        MoverHaciaJugador();
    }


    private void ConfigurarRigidbody()
    {
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;


        rb.interpolation =
            RigidbodyInterpolation.Interpolate;


        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;
    }


    private void BuscarJugador()
    {
        if (player != null)
        {
            if (playerHealth == null)
            {
                playerHealth =
                    player.GetComponent<PlayerHealth>();
            }


            return;
        }


        GameObject playerObject =
            GameObject.Find("Player");


        if (playerObject == null)
            return;


        player =
            playerObject.transform;


        playerHealth =
            playerObject.GetComponent<PlayerHealth>();
    }


    private void MoverHaciaJugador()
    {
        if (player == null)
        {
            DetenerMovimiento();
            return;
        }


        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );


        if (
            distanceToPlayer <=
            stoppingDistance
        )
        {
            DetenerMovimiento();
            return;
        }


        if (navigation == null)
        {
            DetenerMovimiento();
            return;
        }


        Vector3 direction =
            navigation.GetDirection();


        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            DetenerMovimiento();
            return;
        }


        float speedMultiplier;


        if (
            distanceToPlayer <=
            aggressiveDistance
        )
        {
            speedMultiplier =
                aggressiveSpeedMultiplier;
        }
        else
        {
            speedMultiplier =
                cautiousSpeedMultiplier;
        }


        PlayerMovement playerMovement =
            player.GetComponent<PlayerMovement>();


        float baseSpeed =
            6f;


        if (playerMovement != null)
        {
            baseSpeed =
                playerMovement.MoveSpeed;
        }


        float enemySpeed =
            baseSpeed *
            speedMultiplier;


        Vector3 targetVelocity =
            direction *
            enemySpeed;


        targetVelocity.y =
            rb.linearVelocity.y;


        // ================================================================
        // BLOQUEO CONTRA PAREDES
        // ================================================================
        //
        // Cuando el enemigo está abandonando una plataforma,
        // NO proyectamos la velocidad contra la pared.
        //
        // De lo contrario, el lateral de la plataforma puede impedir
        // que el Rigidbody salga completamente del borde.
        //

        if (
            touchingWall &&
            !navigation.IsEdgeDropActive
        )
        {
            Vector3 horizontalVelocity =
                new Vector3(
                    targetVelocity.x,
                    0f,
                    targetVelocity.z
                );


            horizontalVelocity =
                Vector3.ProjectOnPlane(
                    horizontalVelocity,
                    wallNormal
                );


            targetVelocity.x =
                horizontalVelocity.x;


            targetVelocity.z =
                horizontalVelocity.z;
        }


        Vector3 newVelocity =
            Vector3.MoveTowards(
                rb.linearVelocity,
                targetVelocity,
                50f *
                Time.fixedDeltaTime
            );


        newVelocity.y =
            rb.linearVelocity.y;


        rb.linearVelocity =
            newVelocity;
    }


    private void DetenerMovimiento()
    {
        Vector3 velocity =
            rb.linearVelocity;


        velocity.x =
            0f;


        velocity.z =
            0f;


        rb.linearVelocity =
            velocity;
    }


    private void OnCollisionEnter(
        Collision collision
    )
    {
        RegistrarPared(collision);

        ProcesarContacto(collision);
    }


    private void OnCollisionStay(
        Collision collision
    )
    {
        RegistrarPared(collision);

        ProcesarContacto(collision);
    }


    private void OnCollisionExit(
        Collision collision
    )
    {
        if (
            collision.collider ==
            currentWallCollider
        )
        {
            touchingWall =
                false;


            wallNormal =
                Vector3.zero;


            currentWallCollider =
                null;
        }
    }


    private void RegistrarPared(
        Collision collision
    )
    {
        if (player != null)
        {
            if (
                collision.transform == player ||
                collision.transform.IsChildOf(player)
            )
            {
                return;
            }
        }


        foreach (
            ContactPoint contact
            in collision.contacts
        )
        {
            Vector3 normal =
                contact.normal;


            // Ignoramos suelo y superficies casi horizontales.

            if (
                Mathf.Abs(normal.y) <
                0.5f
            )
            {
                wallNormal =
                    normal.normalized;


                touchingWall =
                    true;


                currentWallCollider =
                    collision.collider;


                return;
            }
        }
    }


    private void ProcesarContacto(
        Collision collision
    )
    {
        if (player == null)
            return;


        if (
            collision.transform != player &&
            !collision.transform.IsChildOf(player)
        )
        {
            return;
        }


        if (contactCooldownTimer > 0f)
            return;


        AplicarDañoContacto();

        AplicarKnockbackAlJugador();

        AplicarKnockbackAlEnemigo();


        contactCooldownTimer =
            contactDamageCooldown;
    }


    private void AplicarDañoContacto()
    {
        if (playerHealth == null)
        {
            playerHealth =
                player.GetComponent<PlayerHealth>();
        }


        if (playerHealth == null)
            return;


        playerHealth.RecibirDaño(
            contactDamage
        );
    }


    private void AplicarKnockbackAlJugador()
    {
        Rigidbody playerRb =
            player.GetComponent<Rigidbody>();


        if (playerRb == null)
            return;


        Vector3 direction =
            player.position -
            transform.position;


        direction.y =
            0f;


        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            direction =
                transform.forward;
        }


        direction.Normalize();


        Vector3 force =
            direction *
            playerKnockback;


        force.y =
            knockbackUpward;


        playerRb.AddForce(
            force,
            ForceMode.Impulse
        );
    }


    private void AplicarKnockbackAlEnemigo()
    {
        Vector3 direction =
            transform.position -
            player.position;


        direction.y =
            0f;


        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            direction =
                transform.forward;
        }


        direction.Normalize();


        knockbackVelocity =
            direction *
            enemyKnockback;


        knockbackVelocity.y =
            knockbackUpward;


        knockbackTimer =
            enemyKnockbackDuration;


        isKnockedBack =
            true;
    }


    private void ActualizarKnockback()
    {
        if (!isKnockedBack)
            return;


        knockbackTimer -=
            Time.deltaTime;


        if (knockbackTimer <= 0f)
        {
            knockbackTimer =
                0f;


            knockbackVelocity =
                Vector3.zero;


            isKnockedBack =
                false;
        }
    }


    private void AplicarKnockback()
    {
        Vector3 newVelocity =
            knockbackVelocity;


        newVelocity.y =
            rb.linearVelocity.y;


        rb.linearVelocity =
            newVelocity;
    }


    public void ResetChaseState()
    {
        contactCooldownTimer =
            0f;


        knockbackVelocity =
            Vector3.zero;


        knockbackTimer =
            0f;


        isKnockedBack =
            false;


        touchingWall =
            false;


        wallNormal =
            Vector3.zero;


        currentWallCollider =
            null;


        if (navigation != null)
        {
            navigation.ResetNavigation();
        }
    }
}