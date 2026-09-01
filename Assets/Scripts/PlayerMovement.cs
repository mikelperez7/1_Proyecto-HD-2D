using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador de movimiento en 8 direcciones para un personaje estilo HD-2D.
/// Utiliza el nuevo sistema de entradas de Unity (Input System Package) mediante Keyboard y Gamepad,
/// así como un Rigidbody 3D para la física del personaje en un plano XZ.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad máxima de desplazamiento del jugador.")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("Tasa de aceleración al iniciar el movimiento.")]
    [SerializeField] private float acceleration = 50f;

    [Tooltip("Tasa de desaceleración/frenado al soltar los controles.")]
    [SerializeField] private float deceleration = 60f;

    [Header("Configuración HD-2D / Cámara")]
    [Tooltip("Ajusta el movimiento de forma relativa a la vista de la cámara activa.")]
    [SerializeField] private bool alignWithCamera = true;

    [Tooltip("Referencia a la cámara principal. Si no se asigna, se detectará automáticamente.")]
    [SerializeField] private Camera mainCamera;

    [Header("Configuración de Sprite")]
    [Tooltip("Invierte la orientación del SpriteRenderer horizontalmente según la dirección.")]
    [SerializeField] private bool flipSpriteOnDirection = true;

    [Tooltip("Referencia al SpriteRenderer del personaje (opcional, buscado en este objeto o hijos si no se asigna).")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    // Componentes e identificadores internos
    private Rigidbody rb;
    private Vector2 inputVector;
    private Vector3 targetVelocity;
    private Vector3 lastNonZeroDirection = Vector3.forward;

    private void Awake()
    {
        // Obtención de referencias principales
        rb = GetComponent<Rigidbody>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        ConfigurarRigidbody();
    }

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        ProcesarEntradaNewInputSystem();
        ActualizarOrientacionSprite();
    }

    private void FixedUpdate()
    {
        MoverJugador();
    }

    /// <summary>
    /// Configura las propiedades iniciales del Rigidbody para garantizar
    /// un comportamiento físico estable estilo 2.5D / HD-2D.
    /// </summary>
    private void ConfigurarRigidbody()
    {
        if (rb != null)
        {
            // Congelar rotaciones en X, Y, Z para que el personaje no se vuelque al colisionar
            rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                             RigidbodyConstraints.FreezeRotationY | 
                             RigidbodyConstraints.FreezeRotationZ;

            // Interpolación para un movimiento visual suave en pantalla
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Detección continua de colisiones para evitar traspasos
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    /// <summary>
    /// Captura las entradas utilizando la API del nuevo Input System (Keyboard y Gamepad activos).
    /// Elimina por completo las llamadas obsoletas a UnityEngine.Input.GetAxisRaw.
    /// </summary>
    private void ProcesarEntradaNewInputSystem()
    {
        float moveX = 0f;
        float moveZ = 0f;

        // 1. Lectura del Teclado (WASD y Flechas de dirección)
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;
        }

        // 2. Lectura alternativa desde Gamepad (Stick Izquierdo / D-Pad) si hay alguno conectado
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stickInput = gamepad.leftStick.ReadValue();
            Vector2 dpadInput = gamepad.dpad.ReadValue();

            if (stickInput.sqrMagnitude > 0.05f)
            {
                moveX = stickInput.x;
                moveZ = stickInput.y;
            }
            else if (dpadInput.sqrMagnitude > 0.05f)
            {
                moveX = dpadInput.x;
                moveZ = dpadInput.y;
            }
        }

        inputVector = new Vector2(moveX, moveZ);

        // Normalizar vector para mantener velocidad constante en movimiento diagonal
        if (inputVector.sqrMagnitude > 1f)
        {
            inputVector.Normalize();
        }
    }

    /// <summary>
    /// Aplica las fuerzas y velocidad al Rigidbody basándose en la dirección y la cámara.
    /// </summary>
    private void MoverJugador()
    {
        Vector3 moveDirection = CalcularDireccionMovimiento();

        // Calcular velocidad objetivo en el plano XZ manteniendo la velocidad Y (gravedad)
        targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        // Determinar tasa de aceleración o desaceleración según si hay entrada del jugador
        float rate = (inputVector.sqrMagnitude > 0.01f) ? acceleration : deceleration;

        // Transición fluida hacia la velocidad objetivo
        Vector3 newVelocity = Vector3.MoveTowards(rb.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
        newVelocity.y = rb.linearVelocity.y; // Conservar la velocidad de caída/gravedad

        rb.linearVelocity = newVelocity;

        // Guardar la última dirección de movimiento válida para encarar o animar
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            lastNonZeroDirection = moveDirection;
        }
    }

    /// <summary>
    /// Calcula la dirección 3D del plano XZ considerando si debe ser relativa a la cámara.
    /// </summary>
    /// <returns>Vector unitario con la dirección en el espacio 3D.</returns>
    private Vector3 CalcularDireccionMovimiento()
    {
        if (inputVector == Vector2.zero)
        {
            return Vector3.zero;
        }

        if (alignWithCamera && mainCamera != null)
        {
            // Obtener vectores de dirección de la cámara proyectados en el plano horizontal (XZ)
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            return (camRight * inputVector.x + camForward * inputVector.y).normalized;
        }

        // Movimiento en ejes del mundo por defecto (X = derecha/izquierda, Z = arriba/abajo)
        return new Vector3(inputVector.x, 0f, inputVector.y).normalized;
    }

    /// <summary>
    /// Maneja el volteo horizontal del SpriteRenderer según la dirección de movimiento.
    /// </summary>
private void ActualizarOrientacionSprite()
{
    if (!flipSpriteOnDirection || spriteRenderer == null || inputVector == Vector2.zero)
    {
        return;
    }
    if (inputVector.x < -0.01f)
    {
        spriteRenderer.flipX = true;   // ← ESCRITURA #1
    }
    else if (inputVector.x > 0.01f)
    {
        spriteRenderer.flipX = false;  // ← ESCRITURA #2
    }
}

    /// <summary>
    /// Propiedad pública para consultar el vector de dirección actual normalizado.
    /// </summary>
    public Vector3 Direction => lastNonZeroDirection;

    /// <summary>
    /// Propiedad pública para consultar si el personaje se encuentra actualmente en movimiento.
    /// </summary>
    public bool IsMoving => inputVector.sqrMagnitude > 0.01f;
}
