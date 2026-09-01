using UnityEngine;

/// <summary>
/// Trampa de veneno.
/// Hace daño periódico mientras una entidad compatible permanece dentro del área.
///
/// Actualmente afecta a:
/// - PlayerHealth
/// - EnemyHealth
///
/// El daño se identifica como DamageType.Poison.
/// </summary>
public class PoisonTrap : MonoBehaviour
{
    [Header("Daño")]

    [Tooltip("Cantidad de daño que hace cada golpe.")]
    [SerializeField] private float damageAmount = 5f;

    [Tooltip("Tiempo entre cada golpe de veneno.")]
    [SerializeField] private float damageInterval = 1f;


    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;

    private float damageTimer;


    // =====================================================================
    // ENTRAR EN LA TRAMPA
    // =====================================================================

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth player =
            other.GetComponent<PlayerHealth>();

        if (player == null)
        {
            player =
                other.GetComponentInParent<PlayerHealth>();
        }

        if (player != null)
        {
            playerHealth =
                player;

            damageTimer =
                0f;

            AplicarDanio();
            return;
        }


        EnemyHealth enemy =
            other.GetComponent<EnemyHealth>();

        if (enemy == null)
        {
            enemy =
                other.GetComponentInParent<EnemyHealth>();
        }

        if (enemy != null)
        {
            enemyHealth =
                enemy;

            damageTimer =
                0f;

            AplicarDanio();
        }
    }


    // =====================================================================
    // ACTUALIZAR
    // =====================================================================

    private void Update()
    {
        if (
            playerHealth == null &&
            enemyHealth == null
        )
        {
            return;
        }

        damageTimer -=
            Time.deltaTime;

        if (damageTimer <= 0f)
        {
            AplicarDanio();
        }
    }


    // =====================================================================
    // APLICAR DAÑO
    // =====================================================================

    private void AplicarDanio()
    {
        bool appliedDamage =
            false;

        if (
            playerHealth != null &&
            playerHealth.IsAlive
        )
        {
            playerHealth.RecibirDaño(
                damageAmount,
                DamageType.Poison
            );

            appliedDamage =
                true;
        }


        if (
            enemyHealth != null &&
            enemyHealth.IsAlive
        )
        {
            enemyHealth.RecibirDaño(
                damageAmount,
                DamageType.Poison
            );

            appliedDamage =
                true;
        }


        if (appliedDamage)
        {
            damageTimer =
                damageInterval;
        }
    }


    // =====================================================================
    // SALIR DE LA TRAMPA
    // =====================================================================

    private void OnTriggerExit(Collider other)
    {
        PlayerHealth player =
            other.GetComponent<PlayerHealth>();

        if (player == null)
        {
            player =
                other.GetComponentInParent<PlayerHealth>();
        }

        if (
            player != null &&
            player == playerHealth
        )
        {
            playerHealth =
                null;
        }


        EnemyHealth enemy =
            other.GetComponent<EnemyHealth>();

        if (enemy == null)
        {
            enemy =
                other.GetComponentInParent<EnemyHealth>();
        }

        if (
            enemy != null &&
            enemy == enemyHealth
        )
        {
            enemyHealth =
                null;
        }


        if (
            playerHealth == null &&
            enemyHealth == null
        )
        {
            damageTimer =
                0f;
        }
    }
}