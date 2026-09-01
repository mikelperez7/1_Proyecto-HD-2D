using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de Dash para el jugador.
/// Permite impulsos rápidos en la dirección de movimiento actual,
/// con cargas limitadas, recarga automática, invulnerabilidad
/// y un pequeño impulso vertical durante el Dash.
/// </summary>
public class PlayerDash : MonoBehaviour
{
    [Header("Configuración del Dash")]

    [Tooltip("Velocidad del impulso durante el Dash.")]
    [SerializeField] private float dashSpeed = 12f;

    [Tooltip("Duración del Dash en segundos.")]
    [SerializeField] private float dashDuration = 0.25f;

    [Tooltip("Número máximo de cargas de Dash.")]
    [SerializeField] private int maxDashes = 3;

    [Tooltip("Segundos necesarios para recuperar 1 carga de Dash.")]
    [SerializeField] private float dashRechargeTime = 3f;


    [Header("Salto visual del Dash")]

    [Tooltip("Fuerza del pequeño impulso vertical al iniciar el Dash.")]
    [SerializeField] private float dashJumpForce = 3f;


    // ── Estado interno ────────────────────────────────────────────────────

    private Rigidbody rb;
    private PlayerMovement movement;
    private PlayerHealth health;

    private int currentDashes;
    private float rechargeTimer;
    private bool isDashing;
    private float dashTimer;
    private Vector3 dashDirection;


    // ── Propiedades públicas ──────────────────────────────────────────────

    /// <summary>
    /// Indica si el jugador está ejecutando un Dash.
    /// </summary>
    public bool IsDashing => isDashing;

    /// <summary>
    /// Cargas de Dash disponibles.
    /// </summary>
    public int DashesRemaining => currentDashes;

    /// <summary>
    /// Número máximo de cargas.
    /// </summary>
    public int MaxDashes => maxDashes;

    /// <summary>
    /// Progreso de recarga de la siguiente carga.
    /// </summary>
    public float RechargeProgress =>
        currentDashes >= maxDashes
            ? 1f
            : Mathf.Clamp01(
                rechargeTimer / dashRechargeTime
            );


    // ── Ciclo de vida ─────────────────────────────────────────────────────

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody>();

        movement =
            GetComponent<PlayerMovement>();

        health =
            GetComponent<PlayerHealth>();

        currentDashes =
            maxDashes;

        rechargeTimer =
            0f;

        isDashing =
            false;

        dashTimer =
            0f;
    }


    private void Update()
    {
        ProcesarEntradaDash();
    }


    private void FixedUpdate()
    {
        ActualizarRecarga();

        if (isDashing)
        {
            AplicarDash();
        }
    }


    // ── Entrada ───────────────────────────────────────────────────────────

    /// <summary>
    /// Shift en teclado.
    /// B en Xbox.
    /// Círculo en PlayStation.
    /// </summary>
    private void ProcesarEntradaDash()
    {
        if (
            isDashing ||
            currentDashes <= 0
        )
        {
            return;
        }


        bool dashPressed =
            false;


        // ================================================================
        // TECLADO: SHIFT
        // ================================================================

        Keyboard keyboard =
            Keyboard.current;

        if (
            keyboard != null &&
            (
                keyboard.leftShiftKey.wasPressedThisFrame ||
                keyboard.rightShiftKey.wasPressedThisFrame
            )
        )
        {
            dashPressed =
                true;
        }


        // ================================================================
        // MANDO
        //
        // Xbox        -> B
        // PlayStation -> Círculo
        // ================================================================

        Gamepad gamepad =
            Gamepad.current;

        if (
            gamepad != null &&
            gamepad.buttonEast.wasPressedThisFrame
        )
        {
            dashPressed =
                true;
        }


        if (dashPressed)
        {
            IniciarDash();
        }
    }


    // ── Iniciar Dash ──────────────────────────────────────────────────────

    private void IniciarDash()
    {
        currentDashes--;

        isDashing =
            true;

        dashTimer =
            dashDuration;


        // Dirección actual del movimiento.
        dashDirection =
            movement.Direction;


        // Si no hay dirección, utilizar hacia delante.
        if (
            dashDirection.sqrMagnitude <
            0.01f
        )
        {
            dashDirection =
                Vector3.forward;
        }


        dashDirection.y =
            0f;

        dashDirection.Normalize();


        // ================================================================
        // PEQUEÑO SALTO
        // ================================================================

        Vector3 velocity =
            rb.linearVelocity;

        velocity.y =
            dashJumpForce;

        rb.linearVelocity =
            velocity;


        // ================================================================
        // INVULNERABILIDAD
        // ================================================================

        if (health != null)
        {
            health.IsInvulnerable =
                true;
        }
    }


    // ── Aplicar Dash ──────────────────────────────────────────────────────

    private void AplicarDash()
    {
        dashTimer -=
            Time.fixedDeltaTime;


        Vector3 dashVelocity =
            dashDirection *
            dashSpeed;


        // Mantener el movimiento vertical del pequeño salto.
        dashVelocity.y =
            rb.linearVelocity.y;


        rb.linearVelocity =
            dashVelocity;


        if (dashTimer <= 0f)
        {
            TerminarDash();
        }
    }


    // ── Terminar Dash ─────────────────────────────────────────────────────

    private void TerminarDash()
    {
        isDashing =
            false;

        dashTimer =
            0f;


        if (health != null)
        {
            health.IsInvulnerable =
                false;
        }
    }


    // ── Recarga ───────────────────────────────────────────────────────────

    private void ActualizarRecarga()
    {
        if (currentDashes >= maxDashes)
        {
            return;
        }


        rechargeTimer +=
            Time.fixedDeltaTime;


        if (
            rechargeTimer >=
            dashRechargeTime
        )
        {
            rechargeTimer =
                0f;


            currentDashes =
                Mathf.Min(
                    currentDashes + 1,
                    maxDashes
                );
        }
    }


    // ── Respawn ───────────────────────────────────────────────────────────

    /// <summary>
    /// Restablece completamente el Dash cuando el jugador reaparece.
    /// </summary>
    public void RespawnReset()
    {
        currentDashes =
            maxDashes;

        rechargeTimer =
            0f;

        isDashing =
            false;

        dashTimer =
            0f;

        dashDirection =
            Vector3.zero;


        if (health != null)
        {
            health.IsInvulnerable =
                false;
        }
    }
}