using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD para mostrar las cargas de Dash del jugador.
///
/// Muestra 3 barras horizontales en la esquina inferior izquierda.
/// Cada barra tiene un fondo permanente y un relleno cuyo ancho
/// representa la cantidad de carga disponible.
///
/// La barra de la siguiente carga se rellena progresivamente.
/// </summary>
public class DashUI : MonoBehaviour
{
    [Header("Configuración")]

    [Tooltip("Referencia al PlayerDash.")]
    [SerializeField] private PlayerDash playerDash;

    [Tooltip("Ancho de cada barra.")]
    [SerializeField] private float barWidth = 40f;

    [Tooltip("Altura de cada barra.")]
    [SerializeField] private float barHeight = 8f;

    [Tooltip("Espacio entre barras.")]
    [SerializeField] private float barSpacing = 6f;

    [Tooltip("Color del relleno.")]
    [SerializeField] private Color filledColor =
        new Color(0.2f, 0.85f, 1f, 1f);

    [Tooltip("Color del fondo.")]
    [SerializeField] private Color emptyColor =
        new Color(1f, 1f, 1f, 0.25f);

    // ── Estado interno ────────────────────────────────────────────────────

    private RectTransform[] barFills;
    private Image[] barBackgrounds;

    private int lastKnownDashes = -1;

    // ── Referencia al jugador ─────────────────────────────────────────────

    public void SetPlayerDashReference(PlayerDash dash)
    {
        if (dash == null)
            return;

        playerDash = dash;

        if (barFills == null)
        {
            CrearElementoUI();
        }
    }

    // ── Ciclo de vida ─────────────────────────────────────────────────────

    private void Awake()
    {
        BuscarPlayerDash();
    }

    private void Update()
    {
        if (playerDash == null)
        {
            BuscarPlayerDash();
            return;
        }

        if (barFills == null)
        {
            CrearElementoUI();
            return;
        }

        int remaining = playerDash.DashesRemaining;
        float recharge = playerDash.RechargeProgress;

        // Actualizar siempre.
        // Esto evita que la interfaz pueda quedarse desincronizada.
        ActualizarBarras(remaining, recharge);

        lastKnownDashes = remaining;
    }

    private void BuscarPlayerDash()
    {
        if (playerDash != null)
            return;

        playerDash = FindAnyObjectByType<PlayerDash>();

        if (playerDash != null && barFills == null)
        {
            CrearElementoUI();
        }
    }

    // ── Creación del HUD ──────────────────────────────────────────────────

    private void CrearElementoUI()
    {
        if (playerDash == null)
            return;

        Canvas canvas = CrearCanvas();

        int maxBars = playerDash.MaxDashes;

        barFills = new RectTransform[maxBars];
        barBackgrounds = new Image[maxBars];

        // Contenedor
        GameObject containerObj = new GameObject("DashBars");

        RectTransform containerRect =
            containerObj.AddComponent<RectTransform>();

        containerObj.transform.SetParent(canvas.transform, false);

        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(0f, 0f);
        containerRect.pivot = new Vector2(0f, 0f);

        containerRect.anchoredPosition =
            new Vector2(20f, 20f);

        containerRect.sizeDelta =
            new Vector2(
                maxBars * barWidth +
                (maxBars - 1) * barSpacing,
                barHeight
            );

        // Crear barras
        for (int i = 0; i < maxBars; i++)
        {
            CrearBarra(
                containerObj.transform,
                i,
                out barBackgrounds[i],
                out barFills[i]
            );
        }

        ActualizarBarras(
            playerDash.DashesRemaining,
            playerDash.RechargeProgress
        );
    }

    private void CrearBarra(
        Transform parent,
        int index,
        out Image background,
        out RectTransform fill)
    {
        float x =
            index * (barWidth + barSpacing);

        // ── Fondo ─────────────────────────────────────────────────────────

        GameObject backgroundObj =
            new GameObject($"DashBar_{index}_Background");

        RectTransform backgroundRect =
            backgroundObj.AddComponent<RectTransform>();

        backgroundObj.transform.SetParent(parent, false);

        backgroundRect.anchorMin =
            new Vector2(0f, 0f);

        backgroundRect.anchorMax =
            new Vector2(0f, 0f);

        backgroundRect.pivot =
            new Vector2(0f, 0f);

        backgroundRect.anchoredPosition =
            new Vector2(x, 0f);

        backgroundRect.sizeDelta =
            new Vector2(barWidth, barHeight);

        background =
            backgroundObj.AddComponent<Image>();

        background.color =
            emptyColor;

        // ── Relleno ──────────────────────────────────────────────────────

        GameObject fillObj =
            new GameObject($"DashBar_{index}_Fill");

        fill =
            fillObj.AddComponent<RectTransform>();

        fillObj.transform.SetParent(parent, false);

        fill.anchorMin =
            new Vector2(0f, 0f);

        fill.anchorMax =
            new Vector2(0f, 0f);

        fill.pivot =
            new Vector2(0f, 0f);

        fill.anchoredPosition =
            new Vector2(x, 0f);

        fill.sizeDelta =
            new Vector2(0f, barHeight);

        Image fillImage =
            fillObj.AddComponent<Image>();

        fillImage.color =
            filledColor;
    }

    // ── Canvas ────────────────────────────────────────────────────────────

    private Canvas CrearCanvas()
    {
        GameObject canvasObj =
            new GameObject("DashUI_Canvas");

        Canvas canvas =
            canvasObj.AddComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvasObj.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        canvasObj.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    // ── Actualización ─────────────────────────────────────────────────────

    private void ActualizarBarras(
        int remaining,
        float recharge)
    {
        if (barFills == null)
            return;

        remaining =
            Mathf.Clamp(
                remaining,
                0,
                barFills.Length
            );

        recharge =
            Mathf.Clamp01(recharge);

        for (int i = 0; i < barFills.Length; i++)
        {
            float porcentaje;

            if (i < remaining)
            {
                // Carga completamente disponible.
                porcentaje = 1f;
            }
            else if (
                i == remaining &&
                remaining < barFills.Length)
            {
                // Carga actualmente recargándose.
                porcentaje = recharge;
            }
            else
            {
                // Sin carga.
                porcentaje = 0f;
            }

            barFills[i].sizeDelta =
                new Vector2(
                    barWidth * porcentaje,
                    barHeight
                );

            barBackgrounds[i].color =
                emptyColor;
        }
    }
}