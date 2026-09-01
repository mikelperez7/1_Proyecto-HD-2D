using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyChase : MonoBehaviour
{
    [Header("Persecución")]
    [SerializeField] private float stoppingDistance = 1.2f;
    [SerializeField] private float moveSpeedMultiplier = 0.8f;

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

    private float damageCooldownTimer;
    private float enemyKnockbackTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

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
    }

    private void FixedUpdate()
    {
        if (player == null)
            return;

        // Si el enemigo está muerto, no hacemos nada más.
        if (rb.isKinematic)
            return;

        damageCooldownTimer -= Time.fixedDeltaTime;
        enemyKnockbackTimer -= Time.fixedDeltaTime;

        if (enemyKnockbackTimer > 0f)
            return;

        PerseguirJugador();
    }

    private void PerseguirJugador()
    {
        // Seguridad adicional por si el Rigidbody cambia a Kinematic.
        if (rb.isKinematic)
            return;

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        float distance =
            direction.magnitude;

        if (distance <= stoppingDistance)
        {
            rb.linearVelocity =
                new Vector3(
                    0f,
                    rb.linearVelocity.y,
                    0f
                );

            return;
        }

        direction.Normalize();

        float playerSpeed = 6f;

        if (playerMovement != null)
        {
            playerSpeed =
                playerMovement.MoveSpeed;
        }

        float enemySpeed =
            playerSpeed * moveSpeedMultiplier;

        Vector3 velocity =
            direction * enemySpeed;

        velocity.y =
            rb.linearVelocity.y;

        rb.linearVelocity =
            velocity;
    }

    private void OnCollisionEnter(Collision collision)
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

    private void OnCollisionStay(Collision collision)
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
        if (playerHealth == null)
            return;

        if (!playerHealth.IsAlive)
            return;

        if (damageCooldownTimer > 0f)
            return;

        damageCooldownTimer =
            contactDamageCooldown;

        // Daño
        playerHealth.RecibirDaño(
            contactDamage,
            DamageType.Physical
        );

        // Dirección desde el enemigo hacia el jugador
        Vector3 knockbackDirection =
            player.position - transform.position;

        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude < 0.01f)
        {
            knockbackDirection =
                -transform.forward;
        }

        knockbackDirection.Normalize();

        // Pequeño retroceso del jugador
        Rigidbody playerRb =
            player.GetComponent<Rigidbody>();

        if (playerRb != null)
        {
            Vector3 playerVelocity =
                knockbackDirection * playerKnockback;

            playerVelocity.y =
                knockbackUpward;

            playerRb.linearVelocity =
                playerVelocity;
        }

        // Retroceso del enemigo
        // Solo si su Rigidbody sigue siendo dinámico.
        if (!rb.isKinematic)
        {
            Vector3 enemyVelocity =
                -knockbackDirection * enemyKnockback;

            enemyVelocity.y =
                knockbackUpward;

            rb.linearVelocity =
                enemyVelocity;

            // Impide que la persecución cancele inmediatamente el retroceso.
            enemyKnockbackTimer =
                enemyKnockbackDuration;
        }

        Debug.Log(
            "[EnemyChase] Golpe de contacto + retroceso."
        );
    }
}
