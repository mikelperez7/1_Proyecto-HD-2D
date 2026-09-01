using UnityEngine;

public class RoomResetManager : MonoBehaviour
{
    public static RoomResetManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ResetRoom()
    {
        EnemyHealth[] enemies =
            FindObjectsByType<EnemyHealth>(
                FindObjectsInactive.Include
            );

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null)
                continue;

            enemy.ResetEnemy();
        }

        Debug.Log(
            $"[RoomResetManager] Sala reiniciada. " +
            $"Enemigos restaurados: {enemies.Length}"
        );
    }
}
