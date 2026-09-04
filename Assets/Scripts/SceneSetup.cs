using UnityEngine;

/// <summary>
/// Inicialización de los sistemas jugables de la escena.
///
/// IMPORTANTE:
/// - NO crea el mapa.
/// - NO crea el suelo.
/// - NO crea las paredes.
/// - NO crea las trampas del mapa.
///
/// El mapa es responsabilidad de WorldGrayboxBuilder.
///
/// Este script garantiza que existan:
/// - Player
/// - Cámara
/// - Enemy_Test
/// - HealthUI
/// - DashUI
/// - DamageFeedbackUI
/// </summary>
public class SceneSetup : MonoBehaviour
{
    [Header("Configuración Automática")]
    [Tooltip("Inicializa automáticamente los sistemas al entrar en Play.")]
    [SerializeField] private bool autoSetupOnPlay = true;

    [Header("Player")]
    [SerializeField] private Vector3 playerSpawnPosition =
        new Vector3(0f, 1f, 0f);

    [Header("Enemigo")]
    [SerializeField] private Vector3 enemySpawnPosition =
        new Vector3(8f, 1f, 8f);

    private static bool configuracionEjecutada;

    private void Awake()
    {
        if (!autoSetupOnPlay)
            return;

        EjecutarConfiguracion();
    }

    /// <summary>
    /// Garantiza la inicialización incluso si no existe
    /// un objeto SceneSetup en la escena.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad
    )]
    private static void AutoInicializarEnPlay()
    {
        if (configuracionEjecutada)
            return;

        SceneSetup setup =
            FindAnyObjectByType<SceneSetup>();

        if (setup != null)
        {
            if (setup.autoSetupOnPlay)
            {
                setup.EjecutarConfiguracion();
            }

            return;
        }

        GameObject setupObject =
            new GameObject("SceneSetup_Runtime");

        setup =
            setupObject.AddComponent<SceneSetup>();

        setup.autoSetupOnPlay = true;

        setup.EjecutarConfiguracion();
    }

    [ContextMenu("Configurar Escena")]
    public void EjecutarConfiguracion()
    {
        if (configuracionEjecutada)
            return;

        configuracionEjecutada = true;

        GameObject player =
            CrearJugador();

        ConfigurarCamara(
            player
        );

        CrearEnemigo();

        CrearHUD();

        Debug.Log(
            "[SceneSetup] Sistemas jugables inicializados correctamente."
        );
    }

    // =====================================================================
    // JUGADOR
    // =====================================================================

    private GameObject CrearJugador()
    {
        GameObject player =
            GameObject.Find("Player");

        if (player == null)
        {
            player =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            player.name =
                "Player";

            player.transform.position =
                playerSpawnPosition;

            player.transform.localScale =
                new Vector3(
                    1f,
                    1f,
                    1f
                );

            Renderer renderer =
                player.GetComponent<Renderer>();

            if (renderer != null)
            {
                Shader shader =
                    Shader.Find(
                        "Universal Render Pipeline/Lit"
                    );

                if (shader == null)
                {
                    shader =
                        Shader.Find(
                            "Standard"
                        );
                }

                if (shader != null)
                {
                    Material material =
                        new Material(shader);

                    material.color =
                        new Color(
                            0.2f,
                            0.6f,
                            1f
                        );

                    renderer.sharedMaterial =
                        material;
                }
            }
        }

        // ================================================================
        // RIGIDBODY
        // ================================================================

        Rigidbody rb =
            player.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb =
                player.AddComponent<Rigidbody>();
        }

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        // ================================================================
        // MOVIMIENTO
        // ================================================================

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement == null)
        {
            movement =
                player.AddComponent<PlayerMovement>();
        }

        // ================================================================
        // SPRITE VISUAL
        // ================================================================

        Transform visualChild =
            player.transform.Find(
                "SpriteVisual"
            );

        if (visualChild == null)
        {
            GameObject visualObject =
                new GameObject(
                    "SpriteVisual"
                );

            visualObject.transform.SetParent(
                player.transform
            );

            visualObject.transform.localPosition =
                Vector3.zero;

            visualObject.transform.localRotation =
                Quaternion.identity;

            visualChild =
                visualObject.transform;
        }

        SpriteRenderer spriteRenderer =
            visualChild.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                visualChild.gameObject.AddComponent<SpriteRenderer>();
        }

        // ================================================================
        // ORIENTACIÓN DEL SPRITE
        // ================================================================

        PlayerSpriteDirection spriteDirection =
            player.GetComponent<PlayerSpriteDirection>();

        if (spriteDirection == null)
        {
            spriteDirection =
                player.AddComponent<PlayerSpriteDirection>();
        }

        // ================================================================
        // VIDA
        // ================================================================

        PlayerHealth health =
            player.GetComponent<PlayerHealth>();

        if (health == null)
        {
            health =
                player.AddComponent<PlayerHealth>();
        }

        // ================================================================
        // DASH
        // ================================================================

        PlayerDash dash =
            player.GetComponent<PlayerDash>();

        if (dash == null)
        {
            dash =
                player.AddComponent<PlayerDash>();
        }

        return player;
    }

    // =====================================================================
    // ENEMIGO
    // =====================================================================

    private void CrearEnemigo()
    {
        GameObject enemy =
            GameObject.Find("Enemy_Test");

        if (enemy != null)
        {
            EnemyNavigation navigationExisting =
                enemy.GetComponent<EnemyNavigation>();

            if (navigationExisting == null)
            {
                enemy.AddComponent<EnemyNavigation>();
            }

            EnemyChase chaseExisting =
                enemy.GetComponent<EnemyChase>();

            if (chaseExisting == null)
            {
                enemy.AddComponent<EnemyChase>();
            }

            EnemyHealth healthExisting =
                enemy.GetComponent<EnemyHealth>();

            if (healthExisting == null)
            {
                enemy.AddComponent<EnemyHealth>();
            }

            return;
        }

        enemy =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        enemy.name =
            "Enemy_Test";

        enemy.transform.position =
            enemySpawnPosition;

        enemy.transform.localScale =
            new Vector3(
                1f,
                1f,
                1f
            );

        // ================================================================
        // MATERIAL
        // ================================================================

        Renderer renderer =
            enemy.GetComponent<Renderer>();

        if (renderer != null)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                );

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Standard"
                    );
            }

            if (shader != null)
            {
                Material material =
                    new Material(shader);

                material.color =
                    new Color(
                        0.9f,
                        0.1f,
                        0.1f
                    );

                renderer.sharedMaterial =
                    material;
            }
        }

        // ================================================================
        // RIGIDBODY
        // ================================================================

        Rigidbody rb =
            enemy.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb =
                enemy.AddComponent<Rigidbody>();
        }

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        // ================================================================
        // VIDA
        // ================================================================

        enemy.AddComponent<EnemyHealth>();

        // ================================================================
        // NAVEGACIÓN
        // ================================================================

        enemy.AddComponent<EnemyNavigation>();

        // ================================================================
        // PERSECUCIÓN
        // ================================================================

        enemy.AddComponent<EnemyChase>();

        Debug.Log(
            "[SceneSetup] Enemy_Test creado."
        );
    }

    // =====================================================================
    // CÁMARA
    // =====================================================================

    private void ConfigurarCamara(
        GameObject player
    )
    {
        Camera mainCamera =
            Camera.main;

        if (mainCamera == null)
        {
            GameObject cameraObject =
                new GameObject(
                    "Main Camera"
                );

            cameraObject.tag =
                "MainCamera";

            mainCamera =
                cameraObject.AddComponent<Camera>();

            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.transform.position =
            new Vector3(
                0f,
                10f,
                -10f
            );

        mainCamera.transform.rotation =
            Quaternion.Euler(
                35f,
                0f,
                0f
            );

        CameraFollow cameraFollow =
            mainCamera.GetComponent<CameraFollow>();

        if (cameraFollow == null)
        {
            cameraFollow =
                mainCamera.gameObject.AddComponent<CameraFollow>();
        }

        if (player != null)
        {
            cameraFollow.SetTarget(
                player.transform
            );
        }

        Debug.Log(
            "[SceneSetup] Cámara configurada."
        );
    }

    // =====================================================================
    // HUD
    // =====================================================================

    private void CrearHUD()
    {
        // ================================================================
        // HEALTH UI
        // ================================================================

        HealthUI healthUI =
            FindAnyObjectByType<HealthUI>();

        if (healthUI == null)
        {
            GameObject healthObject =
                new GameObject(
                    "HealthUI"
                );

            healthObject.AddComponent<HealthUI>();
        }

        // ================================================================
        // DASH UI
        // ================================================================

        DashUI dashUI =
            FindAnyObjectByType<DashUI>();

        if (dashUI == null)
        {
            GameObject dashObject =
                new GameObject(
                    "DashUI"
                );

            dashObject.AddComponent<DashUI>();
        }

        // ================================================================
        // DAMAGE FEEDBACK UI
        // ================================================================

        DamageFeedbackUI damageFeedback =
            FindAnyObjectByType<DamageFeedbackUI>();

        if (damageFeedback == null)
        {
            GameObject damageObject =
                new GameObject(
                    "DamageFeedbackUI"
                );

            damageObject.AddComponent<DamageFeedbackUI>();
        }

        Debug.Log(
            "[SceneSetup] HUD y feedback visual configurados."
        );
    }
}