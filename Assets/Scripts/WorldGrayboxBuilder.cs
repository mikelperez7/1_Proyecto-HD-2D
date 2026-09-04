using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.AI.Navigation;

/// <summary>
/// Constructor del mapa Graybox de pruebas para el proyecto HD-2D.
///
/// Crea una zona de pruebas 3D con:
/// - Suelo
/// - Paredes
/// - Plataformas elevadas
/// - Rampas
/// - Escaleras
/// - Obstáculos
/// - Zonas de veneno
/// - Zonas de fuego
/// - Límites físicos del escenario
/// - NavMesh para navegación de enemigos
///
/// Es una herramienta de prototipo.
/// </summary>
public class WorldGrayboxBuilder : MonoBehaviour
{
    [Header("Dimensiones del mapa")]
    [SerializeField] private float mapWidth = 30f;
    [SerializeField] private float mapLength = 30f;
    [SerializeField] private float groundThickness = 0.5f;

    [Header("Alturas")]
    [SerializeField] private float platformHeight = 2f;

    [Header("Navegación")]
    [Tooltip("Construye automáticamente el NavMesh después de crear el Graybox.")]
    [SerializeField] private bool construirNavMesh = true;

    [Header("Organización")]
    [SerializeField] private bool limpiarMapaAnterior = true;

#if UNITY_EDITOR

    [MenuItem("HD-2D/Construir mapa Graybox de pruebas")]
    private static void MenuConstruirMapa()
    {
        WorldGrayboxBuilder builder =
            FindAnyObjectByType<WorldGrayboxBuilder>();

        if (builder == null)
        {
            GameObject builderObject =
                new GameObject("WorldGrayboxBuilder");

            builder =
                builderObject.AddComponent<WorldGrayboxBuilder>();
        }

        builder.ConstruirMapa();

        Debug.Log(
            "[WorldGrayboxBuilder] Mapa Graybox construido correctamente."
        );
    }

#endif

    public void ConstruirMapa()
    {
        if (limpiarMapaAnterior)
        {
            EliminarMapaAnterior();
        }

        Transform world =
            CrearContenedor("World");

        Transform groundContainer =
            CrearContenedor(
                world,
                "Ground"
            );

        Transform wallsContainer =
            CrearContenedor(
                world,
                "Walls"
            );

        Transform platformsContainer =
            CrearContenedor(
                world,
                "Platforms"
            );

        Transform rampsContainer =
            CrearContenedor(
                world,
                "Ramps"
            );

        Transform stairsContainer =
            CrearContenedor(
                world,
                "Stairs"
            );

        Transform obstaclesContainer =
            CrearContenedor(
                world,
                "Obstacles"
            );

        Transform poisonContainer =
            CrearContenedor(
                world,
                "PoisonZones"
            );

        Transform fireContainer =
            CrearContenedor(
                world,
                "FireZones"
            );

        CrearSuelo(
            groundContainer
        );

        CrearParedes(
            wallsContainer
        );

        CrearPlataformas(
            platformsContainer
        );

        CrearRampas(
            rampsContainer
        );

        CrearEscaleras(
            stairsContainer
        );

        CrearObstaculos(
            obstaclesContainer
        );

        CrearZonasVeneno(
            poisonContainer
        );

        CrearZonasFuego(
            fireContainer
        );

        ConfigurarStageBounds();

        if (construirNavMesh)
        {
            ConfigurarNavMesh(
                world
            );
        }

        Debug.Log(
            "[WorldGrayboxBuilder] " +
            "Zona de pruebas creada correctamente."
        );
    }

    // =====================================================================
    // SUELO
    // =====================================================================

    private void CrearSuelo(
        Transform container
    )
    {
        GameObject ground =
            CrearCubo(
                "Ground_Main",
                container,
                new Vector3(
                    0f,
                    -groundThickness * 0.5f,
                    0f
                ),
                new Vector3(
                    mapWidth,
                    groundThickness,
                    mapLength
                )
            );

        MeshCollider meshCollider =
            ground.GetComponent<MeshCollider>();

        if (meshCollider == null)
        {
            BoxCollider boxCollider =
                ground.GetComponent<BoxCollider>();

            if (boxCollider != null)
            {
                DestroyImmediateSafe(
                    boxCollider
                );
            }

            meshCollider =
                ground.AddComponent<MeshCollider>();
        }

        meshCollider.convex =
            false;
    }

    // =====================================================================
    // PAREDES
    // =====================================================================

    private void CrearParedes(
        Transform container
    )
    {
        float wallHeight = 3f;
        float wallThickness = 0.5f;

        CrearCubo(
            "Wall_North",
            container,
            new Vector3(
                0f,
                wallHeight * 0.5f,
                mapLength * 0.5f
            ),
            new Vector3(
                mapWidth,
                wallHeight,
                wallThickness
            )
        );

        CrearCubo(
            "Wall_South",
            container,
            new Vector3(
                0f,
                wallHeight * 0.5f,
                -mapLength * 0.5f
            ),
            new Vector3(
                mapWidth,
                wallHeight,
                wallThickness
            )
        );

        CrearCubo(
            "Wall_East",
            container,
            new Vector3(
                mapWidth * 0.5f,
                wallHeight * 0.5f,
                0f
            ),
            new Vector3(
                wallThickness,
                wallHeight,
                mapLength
            )
        );

        CrearCubo(
            "Wall_West",
            container,
            new Vector3(
                -mapWidth * 0.5f,
                wallHeight * 0.5f,
                0f
            ),
            new Vector3(
                wallThickness,
                wallHeight,
                mapLength
            )
        );

        // Paredes interiores para probar navegación alrededor de obstáculos.

        CrearCubo(
            "Wall_Interior_01",
            container,
            new Vector3(
                -5f,
                1f,
                3f
            ),
            new Vector3(
                8f,
                2f,
                0.5f
            )
        );

        CrearCubo(
            "Wall_Interior_02",
            container,
            new Vector3(
                5f,
                1f,
                -3f
            ),
            new Vector3(
                0.5f,
                2f,
                8f
            )
        );

        CrearCubo(
            "Wall_Interior_03",
            container,
            new Vector3(
                0f,
                1f,
                -1f
            ),
            new Vector3(
                6f,
                2f,
                0.5f
            )
        );

        CrearCubo(
            "Wall_Interior_04",
            container,
            new Vector3(
                -2f,
                1f,
                -6f
            ),
            new Vector3(
                0.5f,
                2f,
                6f
            )
        );
    }

    // =====================================================================
    // PLATAFORMAS
    // =====================================================================

    private void CrearPlataformas(
        Transform container
    )
    {
        CrearCubo(
            "Platform_01",
            container,
            new Vector3(
                -7f,
                platformHeight * 0.5f,
                8f
            ),
            new Vector3(
                7f,
                platformHeight,
                5f
            )
        );

        CrearCubo(
            "Platform_02",
            container,
            new Vector3(
                8f,
                platformHeight * 0.5f,
                7f
            ),
            new Vector3(
                5f,
                platformHeight,
                5f
            )
        );

        CrearCubo(
            "Platform_03",
            container,
            new Vector3(
                7f,
                1f,
                -8f
            ),
            new Vector3(
                6f,
                2f,
                4f
            )
        );
    }

    // =====================================================================
    // RAMPAS
    // =====================================================================

    private void CrearRampas(
        Transform container
    )
    {
        CrearRampa(
            "Ramp_01",
            container,
            new Vector3(
                -7f,
                1f,
                4f
            ),
            new Vector3(
                5f,
                2f,
                3f
            ),
            -20f
        );

        CrearRampa(
            "Ramp_02",
            container,
            new Vector3(
                3f,
                1f,
                8f
            ),
            new Vector3(
                4f,
                2f,
                3f
            ),
            20f
        );
    }

    private void CrearRampa(
        string nombre,
        Transform container,
        Vector3 posicion,
        Vector3 escala,
        float rotacionX
    )
    {
        GameObject ramp =
            CrearCubo(
                nombre,
                container,
                posicion,
                escala
            );

        ramp.transform.rotation =
            Quaternion.Euler(
                rotacionX,
                0f,
                0f
            );
    }

    // =====================================================================
    // ESCALERAS
    // =====================================================================

    private void CrearEscaleras(
        Transform container
    )
    {
        CrearEscalera(
            "Stairs_01",
            container,
            new Vector3(
                -1f,
                0f,
                7f
            ),
            false
        );

        CrearEscalera(
            "Stairs_02",
            container,
            new Vector3(
                -7f,
                0f,
                -7f
            ),
            true
        );
    }

    private void CrearEscalera(
        string nombre,
        Transform container,
        Vector3 posicion,
        bool rotar
    )
    {
        GameObject stairs =
            new GameObject(
                nombre
            );

        stairs.transform.SetParent(
            container
        );

        stairs.transform.position =
            posicion;

        if (rotar)
        {
            stairs.transform.rotation =
                Quaternion.Euler(
                    0f,
                    90f,
                    0f
                );
        }

        const int stepCount = 6;

        float stepWidth = 3f;
        float stepDepth = 0.6f;
        float stepHeight = 0.35f;

        for (int i = 0; i < stepCount; i++)
        {
            float height =
                stepHeight *
                (i + 1);

            float depth =
                stepDepth *
                i;

            CrearCubo(
                $"Step_{i + 1}",
                stairs.transform,
                new Vector3(
                    0f,
                    height * 0.5f,
                    depth
                ),
                new Vector3(
                    stepWidth,
                    height,
                    stepDepth
                )
            );
        }
    }

    // =====================================================================
    // OBSTÁCULOS
    // =====================================================================

    private void CrearObstaculos(
        Transform container
    )
    {
        CrearCubo(
            "Obstacle_Block_01",
            container,
            new Vector3(
                2f,
                1f,
                2f
            ),
            new Vector3(
                2f,
                2f,
                2f
            )
        );

        CrearCubo(
            "Obstacle_Block_02",
            container,
            new Vector3(
                -8f,
                0.75f,
                -1f
            ),
            new Vector3(
                3f,
                1.5f,
                2f
            )
        );

        CrearCubo(
            "Obstacle_Block_03",
            container,
            new Vector3(
                9f,
                0.5f,
                -1f
            ),
            new Vector3(
                1.5f,
                1f,
                3f
            )
        );

        CrearCubo(
            "Obstacle_Block_04",
            container,
            new Vector3(
                0f,
                0.5f,
                -8f
            ),
            new Vector3(
                4f,
                1f,
                1.5f
            )
        );
    }

    // =====================================================================
    // VENENO
    // =====================================================================

    private void CrearZonasVeneno(
        Transform container
    )
    {
        CrearTrampa(
            "PoisonZone_01",
            container,
            new Vector3(
                -10f,
                0.03f,
                7f
            ),
            new Vector3(
                3f,
                0.05f,
                3f
            ),
            true
        );

        CrearTrampa(
            "PoisonZone_02",
            container,
            new Vector3(
                7f,
                0.03f,
                -6f
            ),
            new Vector3(
                3f,
                0.05f,
                3f
            ),
            true
        );
    }

    // =====================================================================
    // FUEGO
    // =====================================================================

    private void CrearZonasFuego(
        Transform container
    )
    {
        CrearTrampa(
            "FireZone_01",
            container,
            new Vector3(
                -4f,
                0.03f,
                -7f
            ),
            new Vector3(
                3f,
                0.05f,
                3f
            ),
            false
        );

        CrearTrampa(
            "FireZone_02",
            container,
            new Vector3(
                9f,
                0.03f,
                4f
            ),
            new Vector3(
                3f,
                0.05f,
                3f
            ),
            false
        );
    }

    private void CrearTrampa(
        string nombre,
        Transform container,
        Vector3 posicion,
        Vector3 escala,
        bool poison
    )
    {
        GameObject trap =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        trap.name =
            nombre;

        trap.transform.SetParent(
            container
        );

        trap.transform.position =
            posicion;

        trap.transform.localScale =
            escala;

        Collider collider =
            trap.GetComponent<Collider>();

        if (collider != null)
        {
            collider.isTrigger =
                true;
        }

        Renderer renderer =
            trap.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial =
                CrearMaterial(
                    poison
                        ? new Color(
                            0.55f,
                            0.05f,
                            0.75f
                        )
                        : new Color(
                            1f,
                            0.18f,
                            0.02f
                        )
                );
        }

        if (poison)
        {
            trap.AddComponent<PoisonTrap>();
        }
        else
        {
            trap.AddComponent<FireTrap>();
        }
    }

    // =====================================================================
    // STAGE BOUNDS
    // =====================================================================

    private void ConfigurarStageBounds()
    {
        GameObject boundsObject =
            GameObject.Find(
                "StageBounds"
            );

        if (boundsObject == null)
        {
            boundsObject =
                new GameObject(
                    "StageBounds"
                );
        }

        StageBounds bounds =
            boundsObject.GetComponent<StageBounds>();

        if (bounds == null)
        {
            bounds =
                boundsObject.AddComponent<StageBounds>();
        }

        bounds.GenerarLimitesFisicos();
    }

    // =====================================================================
    // NAVMESH
    // =====================================================================

    private void ConfigurarNavMesh(
    Transform world
)
{
    NavMeshSurface surface =
        world.GetComponent<NavMeshSurface>();

    if (surface == null)
    {
        surface =
            world.gameObject.AddComponent<NavMeshSurface>();
    }

    /*
     * El NavMesh recoge los colliders de los hijos de World.
     *
     * Esto permite que el suelo, plataformas y demás geometría
     * participen en la construcción.
     */
    surface.collectObjects =
        CollectObjects.Children;

    /*
     * Utilizamos todos los layers porque el Graybox todavía
     * no tiene una separación definitiva de layers.
     */
    surface.layerMask =
        ~0;

    /*
     * Construimos el NavMesh después de haber creado TODO
     * el escenario.
     */
    surface.BuildNavMesh();

    Debug.Log(
        "[WorldGrayboxBuilder] " +
        "NavMesh construido correctamente."
    );
}

    // =====================================================================
    // CREACIÓN DE CUBOS
    // =====================================================================

    private GameObject CrearCubo(
        string nombre,
        Transform parent,
        Vector3 posicion,
        Vector3 escala
    )
    {
        GameObject obj =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        obj.name =
            nombre;

        obj.transform.SetParent(
            parent
        );

        obj.transform.position =
            posicion;

        obj.transform.localScale =
            escala;

        Collider collider =
            obj.GetComponent<Collider>();

        if (collider != null)
        {
            collider.isTrigger =
                false;
        }

        Renderer renderer =
            obj.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial =
                CrearMaterial(
                    new Color(
                        0.55f,
                        0.55f,
                        0.55f
                    )
                );
        }

        return obj;
    }

    // =====================================================================
    // MATERIAL
    // =====================================================================

    private Material CrearMaterial(
        Color color
    )
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

        Material material =
            new Material(
                shader
            );

        material.color =
            color;

        return material;
    }

    // =====================================================================
    // CONTENEDORES
    // =====================================================================

    private Transform CrearContenedor(
        string nombre
    )
    {
        GameObject obj =
            new GameObject(
                nombre
            );

        return obj.transform;
    }

    private Transform CrearContenedor(
        Transform parent,
        string nombre
    )
    {
        GameObject obj =
            new GameObject(
                nombre
            );

        obj.transform.SetParent(
            parent
        );

        obj.transform.localPosition =
            Vector3.zero;

        obj.transform.localRotation =
            Quaternion.identity;

        obj.transform.localScale =
            Vector3.one;

        return obj.transform;
    }

    // =====================================================================
    // LIMPIEZA
    // =====================================================================

    private void EliminarMapaAnterior()
    {
        GameObject world =
            GameObject.Find(
                "World"
            );

        if (world != null)
        {
            DestroyImmediateSafe(
                world
            );
        }
    }

    private void DestroyImmediateSafe(
        Object obj
    )
    {
        if (obj == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(
                obj
            );
        }
        else
        {
            Destroy(
                obj
            );
        }
#else
        Destroy(obj);
#endif
    }
}