using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Feedback visual de daño.
///
/// Físico:
/// - Shake de cámara.
/// - El personaje se vuelve rojo.
///
/// Veneno:
/// - Shake horizontal de cámara.
/// - Marco morado únicamente en los laterales.
/// - Latigazo horizontal corto.
/// - El personaje se vuelve morado.
///
/// Fuego:
/// - Shake vertical de cámara.
/// - Marco rojo/naranja únicamente en la parte inferior.
/// - Latigazo vertical corto.
/// - El personaje se vuelve naranja.
///
/// La cámara es controlada exclusivamente por CameraFollow.
/// </summary>
public class DamageFeedbackUI : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] private PlayerHealth playerHealth;


    [Header("Cámara")]
    [SerializeField] private CameraFollow cameraFollow;


    [Header("Marco")]
    [Tooltip("Grosor base del marco.")]
    [SerializeField] private float frameWidth = 2f;

    [Tooltip("Opacidad máxima del marco.")]
    [SerializeField] private float frameAlpha = 0.30f;

    [Tooltip("Resolución de la textura.")]
    [SerializeField] private int textureSize = 512;


    [Header("Veneno")]
    [Tooltip("Fuerza de la deformación horizontal del marco.")]
    [SerializeField] private float poisonWaveStrength = 1f;

    [Tooltip("Cantidad de ondas del marco de veneno.")]
    [SerializeField] private float poisonWaveCount = 4f;


    [Header("Fuego")]
    [Tooltip("Fuerza de la deformación vertical del marco.")]
    [SerializeField] private float fireWaveStrength = 1f;

    [Tooltip("Cantidad de ondas del marco de fuego.")]
    [SerializeField] private float fireWaveCount = 4f;


    // =====================================================================
    // UI
    // =====================================================================

    private Canvas canvas;

    private Image frameImage;

    private Texture2D frameTexture;

    private Sprite frameSprite;

    private Coroutine frameCoroutine;


    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        BuscarPlayerHealth();

        BuscarCameraFollow();

        CrearUI();
    }


    private void Update()
    {
        if (playerHealth == null)
        {
            BuscarPlayerHealth();
        }

        if (cameraFollow == null)
        {
            BuscarCameraFollow();
        }
    }


    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamageFeedback -=
                RecibirFeedback;
        }


        if (frameSprite != null)
        {
            Destroy(frameSprite);
        }


        if (frameTexture != null)
        {
            Destroy(frameTexture);
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


        playerHealth.OnDamageFeedback -=
            RecibirFeedback;


        playerHealth.OnDamageFeedback +=
            RecibirFeedback;
    }


    // =====================================================================
    // BUSCAR CAMERA FOLLOW
    // =====================================================================

    private void BuscarCameraFollow()
    {
        if (cameraFollow != null)
            return;


        cameraFollow =
            FindAnyObjectByType<CameraFollow>();
    }


    // =====================================================================
    // CREAR UI
    // =====================================================================

    private void CrearUI()
    {
        if (canvas != null)
            return;


        GameObject canvasObject =
            new GameObject(
                "DamageFeedback_Canvas"
            );


        canvas =
            canvasObject.AddComponent<Canvas>();


        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;


        canvas.sortingOrder =
            900;


        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();


        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;


        // ================================================================
        // MARCO
        // ================================================================

        GameObject frameObject =
            new GameObject(
                "DamageFeedback_Frame"
            );


        frameObject.transform.SetParent(
            canvas.transform,
            false
        );


        RectTransform frameRect =
            frameObject.AddComponent<RectTransform>();


        frameRect.anchorMin =
            Vector2.zero;


        frameRect.anchorMax =
            Vector2.one;


        frameRect.offsetMin =
            Vector2.zero;


        frameRect.offsetMax =
            Vector2.zero;


        frameImage =
            frameObject.AddComponent<Image>();


        frameImage.raycastTarget =
            false;


        // ================================================================
        // TEXTURA INICIAL
        // ================================================================

        frameTexture =
            CrearTexturaMarco(
                MarcoTipo.Veneno
            );


        frameSprite =
            Sprite.Create(
                frameTexture,
                new Rect(
                    0f,
                    0f,
                    textureSize,
                    textureSize
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                100f
            );


        frameImage.sprite =
            frameSprite;


        frameImage.type =
            Image.Type.Simple;


        frameImage.preserveAspect =
            false;


        frameImage.color =
            new Color(
                1f,
                1f,
                1f,
                0f
            );


        frameObject.SetActive(
            false
        );
    }


    // =====================================================================
    // TIPO DE MARCO
    // =====================================================================

    private enum MarcoTipo
    {
        Veneno,
        Fuego
    }


    // =====================================================================
    // CREAR TEXTURA DEL MARCO
    // =====================================================================

    private Texture2D CrearTexturaMarco(
        MarcoTipo tipo
    )
    {
        Texture2D texture =
            new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                false
            );


        texture.wrapMode =
            TextureWrapMode.Clamp;


        texture.filterMode =
            FilterMode.Bilinear;


        Color[] pixels =
            new Color[
                textureSize *
                textureSize
            ];


        for (
            int y = 0;
            y < textureSize;
            y++
        )
        {
            for (
                int x = 0;
                x < textureSize;
                x++
            )
            {
                float u =
                    (float)x /
                    (textureSize - 1);


                float v =
                    (float)y /
                    (textureSize - 1);


                float alpha =
                    0f;


                // ========================================================
                // VENENO
                // ========================================================

                if (
                    tipo ==
                    MarcoTipo.Veneno
                )
                {
                    // Distancia a los laterales.

                    float left =
                        x;

                    float right =
                        textureSize -
                        1 -
                        x;


                    float sideDistance =
                        Mathf.Min(
                            left,
                            right
                        );


                    // Solo laterales.

                    float wave =
                        Mathf.Sin(
                            v *
                            Mathf.PI *
                            2f *
                            poisonWaveCount
                        );


                    float width =
                        frameWidth +
                        wave *
                        poisonWaveStrength;


                    width =
                        Mathf.Max(
                            1f,
                            width
                        );


                    float normalized =
                        sideDistance /
                        width;


                    alpha =
                        1f -
                        Mathf.Clamp01(
                            normalized
                        );


                    alpha =
                        alpha *
                        alpha *
                        (
                            3f -
                            2f *
                            alpha
                        );
                }


                // ========================================================
                // FUEGO
                // ========================================================

                else if (
                    tipo ==
                    MarcoTipo.Fuego
                )
                {
                    // Distancia desde la parte inferior.

                    float bottomDistance =
                        y;


                    // Solo zona inferior.

                    float wave =
                        Mathf.Sin(
                            u *
                            Mathf.PI *
                            2f *
                            fireWaveCount
                        );


                    float width =
                        frameWidth +
                        wave *
                        fireWaveStrength;


                    width =
                        Mathf.Max(
                            1f,
                            width
                        );


                    float normalized =
                        bottomDistance /
                        width;


                    alpha =
                        1f -
                        Mathf.Clamp01(
                            normalized
                        );


                    alpha =
                        alpha *
                        alpha *
                        (
                            3f -
                            2f *
                            alpha
                        );
                }


                pixels[
                    y *
                    textureSize +
                    x
                ] =
                    new Color(
                        1f,
                        1f,
                        1f,
                        Mathf.Clamp01(
                            alpha
                        )
                    );
            }
        }


        texture.SetPixels(
            pixels
        );


        texture.Apply();


        return texture;
    }


    // =====================================================================
    // RECIBIR FEEDBACK
    // =====================================================================

    private void RecibirFeedback(
        float damageAmount,
        DamageType damageType
    )
    {
        // ================================================================
        // SHAKE
        // ================================================================

        if (cameraFollow != null)
        {
            cameraFollow.TriggerDamageShake(damageType);
        }


        // ================================================================
        // VENENO
        // ================================================================

        if (
            damageType ==
            DamageType.Poison
        )
        {
            MostrarMarco(
                new Color(
                    0.55f,
                    0.05f,
                    0.9f,
                    1f
                ),
                MarcoTipo.Veneno
            );


            return;
        }


        // ================================================================
        // FUEGO
        // ================================================================

        if (
            damageType ==
            DamageType.Fire
        )
        {
            MostrarMarco(
                new Color(
                    1f,
                    0.22f,
                    0.01f,
                    1f
                ),
                MarcoTipo.Fuego
            );
        }
    }


    // =====================================================================
    // MOSTRAR MARCO
    // =====================================================================

    private void MostrarMarco(
        Color color,
        MarcoTipo tipo
    )
    {
        if (frameImage == null)
            return;


        if (frameCoroutine != null)
        {
            StopCoroutine(
                frameCoroutine
            );
        }


        // ================================================================
        // RECREAR TEXTURA SEGÚN EL TIPO
        // ================================================================

        Texture2D nuevaTextura =
            CrearTexturaMarco(
                tipo
            );


        Sprite nuevoSprite =
            Sprite.Create(
                nuevaTextura,
                new Rect(
                    0f,
                    0f,
                    textureSize,
                    textureSize
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                100f
            );


        if (frameSprite != null)
        {
            Destroy(
                frameSprite
            );
        }


        if (frameTexture != null)
        {
            Destroy(
                frameTexture
            );
        }


        frameTexture =
            nuevaTextura;


        frameSprite =
            nuevoSprite;


        frameImage.sprite =
            frameSprite;


        frameCoroutine =
            StartCoroutine(
                AnimarMarco(
                    color
                )
            );
    }


    // =====================================================================
    // ANIMAR MARCO
    // =====================================================================

    private IEnumerator AnimarMarco(
        Color color
    )
    {
        frameImage.gameObject.SetActive(
            true
        );


        float fadeIn =
            0.06f;


        float hold =
            0.10f;


        float fadeOut =
            0.30f;


        // ================================================================
        // APARECER
        // ================================================================

        float elapsed =
            0f;


        while (
            elapsed <
            fadeIn
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeIn
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            frameImage.color =
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    frameAlpha *
                    t
                );


            yield return null;
        }


        // ================================================================
        // MANTENER
        // ================================================================

        elapsed =
            0f;


        while (
            elapsed <
            hold
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;


            yield return null;
        }


        // ================================================================
        // DESAPARECER
        // ================================================================

        elapsed =
            0f;


        while (
            elapsed <
            fadeOut
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeOut
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            frameImage.color =
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    frameAlpha *
                    (1f - t)
                );


            yield return null;
        }


        frameImage.color =
            new Color(
                color.r,
                color.g,
                color.b,
                0f
            );


        frameImage.gameObject.SetActive(
            false
        );


        frameCoroutine =
            null;
    }
}
