using UnityEngine;

/// <summary>
/// Trampa de veneno.
/// Hace daño periódico al jugador mientras permanece dentro del área.
/// El daño se identifica como DamageType.Poison para activar
/// el feedback visual morado.
/// </summary>
public class PoisonTrap : MonoBehaviour
{
    [Header("Daño")]

    [Tooltip("Cantidad de daño que hace cada golpe.")]
    [SerializeField] private float damageAmount = 5f;

    [Tooltip("Tiempo entre cada golpe de veneno.")]
    [SerializeField] private float damageInterval = 1f;


    private PlayerHealth playerHealth;
    private float damageTimer;


    // =====================================================================
    // ENTRAR EN LA TRAMPA
    // =====================================================================

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth health =
            other.GetComponent<PlayerHealth>();

        if (health == null)
            return;

        playerHealth =
            health;

        damageTimer =
            0f;

        // Daño inmediato al entrar.
        AplicarDanio();
    }


    // =====================================================================
    // ACTUALIZAR
    // =====================================================================

    private void Update()
    {
        if (playerHealth == null)
            return;

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
        if (playerHealth == null)
            return;

        playerHealth.RecibirDaño(
            damageAmount,
            DamageType.Poison
        );

        damageTimer =
            damageInterval;
    }


    // =====================================================================
    // SALIR DE LA TRAMPA
    // =====================================================================

    private void OnTriggerExit(Collider other)
    {
        PlayerHealth health =
            other.GetComponent<PlayerHealth>();

        if (
            health != null &&
            health == playerHealth
        )
        {
            playerHealth =
                null;

            damageTimer =
                0f;
        }
    }
}