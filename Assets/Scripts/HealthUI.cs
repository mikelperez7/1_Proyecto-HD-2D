using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD de vida del jugador.
///
/// Verde = vida actual.
/// Gris = vida perdida.
/// Rojo = daño pendiente de mostrar.
///
/// Todo el daño pendiente se representa mediante UNA ÚNICA
/// barra roja que se consume progresivamente de derecha a izquierda.
/// </summary>
public class HealthUI : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] private PlayerHealth playerHealth;


    [Header("Tamaño")]
    [SerializeField] private float barWidth = 180f;
    [SerializeField] private float barHeight = 12f;


    [Header("Posición")]
    [SerializeField] private float offsetX = 20f;
    [SerializeField] private float offsetY = 20f;


    [Header("Colores")]
    [SerializeField] private Color healthColor =
        new Color(0.2f, 1f, 0.35f, 1f);

    [SerializeField] private Color emptyColor =
        new Color(1f, 1f, 1f, 0.25f);

    [SerializeField] private Color damageColor =
        new Color(1f, 0.15f, 0.15f, 1f);


    [Header("Daño visual")]
    [Tooltip("Velocidad a la que se consume la barra roja.")]
    [SerializeField] private float damageShrinkSpeed = 7f;


    // =====================================================================
    // UI
    // =====================================================================

    private RectTransform barRect;

    private RectTransform healthRect;

    private RectTransform damageRect;

    private Image backgroundImage;

    private Image healthImage;

    private Image damageImage;


    // =====================================================================
    // DAÑO PENDIENTE
    // =====================================================================

    /// <summary>
    /// Anchura total del daño que todavía está representado
    /// mediante la barra roja.
    /// </summary>
    private float pendingDamageWidth;


    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        BuscarPlayerHealth();
    }


    private void Update()
    {
        if (playerHealth == null)
        {
            BuscarPlayerHealth();

            return;
        }


        if (barRect == null)
        {
            CrearHUD();

            return;
        }


        ActualizarVida();

        ActualizarDanio();
    }


    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamageReceived -=
                RegistrarDanio;
        }
    }


    // =====================================================================
    // BUSCAR PLAYER HEALTH
    // =====================================================================

    private void BuscarPlayerHealth()
    {
        if (playerHealth != null)
            return;


        playerHealth =
            FindAnyObjectByType<PlayerHealth>();


        if (playerHealth == null)
            return;


        playerHealth.OnDamageReceived -=
            RegistrarDanio;


        playerHealth.OnDamageReceived +=
            RegistrarDanio;


        CrearHUD();
    }


    // =====================================================================
    // CREAR HUD
    // =====================================================================

    private void CrearHUD()
    {
        if (
            playerHealth == null ||
            barRect != null
        )
        {
            return;
        }


        Canvas canvas =
            CrearCanvas();


        // ================================================================
        // BARRA PRINCIPAL
        // ================================================================

        GameObject barObject =
            CrearObjeto(
                "HealthBar",
                canvas.transform
            );


        barRect =
            barObject.AddComponent<RectTransform>();


        ConfigurarBarraPrincipal(
            barRect
        );


        // ================================================================
        // FONDO
        // ================================================================

        GameObject backgroundObject =
            CrearObjeto(
                "HealthBar_Background",
                barObject.transform
            );


        RectTransform backgroundRect =
            backgroundObject.AddComponent<RectTransform>();


        ConfigurarElemento(
            backgroundRect
        );


        backgroundImage =
            backgroundObject.AddComponent<Image>();


        backgroundImage.color =
            emptyColor;


        // ================================================================
        // VIDA VERDE
        // ================================================================

        GameObject healthObject =
            CrearObjeto(
                "HealthBar_Health",
                barObject.transform
            );


        healthRect =
            healthObject.AddComponent<RectTransform>();


        ConfigurarElemento(
            healthRect
        );


        healthImage =
            healthObject.AddComponent<Image>();


        healthImage.color =
            healthColor;


        // ================================================================
        // DAÑO ROJO
        // ================================================================

        GameObject damageObject =
            CrearObjeto(
                "HealthBar_Damage",
                barObject.transform
            );


        damageRect =
            damageObject.AddComponent<RectTransform>();


        ConfigurarElemento(
            damageRect
        );


        damageImage =
            damageObject.AddComponent<Image>();


        damageImage.color =
            damageColor;


        damageObject.transform.SetAsLastSibling();


        ActualizarVida();

        ActualizarDanio();
    }


    // =====================================================================
    // CREAR OBJETO
    // =====================================================================

    private GameObject CrearObjeto(
        string nombre,
        Transform parent
    )
    {
        GameObject obj =
            new GameObject(nombre);


        obj.transform.SetParent(
            parent,
            false
        );


        return obj;
    }


    // =====================================================================
    // CONFIGURAR BARRA PRINCIPAL
    // =====================================================================

    private void ConfigurarBarraPrincipal(
        RectTransform rect
    )
    {
        rect.anchorMin =
            new Vector2(
                0f,
                1f
            );


        rect.anchorMax =
            new Vector2(
                0f,
                1f
            );


        rect.pivot =
            new Vector2(
                0f,
                1f
            );


        rect.anchoredPosition =
            new Vector2(
                offsetX,
                -offsetY
            );


        rect.sizeDelta =
            new Vector2(
                barWidth,
                barHeight
            );
    }


    // =====================================================================
    // CONFIGURAR ELEMENTO
    // =====================================================================

    private void ConfigurarElemento(
        RectTransform rect
    )
    {
        rect.anchorMin =
            Vector2.zero;


        rect.anchorMax =
            Vector2.zero;


        rect.pivot =
            Vector2.zero;


        rect.anchoredPosition =
            Vector2.zero;


        rect.sizeDelta =
            new Vector2(
                barWidth,
                barHeight
            );
    }


    // =====================================================================
    // CREAR CANVAS
    // =====================================================================

    private Canvas CrearCanvas()
    {
        GameObject canvasObject =
            CrearObjeto(
                "HealthUI_Canvas",
                transform
            );


        Canvas canvas =
            canvasObject.AddComponent<Canvas>();


        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;


        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();


        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;


        canvasObject.AddComponent<GraphicRaycaster>();


        return canvas;
    }


    // =====================================================================
    // ACTUALIZAR VIDA VERDE
    // =====================================================================

    private void ActualizarVida()
    {
        float maxHealth =
            playerHealth.MaxHealth;


        if (maxHealth <= 0f)
            return;


        float healthPercent =
            Mathf.Clamp01(
                playerHealth.Health /
                maxHealth
            );


        float healthWidth =
            barWidth *
            healthPercent;


        healthRect.sizeDelta =
            new Vector2(
                healthWidth,
                barHeight
            );


        healthImage.color =
            healthColor;


        backgroundImage.color =
            emptyColor;
    }


    // =====================================================================
    // REGISTRAR DAÑO
    // =====================================================================

    private void RegistrarDanio(
        float damageAmount
    )
    {
        if (
            damageAmount <= 0f ||
            playerHealth.MaxHealth <= 0f
        )
        {
            return;
        }


        // ================================================================
        // CONVERTIR EL DAÑO A PIXELES DE LA BARRA
        // ================================================================

        float damageWidth =
            (
                damageAmount /
                playerHealth.MaxHealth
            ) *
            barWidth;


        if (damageWidth <= 0f)
            return;


        // ================================================================
        // SUMAR AL DAÑO PENDIENTE
        // ================================================================

        pendingDamageWidth +=
            damageWidth;


        // ================================================================
        // LIMITAR AL ANCHO DISPONIBLE
        // ================================================================

        float healthWidth =
            playerHealth.HealthPercent *
            barWidth;


        float maximumDamageWidth =
            Mathf.Max(
                0f,
                barWidth -
                healthWidth
            );


        pendingDamageWidth =
            Mathf.Min(
                pendingDamageWidth,
                maximumDamageWidth
            );


        ActualizarDanio();
    }


    // =====================================================================
    // ACTUALIZAR BARRA ROJA
    // =====================================================================

    private void ActualizarDanio()
    {
        if (
            damageRect == null ||
            damageImage == null ||
            playerHealth == null
        )
        {
            return;
        }


        // ================================================================
        // VIDA ACTUAL
        // ================================================================

        float healthWidth =
            playerHealth.HealthPercent *
            barWidth;


        // ================================================================
        // REDUCIR DAÑO PENDIENTE
        // ================================================================

        if (pendingDamageWidth > 0f)
        {
            float reduction =
                damageShrinkSpeed *
                Time.deltaTime;


            pendingDamageWidth =
                Mathf.Max(
                    0f,
                    pendingDamageWidth -
                    reduction
                );
        }


        // ================================================================
        // LIMITAR DAÑO AL ESPACIO DISPONIBLE
        // ================================================================

        float maximumDamageWidth =
            Mathf.Max(
                0f,
                barWidth -
                healthWidth
            );


        pendingDamageWidth =
            Mathf.Min(
                pendingDamageWidth,
                maximumDamageWidth
            );


        // ================================================================
        // POSICIÓN DE LA BARRA ROJA
        //
        // Empieza exactamente donde termina la vida verde.
        // ================================================================

        damageRect.anchoredPosition =
            new Vector2(
                healthWidth,
                0f
            );


        // ================================================================
        // ANCHO DE LA BARRA ROJA
        // ================================================================

        damageRect.sizeDelta =
            new Vector2(
                pendingDamageWidth,
                barHeight
            );


        damageImage.color =
            damageColor;


        // ================================================================
        // OCULTAR CUANDO NO QUEDA DAÑO
        // ================================================================

        damageImage.enabled =
            pendingDamageWidth >
            0.01f;
    }
}