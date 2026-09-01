using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 50f;

    [Header("Muerte")]
    [SerializeField] private float deathDuration = 0.35f;
    [SerializeField] private float deathRotation = 90f;

    private float currentHealth;

    private Renderer enemyRenderer;
    private Material enemyMaterial;
    private EnemyChase enemyChase;
    private Collider enemyCollider;
    private Rigidbody rb;

    private Color originalColor;
    private bool isDead;

    public float Health => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => !isDead && currentHealth > 0f;

    private void Awake()
    {
        currentHealth = maxHealth;

        enemyRenderer =
            GetComponentInChildren<Renderer>();

        enemyChase =
            GetComponent<EnemyChase>();

        enemyCollider =
            GetComponent<Collider>();

        rb =
            GetComponent<Rigidbody>();

        if (enemyRenderer != null)
        {
            enemyMaterial =
                enemyRenderer.material;

            originalColor =
                enemyMaterial.color;
        }
    }

    public void RecibirDaño(float damage)
    {
        RecibirDaño(
            damage,
            DamageType.Physical
        );
    }

    public void RecibirDaño(
        float damage,
        DamageType damageType
    )
    {
        if (isDead)
            return;

        if (damage <= 0f)
            return;

        currentHealth -= damage;

        Debug.Log(
            $"[EnemyHealth] Daño recibido: {damage} | Vida restante: {currentHealth}"
        );

        AplicarFeedbackColor(
            damageType
        );

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Morir();
        }
    }

    private void AplicarFeedbackColor(
        DamageType damageType
    )
    {
        if (enemyMaterial == null)
            return;

        switch (damageType)
        {
            case DamageType.Poison:
                enemyMaterial.color =
                    new Color(
                        0.65f,
                        0.2f,
                        0.8f
                    );
                break;

            case DamageType.Fire:
                enemyMaterial.color =
                    new Color(
                        1f,
                        0.35f,
                        0.05f
                    );
                break;

            default:
                enemyMaterial.color =
                    Color.white;
                break;
        }

        CancelInvoke(
            nameof(RestaurarColor)
        );

        Invoke(
            nameof(RestaurarColor),
            0.15f
        );
    }

    private void RestaurarColor()
    {
        if (enemyMaterial != null &&
            !isDead)
        {
            enemyMaterial.color =
                originalColor;
        }
    }

    private void Morir()
    {
        if (isDead)
            return;

        isDead = true;

        if (enemyChase != null)
        {
            enemyChase.enabled = false;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.isKinematic = true;
        }

        StartCoroutine(
            AnimacionMuerte()
        );
    }

    private IEnumerator AnimacionMuerte()
    {
        Vector3 originalScale =
            transform.localScale;

        Quaternion originalRotation =
            transform.localRotation;

        float elapsed = 0f;

        while (elapsed < deathDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / deathDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            transform.localRotation =
                originalRotation *
                Quaternion.Euler(
                    deathRotation * smoothProgress,
                    0f,
                    0f
                );

            float scale =
                Mathf.Lerp(
                    1f,
                    0f,
                    smoothProgress
                );

            transform.localScale =
                originalScale * scale;

            yield return null;
        }

        transform.localScale =
            Vector3.zero;

        CrearExplosionPolvo();

        Destroy(
            gameObject
        );
    }

    private void CrearExplosionPolvo()
    {
        GameObject dust =
            new GameObject(
                "EnemyDeathDust"
            );

        dust.transform.position =
            transform.position;

        ParticleSystem particles =
            dust.AddComponent<ParticleSystem>();

        // IMPORTANTE:
        // Unity puede iniciar automáticamente el sistema
        // al añadir el componente. Lo detenemos inmediatamente
        // antes de modificar cualquier configuración.

        particles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        var main =
            particles.main;

        var emission =
            particles.emission;

        var shape =
            particles.shape;

        var particleRenderer =
            particles.GetComponent<ParticleSystemRenderer>();

        // ==============================
        // CONFIGURACIÓN
        // ==============================

        main.playOnAwake = false;
        main.duration = 0.35f;
        main.loop = false;

        main.startLifetime =
            new ParticleSystem.MinMaxCurve(
                0.15f,
                0.3f
            );

        main.startSpeed =
            new ParticleSystem.MinMaxCurve(
                1.5f,
                2.5f
            );

        main.startSize =
            new ParticleSystem.MinMaxCurve(
                0.08f,
                0.18f
            );

        main.startColor =
            new Color(
                0.65f,
                0.65f,
                0.55f,
                1f
            );

        main.gravityModifier = 1f;
        main.maxParticles = 12;

        emission.rateOverTime = 0f;

        emission.SetBursts(
            new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(
                    0f,
                    12
                )
            }
        );

        shape.shapeType =
            ParticleSystemShapeType.Sphere;

        shape.radius =
            0.15f;

        particleRenderer.renderMode =
            ParticleSystemRenderMode.Billboard;

        // ==============================
        // REPRODUCIR
        // ==============================

        particles.Play();

        StartCoroutine(
            DestruirPolvo(
                dust,
                particles
            )
        );
    }

    private IEnumerator DestruirPolvo(
        GameObject dust,
        ParticleSystem particles
    )
    {
        yield return new WaitForSeconds(
            1f
        );

        if (particles != null)
        {
            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (dust != null)
        {
            Destroy(
                dust
            );
        }
    }
}
