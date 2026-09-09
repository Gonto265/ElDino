using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    private float timer = 0f;
    private float currentSpawnInterval = 2.5f;

    void OnEnable()
    {
        timer = 0f;
        CalculateNextSpawnInterval();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= currentSpawnInterval)
        {
            Instantiate(obstaclePrefab, transform.position, Quaternion.identity);
            timer = 0f;
            CalculateNextSpawnInterval();
        }
    }

    void CalculateNextSpawnInterval()
    {
        float currentSpeed = Mathf.Max(MoveLeft.globalSpeed, 1f);

        // Definir si este obstáculo dará un espacio libre de descanso (25% de probabilidad)
        bool isRestZone = Random.value < 0.25f;

        float targetDistance;

        if (isRestZone)
        {
            // Espacio amplio para suelo libre y descanso
            targetDistance = Random.Range(15f, 22f);
        }
        else
        {
            // Salto normal o desafiante (Distancia segura garantizada)
            targetDistance = Random.Range(9f, 14f);
        }

        // Convertir la distancia a tiempo: Tiempo = Distancia / Velocidad
        currentSpawnInterval = targetDistance / currentSpeed;
    }
}