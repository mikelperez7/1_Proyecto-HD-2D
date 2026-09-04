using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

/// <summary>
/// Genera el mundo Graybox de prueba.
/// Crea geometría, trampas, límites y NavMesh.
/// </summary>
public class WorldGrayboxBuilder : MonoBehaviour
{
    [Header("Configuración general")]
    [SerializeField] private bool generateOnStart = true;

    [Header("NavMesh")]

    private Transform world;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerarGraybox();
        }
    }

    [ContextMenu("Generar Graybox")]
    public void GenerarGraybox()
    {
        LimpiarGraybox();

        world =
            CrearContenedor(
                "World"
            );

        CrearSuelo(world);

        CrearParedes(world);

        CrearPlataformas(world);

        CrearEscaleras(world);

        CrearObstaculos(world);

        CrearZonasVeneno(world);

        CrearZonasFuego(world);

        ConfigurarStageBounds();

        ConfigurarNavMesh(world);

        Debug.Log(
            "[WorldGrayboxBuilder] " +
            "Graybox generado correctamente."
        );
    }

    private void LimpiarGraybox()
    {
        GameObject previousWorld =
            GameObject.Find("World");

        if (previousWorld != null)
        {
            DestroyImmediate(
                previousWorld
            );
        }
    }

    // =====================================================================
    // SUELO
    // =====================================================================

    private void CrearSuelo(
        Transform container
    )
    {
        CrearCubo(
            "Ground",
            container,
            Vector3.zero,
            new Vector3(
                30f,
                1f,
                30f
            )
        );
    }

    // =====================================================================
    // PAREDES
    // =====================================================================

    private void CrearParedes(
        Transform container
    )
    {
        CrearCubo(
            "Wall_North",
            container,
            new Vector3(
                0f,
                1f,
                14.5f
            ),
            new Vector3(
                30f,
                2f,
                1f
            )
        );

        CrearCubo(
            "Wall_South",
            container,
            new Vector3(
                0f,
                1f,
                -14.5f
            ),
            new Vector3(
                30f,
                2f,
                1f
            )
        );

        CrearCubo(
            "Wall_East",
            container,
            new Vector3(
                14.5f,
                1f,
                0f
            ),
            new Vector3(
                1f,
                2f,
                30f
            )
        );

        CrearCubo(
            "Wall_West",
            container,
            new Vector3(
                -14.5f,
                1f,
                0f
            ),
            new Vector3(
                1f,
                2f,
                30f
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
                -6f,
                1.5f,
                3f
            ),
            new Vector3(
                5f,
                3f,
                5f
            )
        );

        CrearCubo(
            "Platform_02",
            container,
            new Vector3(
                7f,
                2f,
                5f
            ),
            new Vector3(
                4f,
                4f,
                4f
            )
        );
    }

    // =====================================================================
    // ESCALERAS
    // =====================================================================

    private void CrearEscaleras(
        Transform container
    )
    {
        Transform stairs =
            CrearContenedor(
                container,
                "Stairs"
            );

        for (
            int i = 0;
            i < 5;
            i++
        )
        {
            CrearCubo(
                "Step_" + i,
                stairs,
                new Vector3(
                    -6f + i * 0.8f,
                    0.2f + i * 0.2f,
                    0f
                ),
                new Vector3(
                    0.8f,
                    0.4f + i * 0.4f,
                    2f
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
                -2f,
                1f,
                6f
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
                3f,
                0.75f,
                1f
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
                8f,
                1f,
                -2f
            ),
            new Vector3(
                2f,
                2f,
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
         * Mantenemos exactamente el Agent Type que ya estaba
         * configurado en el NavMeshSurface.
         *
         * No creamos Agent Types runtime.
         */
        surface.BuildNavMesh();

        Debug.Log(
            "[WorldGrayboxBuilder] " +
            "NavMesh construido correctamente. " +
            "AgentTypeID: " +
            surface.agentTypeID
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
}
