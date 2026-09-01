using UnityEngine;

/// <summary>
/// Define los límites físicos perimetrales para una zona jugable en un escenario 3D / HD-2D.
/// Genera o configura de forma dinámica o estática colisionadores (BoxCollider) infranqueables
/// alrededor del perímetro de la superficie jugable (p. ej. un suelo o plataforma).
/// </summary>
public class StageBounds : MonoBehaviour
{
    [Header("Superficie de Referencia")]
    [Tooltip("Collider o Transform de la plataforma/suelo jugable. Si no se asigna, se intentará detectar automáticamente en el mismo objeto.")]
    [SerializeField] private Collider groundCollider;

    [Header("Dimensiones Manuales (Usar si no hay Ground Collider)")]
    [Tooltip("Ancho de la zona jugable en el eje X (se ignora si groundCollider está asignado y usarBoundingBoxDelSuelo es true).")]
    [SerializeField] private float areaWidthX = 30f;

    [Tooltip("Largo de la zona jugable en el eje Z (se ignora si groundCollider está asignado y usarBoundingBoxDelSuelo es true).")]
    [SerializeField] private float areaLengthZ = 30f;

    [Tooltip("Centro del área jugable.")]
    [SerializeField] private Vector3 areaCenter = Vector3.zero;

    [Header("Propiedades del Muro/Límite")]
    [Tooltip("Si es true, calcula las dimensiones exactas a partir del Bounds del groundCollider.")]
    [SerializeField] private bool autoDetectFromGround = false;

    [Tooltip("Altura de los muros invisibles para evitar que el jugador salte o atraviese por arriba.")]
    [SerializeField] private float wallHeight = 10f;

    [Tooltip("Grosor de los muros físicos para evitar atravesamientos a altas velocidades.")]
    [SerializeField] private float wallThickness = 2f;

    [Tooltip("Material de física (PhysicMaterial/PhysicsMaterial) con fricción 0 y sin rebotabilidad para deslizamiento perfecto contra paredes.")]
    [SerializeField] private PhysicsMaterial wallPhysicsMaterial;

    // Objeto contenedor de los colliders
    private GameObject wallsContainer;

    private void Awake()
    {
        GenerarLimitesFisicos();
    }

    /// <summary>
    /// Construye o actualiza las 4 paredes físicas alrededor del perímetro del escenario.
    /// </summary>
    public void GenerarLimitesFisicos()
    {
        // 1. Obtener dimensiones del área jugable
        Vector3 center = areaCenter;
        float sizeX = areaWidthX;
        float sizeZ = areaLengthZ;

        if (groundCollider == null)
        {
            groundCollider = GetComponent<Collider>();
        }

        if (autoDetectFromGround && groundCollider != null)
        {
            Bounds bounds = groundCollider.bounds;
            center = bounds.center;
            sizeX = bounds.size.x;
            sizeZ = bounds.size.z;
        }

        // 2. Crear material físico anti-fricción/anti-rebote si no se ha asignado uno
        if (wallPhysicsMaterial == null)
        {
            wallPhysicsMaterial = CrearMaterialFisicoSinFriccion();
        }

        // 3. Crear o limpiar el contenedor de los muros
        if (wallsContainer == null)
        {
            Transform existing = transform.Find("StagePhysicalBounds");
            if (existing != null)
            {
                wallsContainer = existing.gameObject;
            }
            else
            {
                wallsContainer = new GameObject("StagePhysicalBounds");
                wallsContainer.transform.SetParent(transform);
                wallsContainer.transform.localPosition = Vector3.zero;
                wallsContainer.transform.localRotation = Quaternion.identity;
            }
        }

        // Eliminar hijos previos si se regenera
        for (int i = wallsContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(wallsContainer.transform.GetChild(i).gameObject);
        }

        // 4. Calcular posiciones exactas del perímetro
        // El borde exterior del suelo está en center.x +/- (sizeX / 2) y center.z +/- (sizeZ / 2).
        // Colocamos el centro del BoxCollider pegado justo por fuera del borde para que la cara interna
        // del muro coincida exactamente con el borde del suelo:
        // Offset del centro del muro respecto al borde = wallThickness / 2
        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;
        float halfThickness = wallThickness * 0.5f;
        float halfHeight = wallHeight * 0.5f;

        // Muro Norte (+Z)
        Vector3 posNorth = new Vector3(center.x, center.y + halfHeight, center.z + halfZ + halfThickness);
        Vector3 sizeNorth = new Vector3(sizeX + (wallThickness * 2f), wallHeight, wallThickness); // Sobrepasa las esquinas
        CrearMuroColision("Wall_North", posNorth, sizeNorth);

        // Muro Sur (-Z)
        Vector3 posSouth = new Vector3(center.x, center.y + halfHeight, center.z - halfZ - halfThickness);
        Vector3 sizeSouth = new Vector3(sizeX + (wallThickness * 2f), wallHeight, wallThickness);
        CrearMuroColision("Wall_South", posSouth, sizeSouth);

        // Muro Este (+X)
        Vector3 posEast = new Vector3(center.x + halfX + halfThickness, center.y + halfHeight, center.z);
        Vector3 sizeEast = new Vector3(wallThickness, wallHeight, sizeZ + (wallThickness * 2f));
        CrearMuroColision("Wall_East", posEast, sizeEast);

        // Muro Oeste (-X)
        Vector3 posWest = new Vector3(center.x - halfX - halfThickness, center.y + halfHeight, center.z);
        Vector3 sizeWest = new Vector3(wallThickness, wallHeight, sizeZ + (wallThickness * 2f));
        CrearMuroColision("Wall_West", posWest, sizeWest);
    }

    /// <summary>
    /// Instancia un objeto individual con BoxCollider para actuar como muro rígido del escenario.
    /// </summary>
    private void CrearMuroColision(string wallName, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(wallsContainer.transform);
        wall.transform.position = position;
        wall.transform.rotation = Quaternion.identity;
        wall.layer = gameObject.layer; // Hereda la capa del escenario o escenario estático

        BoxCollider box = wall.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = false; // Muro físico estático (infranqueable)
        box.sharedMaterial = wallPhysicsMaterial;
    }

    /// <summary>
    /// Crea un PhysicsMaterial optimizado para paredes estáticas (fricción 0, sin rebote)
    /// lo que evita que el jugador se quede pegado al deslizarse o rebote al chocar.
    /// </summary>
    private PhysicsMaterial CrearMaterialFisicoSinFriccion()
    {
        PhysicsMaterial mat = new PhysicsMaterial("SmoothWallMaterial")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        return mat;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualización conveniente en el Scene View de Unity
        Gizmos.color = new Color(0.1f, 0.9f, 0.2f, 0.4f);

        Vector3 center = areaCenter;
        float sizeX = areaWidthX;
        float sizeZ = areaLengthZ;

        if (autoDetectFromGround && groundCollider != null)
        {
            Bounds bounds = groundCollider.bounds;
            center = bounds.center;
            sizeX = bounds.size.x;
            sizeZ = bounds.size.z;
        }

    Gizmos.DrawWireCube(new Vector3(center.x, center.y + (wallHeight * 0.5f), center.z), new Vector3(sizeX, wallHeight, sizeZ));
}

/// <summary>
/// Devuelve el Bounds del área jugable (centro y dimensiones X/Z) reutilizando
/// la misma lógica de detección que GenerarLimitesFisicos.
/// </summary>
public Bounds GetPlayAreaBounds()
{
    Vector3 center = areaCenter;
    float sizeX = areaWidthX;
    float sizeZ = areaLengthZ;

    if (groundCollider == null)
    {
        groundCollider = GetComponent<Collider>();
    }

    if (autoDetectFromGround && groundCollider != null)
    {
        Bounds b = groundCollider.bounds;
        center = b.center;
        sizeX = b.size.x;
        sizeZ = b.size.z;
    }

    return new Bounds(center, new Vector3(sizeX, wallHeight, sizeZ));
}
}