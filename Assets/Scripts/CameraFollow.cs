using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo a Seguir")]
    [SerializeField] private Transform target;


    [Header("Configuración de Posición y Distancia")]
    [SerializeField] private Vector3 offset =
        new Vector3(0f, 10f, -10f);

    [SerializeField] private Vector3 rotationAngle =
        new Vector3(35f, 0f, 0f);


    [Header("Suavizado y Amortiguación")]
    [Range(0.01f, 1f)]
    [SerializeField] private float smoothTime = 0.15f;

    [SerializeField] private bool autoFindPlayer = true;


    [Header("Límites de Cámara")]
    [SerializeField] private StageBounds stageBounds;


    // =====================================================================
    // SHAKE
    // =====================================================================

    [Header("Shake de daño")]
    [Tooltip("Duración del latigazo.")]
    [SerializeField] private float shakeDuration = 0.20f;

    [Tooltip("Intensidad horizontal del veneno.")]
    [SerializeField] private float poisonShakeStrength = 0.5f;

    [Tooltip("Intensidad vertical del fuego.")]
    [SerializeField] private float fireShakeStrength = 0.2f;

    [Tooltip("Número de latigazos.")]
    [SerializeField] private int shakeCycles = 1;


    // =====================================================================
    // ESTADO
    // =====================================================================

    private Vector3 velocity = Vector3.zero;

    private float shakeTimer = 0f;

    private float shakeProgress = 0f;

    private DamageType currentShakeType;

    private Vector3 naturalPosition;


    // =====================================================================
    // PROPIEDADES
    // =====================================================================

    public bool IsShaking =>
        shakeTimer > 0f;


    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        if (target == null && autoFindPlayer)
        {
            BuscarJugador();
        }


        if (stageBounds == null)
        {
            stageBounds =
                FindAnyObjectByType<StageBounds>();
        }
    }


    private void Start()
    {
        if (target == null && autoFindPlayer)
        {
            BuscarJugador();
        }

        AplicarRotacionInicial();
    }


    private void LateUpdate()
    {
        if (target == null)
        {
            if (autoFindPlayer)
            {
                BuscarJugador();
            }

            return;
        }


        // ================================================================
        // POSICIÓN NATURAL
        // ================================================================

        SeguirObjetivoSuavemente();


        naturalPosition =
            transform.position;


        // ================================================================
        // SHAKE
        // ================================================================

        if (shakeTimer > 0f)
        {
            AplicarShake();
        }
    }


    // =====================================================================
    // BUSCAR JUGADOR
    // =====================================================================

    public void BuscarJugador()
    {
        GameObject playerObj =
            GameObject.FindWithTag("Player");


        if (playerObj == null)
        {
            PlayerMovement movement =
                FindAnyObjectByType<PlayerMovement>();


            if (movement != null)
            {
                playerObj =
                    movement.gameObject;
            }
        }


        if (playerObj != null)
        {
            target =
                playerObj.transform;
        }
    }


    // =====================================================================
    // TARGET
    // =====================================================================

    public void SetTarget(
        Transform newTarget
    )
    {
        target =
            newTarget;
    }


    // =====================================================================
    // ROTACIÓN
    // =====================================================================

    private void AplicarRotacionInicial()
    {
        transform.rotation =
            Quaternion.Euler(
                rotationAngle
            );
    }


    // =====================================================================
    // SEGUIMIENTO
    // =====================================================================

    private void SeguirObjetivoSuavemente()
    {
        /*
         * IMPORTANTE:
         *
         * La cámara sigue directamente al jugador.
         *
         * No se aplican límites mediante StageBounds.
         *
         * Los límites físicos del mundo pertenecen al escenario,
         * no a la cámara.
         *
         * Esto evita que la cámara parezca tener una pared invisible
         * o un límite inferior/superior al llegar a los extremos del mapa.
         */

        Vector3 targetPosition =
            target.position +
            offset;


        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );
    }


    // =====================================================================
    // INICIAR SHAKE
    // =====================================================================

    public void TriggerDamageShake()
    {
        TriggerDamageShake(
            DamageType.Poison
        );
    }


    public void TriggerDamageShake(
        DamageType damageType
    )
    {
        shakeTimer =
            shakeDuration;


        shakeProgress =
            0f;


        currentShakeType =
            damageType;
    }


    // =====================================================================
    // APLICAR SHAKE
    // =====================================================================

    private void AplicarShake()
    {
        shakeProgress =
            1f -
            (
                shakeTimer /
                shakeDuration
            );


        shakeProgress =
            Mathf.Clamp01(
                shakeProgress
            );


        // ================================================================
        // ONDA
        // ================================================================

        float wave =
            Mathf.Sin(
                shakeProgress *
                Mathf.PI *
                2f *
                shakeCycles
            );


        // ================================================================
        // INTENSIDAD
        // ================================================================

        float strength;

        if (
            currentShakeType ==
            DamageType.Fire
        )
        {
            strength =
                fireShakeStrength;
        }
        else
        {
            strength =
                poisonShakeStrength;
        }


        // ================================================================
        // DIRECCIÓN
        // ================================================================

        Vector3 direction;

        if (
            currentShakeType ==
            DamageType.Fire
        )
        {
            // ARRIBA / ABAJO EN PANTALLA.

            direction =
                transform.up;
        }
        else
        {
            // IZQUIERDA / DERECHA EN PANTALLA.

            direction =
                transform.right;
        }


        // ================================================================
        // DESPLAZAMIENTO
        // ================================================================

        Vector3 shakeOffset =
            direction *
            wave *
            strength;


        transform.position =
            naturalPosition +
            shakeOffset;


        // ================================================================
        // TIMER
        // ================================================================

        shakeTimer -=
            Time.deltaTime;


        if (shakeTimer <= 0f)
        {
            shakeTimer =
                0f;

            shakeProgress =
                1f;

            // MUY IMPORTANTE:
            // vuelve inmediatamente a la posición natural.
            transform.position =
                naturalPosition;
        }
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        AplicarRotacionInicial();


        shakeDuration =
            Mathf.Max(
                0.01f,
                shakeDuration
            );


        poisonShakeStrength =
            Mathf.Max(
                0f,
                poisonShakeStrength
            );


        fireShakeStrength =
            Mathf.Max(
                0f,
                fireShakeStrength
            );


        shakeCycles =
            Mathf.Max(
                1,
                shakeCycles
            );
    }

#endif
}