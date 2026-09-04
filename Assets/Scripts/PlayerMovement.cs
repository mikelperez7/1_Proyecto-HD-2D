using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 60f;

    [Header("Configuración HD-2D / Cámara")]
    [SerializeField] private bool alignWithCamera = true;
    [SerializeField] private Camera mainCamera;

    [Header("Configuración de Sprite")]
    [SerializeField] private bool flipSpriteOnDirection = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody rb;
    private Vector2 inputVector;
    private Vector3 targetVelocity;
    private Vector3 lastNonZeroDirection = Vector3.forward;

    private Vector3 wallNormal;
    private Collider currentWallCollider;

    public float MoveSpeed => moveSpeed;

    public Vector3 Direction => lastNonZeroDirection;

    public bool IsMoving =>
        inputVector.sqrMagnitude > 0.01f;

    private void Awake()
    {
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

    private void ConfigurarRigidbody()
    {
        if (rb == null)
            return;

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;
    }

    private void ProcesarEntradaNewInputSystem()
    {
        float moveX = 0f;
        float moveZ = 0f;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed ||
                keyboard.upArrowKey.isPressed)
            {
                moveZ += 1f;
            }

            if (keyboard.sKey.isPressed ||
                keyboard.downArrowKey.isPressed)
            {
                moveZ -= 1f;
            }

            if (keyboard.dKey.isPressed ||
                keyboard.rightArrowKey.isPressed)
            {
                moveX += 1f;
            }

            if (keyboard.aKey.isPressed ||
                keyboard.leftArrowKey.isPressed)
            {
                moveX -= 1f;
            }
        }

        Gamepad gamepad = Gamepad.current;

        if (gamepad != null)
        {
            Vector2 stickInput =
                gamepad.leftStick.ReadValue();

            Vector2 dpadInput =
                gamepad.dpad.ReadValue();

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

        inputVector =
            new Vector2(
                moveX,
                moveZ
            );

        if (inputVector.sqrMagnitude > 1f)
        {
            inputVector.Normalize();
        }
    }

    private void MoverJugador()
    {
        Vector3 moveDirection =
            CalcularDireccionMovimiento();

        targetVelocity =
            moveDirection *
            moveSpeed;

        /*
         * No proyectamos la velocidad sobre la normal
         * de una pared.
         *
         * El Rigidbody y PhysX se encargan de resolver
         * la colisión físicamente.
         *
         * Esto permite que el jugador pueda separarse
         * inmediatamente de una pared al introducir
         * dirección contraria.
         */
        targetVelocity.y =
            rb.linearVelocity.y;

        float rate =
            (inputVector.sqrMagnitude > 0.01f)
                ? acceleration
                : deceleration;

        Vector3 newVelocity =
            Vector3.MoveTowards(
                rb.linearVelocity,
                targetVelocity,
                rate *
                Time.fixedDeltaTime
            );

        newVelocity.y =
            rb.linearVelocity.y;

        rb.linearVelocity =
            newVelocity;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            lastNonZeroDirection =
                moveDirection;
        }
    }

    private Vector3 CalcularDireccionMovimiento()
    {
        if (inputVector == Vector2.zero)
            return Vector3.zero;

        if (alignWithCamera &&
            mainCamera != null)
        {
            Vector3 camForward =
                mainCamera.transform.forward;

            Vector3 camRight =
                mainCamera.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            return (
                camRight * inputVector.x +
                camForward * inputVector.y
            ).normalized;
        }

        return new Vector3(
            inputVector.x,
            0f,
            inputVector.y
        ).normalized;
    }

    private void ActualizarOrientacionSprite()
    {
        if (!flipSpriteOnDirection ||
            spriteRenderer == null ||
            inputVector == Vector2.zero)
        {
            return;
        }

        if (inputVector.x < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
        else if (inputVector.x > 0.01f)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        RegistrarPared(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        RegistrarPared(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider ==
            currentWallCollider)
        {
            wallNormal =
                Vector3.zero;

            currentWallCollider =
                null;
        }
    }

    private void RegistrarPared(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 normal =
                contact.normal;

            // Una pared tiene una normal principalmente horizontal.
            // El suelo queda excluido.
            if (Mathf.Abs(normal.y) < 0.5f)
            {
                wallNormal =
                    normal.normalized;

                currentWallCollider =
                    collision.collider;

                return;
            }
        }
    }
}
