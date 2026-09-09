using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    public static float globalSpeed = 5.5f;   // Velocidad inicial cómoda
    public static float maxSpeed = 13.5f;     // Velocidad máxima desafiante y justa
    public static float acceleration = 0.05f; // Aceleración muy paulatina por segundo

    private static float initialSpeed = 5.5f;

    void Update()
    {
        // Incrementar la velocidad suavemente con el tiempo
        if (globalSpeed < maxSpeed)
        {
            globalSpeed += acceleration * Time.deltaTime;
        }

        transform.Translate(Vector3.left * globalSpeed * Time.deltaTime);

        if (transform.position.x < -12f)
        {
            Destroy(gameObject);
        }
    }

    public static void ResetSpeed()
    {
        globalSpeed = initialSpeed;
    }
}