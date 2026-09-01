using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script de inicialización rápida y automatizada para la escena HD-2D.
/// Crea automáticamente el suelo, el jugador, la cámara,
/// las trampas de veneno, las trampas de fuego,
/// los checkpoints y el enemigo de prueba.
/// </summary>
public class SceneSetup : MonoBehaviour
{
    [Header("Configuración Automática")]

    [Tooltip("Si está activo, generará los elementos de la escena automáticamente al pulsar Play si no existen.")]
    [SerializeField] private bool autoSetupOnPlay = true;


    private void Awake()
    {
        if (autoSetupOnPlay)
        {
            EjecutarConfiguracion();
        }
    }


    /// <summary>
    /// Inicialización automática al cargar la escena.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInicializarEnPlay()
    {
        if (FindAnyObjectByType<SceneSetup>() == null)
        {
            EjecutarConfiguracion();
        }
    }


#if UNITY_EDITOR

    /// <summary>
    /// Permite ejecutar la configuración desde el menú de Unity Editor.
    /// </summary>
    [MenuItem("HD-2D/Configurar Escena Automáticamente")]
    public static void MenuConfigurarEscena()
    {
        EjecutarConfiguracion();

        Debug.Log(
            "[SceneSetup] Escena HD-2D configurada con éxito desde el menú."
        );
    }

#endif


    /// <summary>
    /// Crea el suelo, jugador, cámara,
    /// trampas de veneno, trampas de fuego,
    /// checkpoints y enemigo.
    /// </summary>
    public static void EjecutarConfiguracion()
    {
        CrearSuelo();

        GameObject playerObj =
            CrearJugador();

        ConfigurarCamara(
            playerObj
        );

        CrearTrampasVeneno();

        CrearTrampasFuego();

        CrearCheckpoints();

        CrearEnemigo();
    }


    // =====================================================================
    // SUELO
    // =====================================================================

    private static void CrearSuelo()
    {
        GameObject ground =
            GameObject.Find("Suelo_Ground");

        if (ground == null)
        {
            ground =
                GameObject.CreatePrimitive(
                    PrimitiveType.Plane
                );

            ground.name =
                "Suelo_Ground";

            ground.transform.position =
                Vector3.zero;

            ground.transform.localScale =
                new Vector3(
                    3f,
                    1f,
                    3f
                );
        }


        Collider groundCol =
            ground.GetComponent<Collider>();

        if (groundCol == null)
        {
            groundCol =
                ground.AddComponent<MeshCollider>();
        }


        StageBounds bounds =
            ground.GetComponent<StageBounds>();

        if (bounds == null)
        {
            bounds =
                ground.AddComponent<StageBounds>();
        }


        bounds.GenerarLimitesFisicos();
    }


    // =====================================================================
    // JUGADOR
    // =====================================================================

    private static GameObject CrearJugador()
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
                new Vector3(
                    0f,
                    1f,
                    0f
                );


            Renderer cubeRenderer =
                player.GetComponent<Renderer>();


            if (cubeRenderer != null)
            {
                cubeRenderer.sharedMaterial =
                    new Material(
                        Shader.Find(
                            "Universal Render Pipeline/Lit"
                        )
                        ??
                        Shader.Find("Standard")
                    )
                    {
                        color =
                            new Color(
                                0.2f,
                                0.6f,
                                1f
                            )
                    };
            }
        }


        // Rigidbody

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


        // PlayerMovement

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement == null)
        {
            movement =
                player.AddComponent<PlayerMovement>();
        }


        // SpriteVisual

        Transform visualChild =
            player.transform.Find("SpriteVisual");


        if (visualChild == null)
        {
            GameObject visualObj =
                new GameObject(
                    "SpriteVisual"
                );

            visualObj.transform.SetParent(
                player.transform
            );

            visualObj.transform.localPosition =
                Vector3.zero;

            visualChild =
                visualObj.transform;
        }


        SpriteRenderer spriteRenderer =
            visualChild.GetComponent<SpriteRenderer>();


        if (spriteRenderer == null)
        {
            spriteRenderer =
                visualChild.gameObject
                    .AddComponent<SpriteRenderer>();
        }


        // PlayerSpriteDirection

        PlayerSpriteDirection spriteDir =
            player.GetComponent<PlayerSpriteDirection>();


        if (spriteDir == null)
        {
            spriteDir =
                player.AddComponent<PlayerSpriteDirection>();
        }


        // PlayerHealth

        PlayerHealth health =
            player.GetComponent<PlayerHealth>();


        if (health == null)
        {
            health =
                player.AddComponent<PlayerHealth>();
        }


        // PlayerDash

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

    /// <summary>
    /// Crea el enemigo de prueba.
    /// </summary>
    private static void CrearEnemigo()
    {
        if (
            GameObject.Find(
                "Enemy_Test"
            ) != null
        )
        {
            return;
        }


        GameObject enemy =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        enemy.name =
            "Enemy_Test";


        enemy.transform.position =
            new Vector3(
                8f,
                1f,
                8f
            );


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
            Material material =
                new Material(
                    Shader.Find(
                        "Universal Render Pipeline/Lit"
                    )
                    ??
                    Shader.Find("Standard")
                );

            material.color =
                new Color(
                    0.9f,
                    0.1f,
                    0.1f
                );

            renderer.sharedMaterial =
                material;
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

        EnemyHealth health =
            enemy.GetComponent<EnemyHealth>();

        if (health == null)
        {
            health =
                enemy.AddComponent<EnemyHealth>();
        }


        // ================================================================
        // PERSECUCIÓN
        // ================================================================

        EnemyChase chase =
            enemy.GetComponent<EnemyChase>();

        if (chase == null)
        {
            chase =
                enemy.AddComponent<EnemyChase>();
        }


        Debug.Log(
            "[SceneSetup] Enemigo de prueba creado."
        );
    }


    // =====================================================================
    // TRAMPAS DE VENENO
    // =====================================================================

    private static void CrearTrampasVeneno()
    {
        if (
            GameObject.Find(
                "PoisonTrap_0"
            ) != null
        )
        {
            return;
        }


        Vector3[] posiciones =
        {
            new Vector3(-6f, 0.02f,  4f),
            new Vector3( 5f, 0.02f,  5f),
            new Vector3(-7f, 0.02f, -5f),
            new Vector3( 6f, 0.02f, -4f),
            new Vector3( 0f, 0.02f,  7f),
            new Vector3( 2f, 0.02f, -8f)
        };


        for (int i = 0; i < posiciones.Length; i++)
        {
            CrearTrampaVeneno(
                $"PoisonTrap_{i}",
                posiciones[i]
            );
        }
    }


    private static void CrearTrampaVeneno(
        string nombre,
        Vector3 posicion
    )
    {
        GameObject trap =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );


        trap.name =
            nombre;

        trap.transform.position =
            posicion;

        trap.transform.localScale =
            new Vector3(
                1.5f,
                0.02f,
                1.5f
            );


        Renderer renderer =
            trap.GetComponent<Renderer>();


        if (renderer != null)
        {
            Material material =
                new Material(
                    Shader.Find(
                        "Universal Render Pipeline/Lit"
                    )
                    ??
                    Shader.Find("Standard")
                );


            material.color =
                new Color(
                    0.55f,
                    0.05f,
                    0.75f,
                    1f
                );


            renderer.sharedMaterial =
                material;
        }


        Collider collider =
            trap.GetComponent<Collider>();


        if (collider != null)
        {
            collider.isTrigger =
                true;
        }


        PoisonTrap poison =
            trap.GetComponent<PoisonTrap>();


        if (poison == null)
        {
            poison =
                trap.AddComponent<PoisonTrap>();
        }
    }


    // =====================================================================
    // TRAMPAS DE FUEGO
    // =====================================================================

    private static void CrearTrampasFuego()
    {
        if (
            GameObject.Find(
                "FireTrap_0"
            ) != null
        )
        {
            return;
        }


        Vector3[] posiciones =
        {
            new Vector3(-3f, 0.02f,  6f),
            new Vector3( 7f, 0.02f,  2f),
            new Vector3(-5f, 0.02f, -2f),
            new Vector3( 4f, 0.02f, -7f),
            new Vector3(-1f, 0.02f, -7f),
            new Vector3( 7f, 0.02f, -6f)
        };


        for (int i = 0; i < posiciones.Length; i++)
        {
            CrearTrampaFuego(
                $"FireTrap_{i}",
                posiciones[i]
            );
        }
    }


    private static void CrearTrampaFuego(
        string nombre,
        Vector3 posicion
    )
    {
        GameObject trap =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );


        trap.name =
            nombre;

        trap.transform.position =
            posicion;


        float escala =
            Random.Range(
                0.8f,
                1.1f
            );


        trap.transform.localScale =
            new Vector3(
                1.2f * escala,
                0.02f,
                1.2f * escala
            );


        trap.transform.rotation =
            Quaternion.Euler(
                0f,
                Random.Range(
                    0f,
                    360f
                ),
                0f
            );


        Renderer renderer =
            trap.GetComponent<Renderer>();


        if (renderer != null)
        {
            Material material =
                new Material(
                    Shader.Find(
                        "Universal Render Pipeline/Lit"
                    )
                    ??
                    Shader.Find("Standard")
                );


            material.color =
                new Color(
                    1f,
                    0.18f,
                    0.02f,
                    1f
                );


            renderer.sharedMaterial =
                material;
        }


        Collider collider =
            trap.GetComponent<Collider>();


        if (collider != null)
        {
            collider.isTrigger =
                true;
        }


        FireTrap fire =
            trap.GetComponent<FireTrap>();


        if (fire == null)
        {
            fire =
                trap.AddComponent<FireTrap>();
        }
    }


    // =====================================================================
    // CÁMARA
    // =====================================================================

    private static void ConfigurarCamara(
        GameObject player
    )
    {
        Camera mainCam =
            Camera.main;


        if (mainCam == null)
        {
            GameObject camObj =
                new GameObject(
                    "Main Camera"
                );


            camObj.tag =
                "MainCamera";


            mainCam =
                camObj.AddComponent<Camera>();


            camObj.AddComponent<AudioListener>();
        }


        mainCam.transform.position =
            new Vector3(
                0f,
                7f,
                -10f
            );


        mainCam.transform.rotation =
            Quaternion.Euler(
                35f,
                0f,
                0f
            );


        CameraFollow cameraFollow =
            mainCam.GetComponent<CameraFollow>();


        if (cameraFollow == null)
        {
            cameraFollow =
                mainCam.gameObject
                    .AddComponent<CameraFollow>();
        }


        if (player != null)
        {
            cameraFollow.SetTarget(
                player.transform
            );
        }
    }


    // =====================================================================
    // CHECKPOINTS
    // =====================================================================

    private static void CrearCheckpoints()
    {
        if (
            GameObject.Find(
                "Checkpoint_0"
            ) != null
        )
        {
            return;
        }


        GameObject ground =
            GameObject.Find(
                "Suelo_Ground"
            );


        if (ground == null)
        {
            Debug.LogWarning(
                "[SceneSetup] No se encontró el suelo para crear checkpoints."
            );

            return;
        }


        StageBounds bounds =
            ground.GetComponent<StageBounds>();


        if (bounds == null)
        {
            Debug.LogWarning(
                "[SceneSetup] No se encontró StageBounds para crear checkpoints."
            );

            return;
        }


        Bounds playBounds =
            bounds.GetPlayAreaBounds();


        float margin =
            2.5f;


        float xMin =
            playBounds.min.x + margin;

        float xMax =
            playBounds.max.x - margin;

        float zMin =
            playBounds.min.z + margin;

        float zMax =
            playBounds.max.z - margin;


        Vector3[] posiciones =
        {
            new Vector3(
                xMin,
                0.04f,
                zMin
            ),

            new Vector3(
                xMax,
                0.04f,
                zMin
            ),

            new Vector3(
                xMin,
                0.04f,
                zMax
            ),

            new Vector3(
                xMax,
                0.04f,
                zMax
            )
        };


        for (
            int i = 0;
            i < posiciones.Length;
            i++
        )
        {
            CrearCheckpoint(
                $"Checkpoint_{i}",
                posiciones[i]
            );
        }


        Debug.Log(
            "[SceneSetup] 4 checkpoints creados."
        );
    }


    private static void CrearCheckpoint(
        string nombre,
        Vector3 posicion
    )
    {
        GameObject checkpoint =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );


        checkpoint.name =
            nombre;


        checkpoint.transform.position =
            posicion;


        checkpoint.transform.localScale =
            new Vector3(
                1.5f,
                0.04f,
                1.5f
            );


        Renderer renderer =
            checkpoint.GetComponent<Renderer>();


        if (renderer != null)
        {
            Material material =
                new Material(
                    Shader.Find(
                        "Universal Render Pipeline/Unlit"
                    )
                    ??
                    Shader.Find("Standard")
                );


            material.color =
                new Color(
                    0.1f,
                    1f,
                    0.2f,
                    1f
                );


            renderer.sharedMaterial =
                material;
        }


        BoxCollider collider =
            checkpoint.GetComponent<BoxCollider>();


        if (collider != null)
        {
            collider.isTrigger =
                true;
        }


        checkpoint.AddComponent<Checkpoint>();
    }
}