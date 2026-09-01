using UnityEngine;

/// <summary>
/// Enumeración para representar las 8 direcciones visuales posibles del jugador en HD-2D.
/// </summary>
public enum SpriteDirection8
{
    South = 0,       // Frente / Abajo
    SouthEast = 1,   // Diagonal Abajo-Derecha
    East = 2,        // Perfil Derecha
    NorthEast = 3,   // Diagonal Arriba-Derecha
    North = 4,       // Espalda / Arriba
    NorthWest = 5,   // Diagonal Arriba-Izquierda
    West = 6,        // Perfil Izquierda
    SouthWest = 7    // Diagonal Abajo-Izquierda
}

/// <summary>
/// Gestiona la orientación visual en 8 direcciones de un personaje HD-2D.
/// Determina el sector angular de movimiento en el plano 3D (XZ) relativo a la cámara,
/// selecciona el sprite o animación correspondiente y gestiona el volteo (flipX) inteligente.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerSpriteDirection : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al componente PlayerMovement. Se obtiene automáticamente si no se asigna.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Tooltip("SpriteRenderer donde se renderizará el sprite del jugador.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Cámara principal para calcular la dirección visual relativa. Se busca Camera.main si no se asigna.")]
    [SerializeField] private Camera mainCamera;

    [Header("Configuración de Sprites Estáticos (Idle / Estático)")]
    [Tooltip("Sprite mirando al frente (Abajo / South).")]
    [SerializeField] private Sprite spriteSouth;

    [Tooltip("Sprite mirando hacia la diagonal abajo (SouthEast / SouthWest).")]
    [SerializeField] private Sprite spriteSouthEast;

    [Tooltip("Sprite mirando de perfil (East / West).")]
    [SerializeField] private Sprite spriteEast;

    [Tooltip("Sprite mirando hacia la diagonal arriba (NorthEast / NorthWest).")]
    [SerializeField] private Sprite spriteNorthEast;

    [Tooltip("Sprite mirando hacia la espalda (Arriba / North).")]
    [SerializeField] private Sprite spriteNorth;

    [Header("Modo Espejo (FlipX)")]
    [Tooltip("Si es true, reutiliza los sprites del lado derecho (East, SouthEast, NorthEast) invirtiéndolos para el lado izquierdo (West, SouthWest, NorthWest).")]
    [SerializeField] private bool useFlipForLeftDirections = true;

    [Tooltip("Sprites específicos para el lado izquierdo en caso de no usar modo espejo (useFlipForLeftDirections = false).")]
    [SerializeField] private Sprite spriteWest;
    [SerializeField] private Sprite spriteSouthWest;
    [SerializeField] private Sprite spriteNorthWest;

    [Header("Configuración de Ángulos")]
    [Tooltip("Desfase angular en grados para alinear la dirección de inicio (0° = Frente/South).")]
    [SerializeField] private float angleOffset = 0f;

    // Estado interno
    private SpriteDirection8 currentDirection = SpriteDirection8.South;

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Aplicar sprite inicial en reposo
        ActualizarSpriteVisual(currentDirection);
    }

    private void Update()
    {
        ActualizarDireccionVisual();
    }

    /// <summary>
    /// Calcula la dirección visual actual del personaje basada en el movimiento del PlayerMovement.
    /// </summary>
    private void ActualizarDireccionVisual()
    {
        // Solo recalcula la dirección si el jugador se está moviendo
        if (playerMovement == null || !playerMovement.IsMoving)
        {
            return;
        }

        Vector3 moveDirection = playerMovement.Direction;
        if (moveDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        // 1. Proyectar la dirección de movimiento al plano 2D de la pantalla / cámara
        Vector2 screenDirection = ConvertirDireccionAMostradorCamara(moveDirection);

        // 2. Calcular el ángulo en grados (-180 a 180) usando Mathf.Atan2
        //    Atan2(y, x): 0° = Derecha (+X), 90° = Arriba (+Y), -90° = Abajo (-Y), 180/-180° = Izquierda (-X)
        float angle = Mathf.Atan2(screenDirection.y, screenDirection.x) * Mathf.Rad2Deg;

        // 3. Convertir el ángulo a 8 sectores (45° por sector)
        SpriteDirection8 newDirection = CalcularSector8Direcciones(angle);

        // 4. Si la dirección ha cambiado, actualizar el sprite visual
        if (newDirection != currentDirection)
        {
            currentDirection = newDirection;
            ActualizarSpriteVisual(currentDirection);
        }
    }

    /// <summary>
    /// Proyecta un vector de movimiento 3D (XZ) a un plano 2D visto desde la perspectiva de la cámara.
    /// Esto garantiza que "arriba" en pantalla sea hacia el fondo 3D y "abajo" hacia el frente 3D.
    /// </summary>
    /// <param name="dir3D">Vector de dirección en espacio de mundo 3D.</param>
    /// <returns>Vector 2D en espacio de pantalla (X = horizontal, Y = vertical).</returns>
    private Vector2 ConvertirDireccionAMostradorCamara(Vector3 dir3D)
    {
        if (mainCamera == null)
        {
            // Sin cámara, mapeo estándar plano XZ: X_2D = X_3D, Y_2D = Z_3D
            return new Vector2(dir3D.x, dir3D.z);
        }

        // Obtener la rotación Y de la cámara para cancelar la orientación de la vista
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        // Producto escalar para proyectar el vector sobre los ejes locales de la cámara
        float x2D = Vector3.Dot(dir3D, camRight);
        float y2D = Vector3.Dot(dir3D, camForward);

        return new Vector2(x2D, y2D);
    }

    /// <summary>
    /// Divide un círculo de 360° en 8 sectores de 45° cada uno y devuelve la dirección de la enumeración.
    /// </summary>
    /// <param name="angle">Ángulo en grados obtenido de Mathf.Atan2 (-180 a 180).</param>
    /// <returns>Elemento de la enumeración SpriteDirection8.</returns>
    private SpriteDirection8 CalcularSector8Direcciones(float angle)
    {
        // Aplicar desfase opcional y ajustar el ángulo a un rango positivo (0 a 360)
        float normalizedAngle = (angle + angleOffset + 360f) % 360f;

        // Reposicionar 0° para que apunte hacia ABAJO (South)
        // Por defecto Atan2 pone 0° a la derecha (East).
        // Restamos 270° (o sumamos 90°) para rotar el origen al sur (South = -90° / 270°).
        // Cada sector mide 45° (360 / 8 = 45°). Centramos cada sector sumando la mitad (22.5°).
        float shiftedAngle = (normalizedAngle + 90f + 22.5f) % 360f;

        // Dividir entre 45 grados para obtener un índice del 0 al 7
        int step = Mathf.FloorToInt(shiftedAngle / 45f);

        // Mapeo directo del índice de 45° a la enumeración de 8 direcciones:
        // Index 0: South (Frente)
        // Index 1: SouthEast (Diagonal Abajo-Derecha)
        // Index 2: East (Perfil Derecha)
        // Index 3: NorthEast (Diagonal Arriba-Derecha)
        // Index 4: North (Espalda)
        // Index 5: NorthWest (Diagonal Arriba-Izquierda)
        // Index 6: West (Perfil Izquierda)
        // Index 7: SouthWest (Diagonal Abajo-Izquierda)
        return (SpriteDirection8)(step % 8);
    }

    /// <summary>
    /// Asigna el sprite correspondiente al SpriteRenderer según la dirección activa y aplica flipX si procede.
    /// </summary>
    /// <param name="direction">Dirección calculada.</param>
    private void ActualizarSpriteVisual(SpriteDirection8 direction)
    {
        if (spriteRenderer == null) return;

        Sprite selectedSprite = null;
        bool shouldFlip = false;

        switch (direction)
        {
            case SpriteDirection8.South:
                selectedSprite = spriteSouth;
                shouldFlip = false;
                break;

            case SpriteDirection8.SouthEast:
                selectedSprite = spriteSouthEast;
                shouldFlip = false;
                break;

            case SpriteDirection8.East:
                selectedSprite = spriteEast;
                shouldFlip = false;
                break;

            case SpriteDirection8.NorthEast:
                selectedSprite = spriteNorthEast;
                shouldFlip = false;
                break;

            case SpriteDirection8.North:
                selectedSprite = spriteNorth;
                shouldFlip = false;
                break;

            case SpriteDirection8.NorthWest:
                if (useFlipForLeftDirections)
                {
                    selectedSprite = spriteNorthEast;
                    shouldFlip = true;
                }
                else
                {
                    selectedSprite = spriteNorthWest;
                    shouldFlip = false;
                }
                break;

            case SpriteDirection8.West:
                if (useFlipForLeftDirections)
                {
                    selectedSprite = spriteEast;
                    shouldFlip = true;
                }
                else
                {
                    selectedSprite = spriteWest;
                    shouldFlip = false;
                }
                break;

            case SpriteDirection8.SouthWest:
                if (useFlipForLeftDirections)
                {
                    selectedSprite = spriteSouthEast;
                    shouldFlip = true;
                }
                else
                {
                    selectedSprite = spriteSouthWest;
                    shouldFlip = false;
                }
                break;
        }

        // Asignar sprite sólo si ha cambiado
        if (selectedSprite != null)
        {
            spriteRenderer.sprite = selectedSprite;
        }

        // Aplicar volteo horizontal
        spriteRenderer.flipX = shouldFlip;
    }

    /// <summary>
    /// Dirección visual actual calculada (Read-Only).
    /// </summary>
    public SpriteDirection8 CurrentDirection => currentDirection;
}
