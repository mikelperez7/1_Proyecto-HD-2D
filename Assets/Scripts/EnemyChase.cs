using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyChase : MonoBehaviour
{
    [Header("Persecución")]
    [Tooltip("El enemigo persigue hasta que los colliders físicos impiden seguir avanzando.")]
    [SerializeField] private float stoppingDistance = 0f;

    [Tooltip("Velocidad del enemigo cuando el jugador lo está mirando y está lejos.")]
    [SerializeField] private float cautiousSpeedMultiplier = 0.5f;

    [Tooltip("Distancia muy cercana al jugador donde el enemigo se vuelve agresivo.")]
    [SerializeField] private float aggressiveDistance = 1.8f;

    [Tooltip("Velocidad del enemigo cuando ataca de cerca o el jugador le da la espalda.")]
    [SerializeField] private float aggressiveSpeedMultiplier = 0.9f;

    [Header("Visión del jugador")]
    [Tooltip("Ángulo total del campo de visión frontal del jugador.")]
    [SerializeField] private float playerVisionAngle = 90f;

    [Header("Daño por contacto")]
    [SerializeField] private float contactDamage = 30f;
    [SerializeField] private float contactDamageCooldown = 1f;

    [Header("Retroceso")]
    [SerializeField] private float playerKnockback = 1f;
    [SerializeField] private float enemyKnockback = 3.5f;
    [SerializeField] private float knockbackUpward = 1.5f;
    [SerializeField] private float enemyKnockbackDuration = 0.2f;

    private Rigidbody rb;
    private Transform player;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private EnemyHealth enemyHealth;

    private float damageCooldownTimer;
    private float enemyKnockbackTimer;

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody>();

        enemyHealth =
            GetComponent<EnemyHealth>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;
    }

    private void Start()
    {
        GameObject playerObject =
            GameObject.Find("Player");

        if (playerObject == null)
        {
            Debug.LogWarning(
                "[EnemyChase] No se encontró el objeto Player."
            );

            return;
        }

        player =
            playerObject.transform;

        playerHealth =
            playerObject.GetComponent<PlayerHealth>();

        playerMovement =
            playerObject.GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogWarning(
                "[EnemyChase] El Player no tiene PlayerMovement."
            );
        }
    }

    public void ResetChaseState()
    {
        damageCooldownTimer =
            contactDamageCooldown;

        enemyKnockbackTimer =
            0f;

        if (rb != null)
        {
            if (rb.isKinematic)
                return;

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        if (player == null)
            return;

        if (enemyHealth != null &&
            !enemyHealth.IsAlive)
            return;

        if (rb.isKinematic)
            return;

        damageCooldownTimer -=
            Time.fixedDeltaTime;

        enemyKnockbackTimer -=
            Time.fixedDeltaTime;

        if (enemyKnockbackTimer > 0f)
            return;

        PerseguirJugador();
    }

    private void PerseguirJugador()
    {
        if (enemyHealth != null &&
            !enemyHealth.IsAlive)
            return;

        if (rb.isKinematic)
            return;

        Vector3 directionToPlayer =
            player.position -
            transform.position;

        directionToPlayer.y =
            0f;

        float distance =
            directionToPlayer.magnitude;

        if (distance <= stoppingDistance)
        {
            if (rb.isKinematic)
                return;

            rb.linearVelocity =
                new Vector3(
                    0f,
                    rb.linearVelocity.y,
                    0f
                );

            return;
        }

        directionToPlayer.Normalize();

        float playerSpeed =
            6f;

        if (playerMovement != null)
        {
            playerSpeed =
                playerMovement.MoveSpeed;
        }

        bool playerIsLookingAtEnemy =
            EstaJugadorMirandoAlEnemigo();

        float speedMultiplier;

        if (!playerIsLookingAtEnemy)
        {
            speedMultiplier =
                aggressiveSpeedMultiplier;
        }
        else if (distance <= aggressiveDistance)
        {
            speedMultiplier =
                aggressiveSpeedMultiplier;
        }
        else
        {
            speedMultiplier =
                cautiousSpeedMultiplier;
        }

        float enemySpeed =
            playerSpeed *
            speedMultiplier;

        Vector3 velocity =
            directionToPlayer *
            enemySpeed;

        if (rb.isKinematic)
            return;

        velocity.y =
            rb.linearVelocity.y;

        if (rb.isKinematic)
            return;

        rb.linearVelocity =
            velocity;
    }

    private bool EstaJugadorMirandoAlEnemigo()
    {
        if (playerMovement == null)
            return false;

        Vector3 playerFacingDirection =
            playerMovement.Direction;

        playerFacingDirection.y =
            0f;

        if (playerFacingDirection.sqrMagnitude < 0.01f)
        {
            return false;
        }

        playerFacingDirection.Normalize();

        Vector3 directionToEnemy =
            transform.position -
            player.position;

        directionToEnemy.y =
            0f;

        if (directionToEnemy.sqrMagnitude < 0.01f)
        {
            return true;
        }

        directionToEnemy.Normalize();

        float dot =
            Vector3.Dot(
                playerFacingDirection,
                directionToEnemy
            );

        float visionLimit =
            Mathf.Cos(
                playerVisionAngle *
                0.5f *
                Mathf.Deg2Rad
            );

        return dot >= visionLimit;
    }

    private void OnCollisionEnter(
        Collision collision
    )
    {
        if (player == null)
            return;

        if (collision.transform != player &&
            collision.transform.GetComponentInParent<PlayerHealth>() == null)
        {
            return;
        }

        AplicarGolpe();
    }

    private void OnCollisionStay(
        Collision collision
    )
    {
        if (player == null)
            return;

        if (collision.transform != player &&
            collision.transform.GetComponentInParent<PlayerHealth>() == null)
        {
            return;
        }

        AplicarGolpe();
    }

    private void AplicarGolpe()
    {
        if (enemyHealth != null &&
            !enemyHealth.IsAlive)
            return;

        if (rb == null ||
            rb.isKinematic)
            return;

        if (playerHealth == null)
            return;

        if (!playerHealth.IsAlive)
            return;

        if (damageCooldownTimer > 0f)
            return;

        damageCooldownTimer =
            contactDamageCooldown;

        playerHealth.RecibirDaño(
            contactDamage,
            DamageType.Physical
        );

        Vector3 knockbackDirection =
            player.position -
            transform.position;

        knockbackDirection.y =
            0f;

        if (knockbackDirection.sqrMagnitude < 0.01f)
        {
            knockbackDirection =
                -transform.forward;
        }

        knockbackDirection.Normalize();

        Rigidbody playerRb =
            player.GetComponent<Rigidbody>();

        if (playerRb != null &&
    !playerRb.isKinematic)
{
    Vector3 playerVelocity =
        knockbackDirection *
        playerKnockback;

    playerVelocity.y =
        knockbackUpward;

    playerRb.linearVelocity =
        playerVelocity;
}

        // El daño al jugador anterior podría haber
        // provocado algún cambio de estado. Comprobamos
        // de nuevo antes de tocar la velocidad del enemigo.
        if (enemyHealth != null &&
            !enemyHealth.IsAlive)
            return;

        if (rb == null ||
            rb.isKinematic)
            return;

        Vector3 enemyVelocity =
            -knockbackDirection *
            enemyKnockback;

        enemyVelocity.y =
            knockbackUpward;

        if (rb.isKinematic)
            return;

        rb.linearVelocity =
            enemyVelocity;

        enemyKnockbackTimer =
            enemyKnockbackDuration;

        Debug.Log(
            "[EnemyChase] Golpe de contacto + retroceso."
        );
    }
}