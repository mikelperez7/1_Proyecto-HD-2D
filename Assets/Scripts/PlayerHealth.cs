using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tipos de daño disponibles.
/// </summary>
public enum DamageType
{
    Physical,
    Poison,
    Fire
}

/// <summary>
/// Sistema de vida del jugador.
///
/// Gestiona:
/// - Vida.
/// - Daño.
/// - Invulnerabilidad.
/// - Feedback visual.
/// - Muerte.
/// - Pantalla de muerte.
/// - Respawn.
/// - Checkpoints.
/// - Protección temporal después del respawn.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField] private float respawnInvulnerabilityTime = 1f;

    [Header("Feedback de daño")]
    [SerializeField] private float damageFlashDuration = 0.15f;

    [Header("Pantalla de muerte")]
    [SerializeField] private Color deathScreenColor =
        new Color(0f, 0f, 0f, 0.75f);

    [SerializeField] private Color deathTextColor =
        Color.white;

    // =====================================================================
    // ESTADO
    // =====================================================================

    private float currentHealth;

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    private Rigidbody rb;
    private PlayerMovement movement;
    private PlayerDash dash;

    private Renderer playerRenderer;
    private Material playerMaterial;
    private Color originalPlayerColor;

    private Coroutine damageFlashCoroutine;
    private Coroutine respawnCoroutine;
    private Coroutine respawnProtectionCoroutine;

    private GameObject deathScreenObject;

    private bool isDead;

    // =====================================================================
    // EVENTOS
    // =====================================================================

    /// <summary>
    /// Evento utilizado por HealthUI.
    /// Envía únicamente la cantidad de daño recibido.
    /// </summary>
    public event Action<float> OnDamageReceived;

    /// <summary>
    /// Evento utilizado por DamageFeedbackUI.
    /// Envía cantidad y tipo de daño.
    /// </summary>
    public event Action<float, DamageType> OnDamageFeedback;

    // =====================================================================
    // PROPIEDADES
    // =====================================================================

    public float Health =>
        currentHealth;

    public float MaxHealth =>
        maxHealth;

    public float HealthPercent
    {
        get
        {
            if (maxHealth <= 0f)
                return 0f;

            return Mathf.Clamp01(
                currentHealth / maxHealth
            );
        }
    }

    public bool IsAlive =>
        currentHealth > 0f &&
        !isDead;

    /// <summary>
    /// Invulnerabilidad utilizada por el Dash
    /// y por la protección tras el respawn.
    /// </summary>
    public bool IsInvulnerable { get; set; }

    // =====================================================================
    // CICLO DE VIDA
    // =====================================================================

    private void Awake()
    {
        currentHealth =
            maxHealth;

        IsInvulnerable =
            false;

        isDead =
            false;

        rb =
            GetComponent<Rigidbody>();

        movement =
            GetComponent<PlayerMovement>();

        dash =
            GetComponent<PlayerDash>();

        playerRenderer =
            GetComponent<Renderer>();

        if (playerRenderer == null)
        {
            playerRenderer =
                GetComponentInChildren<Renderer>();
        }

        if (playerRenderer != null)
        {
            playerMaterial =
                playerRenderer.material;

            originalPlayerColor =
                playerMaterial.color;
        }
    }

    private void Start()
    {
        GuardarCheckpointInicial();

        if (RoomResetManager.Instance == null)
        {
            GameObject resetManagerObject =
                new GameObject(
                    "RoomResetManager"
                );

            resetManagerObject.AddComponent<RoomResetManager>();
        }
    }

    // =====================================================================
    // CHECKPOINT INICIAL
    // =====================================================================

    private void GuardarCheckpointInicial()
    {
        respawnPosition =
            transform.position;

        respawnRotation =
            transform.rotation;

        Debug.Log(
            $"[PlayerHealth] Punto de respawn establecido en: " +
            $"{respawnPosition}"
        );
    }

    // =====================================================================
    // CHECKPOINT
    // =====================================================================

    public void EstablecerCheckpoint(
        Transform checkpoint
    )
    {
        if (checkpoint == null)
            return;

        respawnPosition =
            checkpoint.position;

        respawnRotation =
            checkpoint.rotation;

        Debug.Log(
            $"[PlayerHealth] Nuevo checkpoint establecido en: " +
            $"{respawnPosition}"
        );
    }

    public void EstablecerCheckpoint(
        Vector3 position,
        Quaternion rotation
    )
    {
        respawnPosition =
            position;

        respawnRotation =
            rotation;

        Debug.Log(
            $"[PlayerHealth] Nuevo checkpoint establecido en: " +
            $"{respawnPosition}"
        );
    }

    // =====================================================================
    // RECIBIR DAÑO NORMAL
    // =====================================================================

    public void RecibirDaño(
        float cantidad
    )
    {
        RecibirDaño(
            cantidad,
            DamageType.Physical
        );
    }

    // =====================================================================
    // RECIBIR DAÑO POR TIPO
    // =====================================================================

    public void RecibirDaño(
        float cantidad,
        DamageType damageType
    )
    {
        if (!IsAlive)
            return;

        if (IsInvulnerable)
            return;

        if (cantidad <= 0f)
            return;

        float healthBefore =
            currentHealth;

        currentHealth =
            Mathf.Max(
                0f,
                currentHealth - cantidad
            );

        float realDamage =
            healthBefore -
            currentHealth;

        if (realDamage <= 0f)
            return;

        Debug.Log(
            $"[PlayerHealth] Daño {damageType}: " +
            $"{realDamage}. " +
            $"Vida: {currentHealth}/{maxHealth}"
        );

        OnDamageReceived?.Invoke(
            realDamage
        );

        OnDamageFeedback?.Invoke(
            realDamage,
            damageType
        );

        IniciarFeedbackDanio(
            damageType
        );

        if (currentHealth <= 0f)
        {
            Morir();
        }
    }

    // =====================================================================
    // FEEDBACK DE COLOR DEL PERSONAJE
    // =====================================================================

    private void IniciarFeedbackDanio(
        DamageType damageType
    )
    {
        if (playerMaterial == null)
            return;

        if (damageFlashCoroutine != null)
        {
            StopCoroutine(
                damageFlashCoroutine
            );
        }

        damageFlashCoroutine =
            StartCoroutine(
                FeedbackDanio(
                    damageType
                )
            );
    }

    private IEnumerator FeedbackDanio(
        DamageType damageType
    )
    {
        playerMaterial.color =
            ObtenerColorDanio(
                damageType
            );

        yield return new WaitForSecondsRealtime(
            damageFlashDuration
        );

        if (playerMaterial != null)
        {
            playerMaterial.color =
                originalPlayerColor;
        }

        damageFlashCoroutine =
            null;
    }

    private Color ObtenerColorDanio(
        DamageType damageType
    )
    {
        switch (damageType)
        {
            case DamageType.Poison:

                return new Color(
                    0.65f,
                    0.05f,
                    0.9f,
                    1f
                );

            case DamageType.Fire:

                return new Color(
                    1f,
                    0.35f,
                    0.02f,
                    1f
                );

            case DamageType.Physical:
            default:

                return new Color(
                    1f,
                    0.1f,
                    0.1f,
                    1f
                );
        }
    }

    // =====================================================================
    // MUERTE
    // =====================================================================

    private void Morir()
    {
        if (isDead)
            return;

        isDead =
            true;

        IsInvulnerable =
            false;

        Debug.Log(
            $"[PlayerHealth] ¡Jugador muerto! " +
            $"(GameObject: {gameObject.name})"
        );

        if (movement != null)
        {
            movement.enabled =
                false;
        }

        if (dash != null)
        {
            dash.enabled =
                false;
        }

        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.isKinematic =
                true;
        }

        if (playerMaterial != null)
        {
            playerMaterial.color =
                originalPlayerColor;
        }

        CrearPantallaMuerte();

        if (respawnCoroutine != null)
        {
            StopCoroutine(
                respawnCoroutine
            );
        }

        respawnCoroutine =
            StartCoroutine(
                Respawn()
            );
    }

    // =====================================================================
    // PANTALLA DE MUERTE
    // =====================================================================

    private void CrearPantallaMuerte()
    {
        if (deathScreenObject != null)
            return;

        GameObject deathScreen =
            new GameObject(
                "DeathScreen"
            );

        deathScreenObject =
            deathScreen;

        Canvas canvas =
            deathScreenObject.AddComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            1000;

        CanvasScaler scaler =
            deathScreenObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        deathScreenObject.AddComponent<GraphicRaycaster>();

        GameObject panel =
            new GameObject(
                "DeathScreen_Background"
            );

        panel.transform.SetParent(
            deathScreenObject.transform,
            false
        );

        RectTransform panelRect =
            panel.AddComponent<RectTransform>();

        panelRect.anchorMin =
            Vector2.zero;

        panelRect.anchorMax =
            Vector2.one;

        panelRect.offsetMin =
            Vector2.zero;

        panelRect.offsetMax =
            Vector2.zero;

        Image panelImage =
            panel.AddComponent<Image>();

        panelImage.color =
            deathScreenColor;

        GameObject textObject =
            new GameObject(
                "DeathScreen_Text"
            );

        textObject.transform.SetParent(
            deathScreenObject.transform,
            false
        );

        RectTransform textRect =
            textObject.AddComponent<RectTransform>();

        textRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        textRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        textRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        textRect.anchoredPosition =
            Vector2.zero;

        textRect.sizeDelta =
            new Vector2(
                500f,
                150f
            );

        Text deathText =
            textObject.AddComponent<Text>();

        deathText.text =
            "HAS MUERTO\n\nReapareciendo...";

        deathText.alignment =
            TextAnchor.MiddleCenter;

        deathText.color =
            deathTextColor;

        deathText.fontSize =
            36;

        deathText.fontStyle =
            FontStyle.Bold;

        deathText.horizontalOverflow =
            HorizontalWrapMode.Overflow;

        deathText.verticalOverflow =
            VerticalWrapMode.Overflow;

        deathText.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );
    }

    // =====================================================================
    // RESPAWN
    // =====================================================================

    private IEnumerator Respawn()
    {
        yield return new WaitForSecondsRealtime(
            respawnDelay
        );

        // ================================================================
        // REINICIAR SALA
        // ================================================================

        if (RoomResetManager.Instance != null)
        {
            RoomResetManager.Instance.ResetRoom();
        }

        // ================================================================
        // CERRAR PANTALLA
        // ================================================================

        if (deathScreenObject != null)
        {
            Destroy(
                deathScreenObject
            );

            deathScreenObject =
                null;
        }

        // ================================================================
        // VOLVER AL CHECKPOINT
        // ================================================================

        transform.position =
            respawnPosition;

        transform.rotation =
            respawnRotation;

        // ================================================================
        // RIGIDBODY
        // ================================================================

        if (rb != null)
        {
            rb.isKinematic =
                false;

            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }

        // ================================================================
        // RESTAURAR VIDA
        // ================================================================

        currentHealth =
            maxHealth;

        isDead =
            false;

        // ================================================================
        // PROTECCIÓN DE RESPAWN
        // ================================================================

        IsInvulnerable =
            true;

        if (respawnProtectionCoroutine != null)
        {
            StopCoroutine(
                respawnProtectionCoroutine
            );
        }

        respawnProtectionCoroutine =
            StartCoroutine(
                ProteccionRespawn()
            );

        // ================================================================
        // RESTAURAR DASH
        // ================================================================

        if (dash != null)
        {
            dash.RespawnReset();

            dash.enabled =
                true;
        }

        // ================================================================
        // RESTAURAR MOVIMIENTO
        // ================================================================

        if (movement != null)
        {
            movement.enabled =
                true;
        }

        // ================================================================
        // RESTAURAR COLOR
        // ================================================================

        if (playerMaterial != null)
        {
            playerMaterial.color =
                originalPlayerColor;
        }

        respawnCoroutine =
            null;

        Debug.Log(
            $"[PlayerHealth] Respawn completado en: " +
            $"{respawnPosition}"
        );
    }

    // =====================================================================
    // PROTECCIÓN DESPUÉS DEL RESPAWN
    // =====================================================================

    private IEnumerator ProteccionRespawn()
    {
        yield return new WaitForSecondsRealtime(
            respawnInvulnerabilityTime
        );

        IsInvulnerable =
            false;

        respawnProtectionCoroutine =
            null;

        Debug.Log(
            "[PlayerHealth] Protección de respawn terminada."
        );
    }
}
