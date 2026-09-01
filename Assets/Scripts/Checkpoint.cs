using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Colores")]
    [SerializeField] private Color inactiveColor =
        new Color(0.1f, 1f, 0.2f, 1f);

    [SerializeField] private Color activeColor =
        new Color(0.4f, 1f, 0.2f, 1f);

    private Renderer checkpointRenderer;

    private void Awake()
    {
        checkpointRenderer =
            GetComponent<Renderer>();

        if (checkpointRenderer != null)
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
                inactiveColor;

            checkpointRenderer.sharedMaterial =
                material;
        }

        BoxCollider trigger =
            GetComponent<BoxCollider>();

        if (trigger == null)
        {
            trigger =
                gameObject.AddComponent<BoxCollider>();
        }

        trigger.isTrigger =
            true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth =
            other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth =
                other.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null)
            return;

        playerHealth.EstablecerCheckpoint(
            transform
        );

        ActivarCheckpoint();
    }

    private void ActivarCheckpoint()
    {
        if (checkpointRenderer != null)
        {
            checkpointRenderer.material.color =
                activeColor;
        }

        Checkpoint[] checkpoints =
            FindObjectsByType<Checkpoint>();

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (checkpoint != this)
            {
                checkpoint.DesactivarCheckpoint();
            }
        }

        Debug.Log(
            $"[Checkpoint] Activado: {gameObject.name}"
        );
    }

    private void DesactivarCheckpoint()
    {
        if (checkpointRenderer != null)
        {
            checkpointRenderer.material.color =
                inactiveColor;
        }
    }
}