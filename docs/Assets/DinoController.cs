using UnityEngine;
using UnityEngine.InputSystem;

public class DinoController : MonoBehaviour
{
    [Header("Configuración del Juego")]
    public float jumpForce = 10f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isDead = false;

    [Header("Puntaje")]
    public float score = 0f;
    public float scoreSpeed = 5f;

    private Vector3 initialPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialPosition = transform.position;
    }

    void OnEnable()
    {
        isDead = false;
        score = 0f;
        transform.position = initialPosition;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        MoveLeft.ResetSpeed();
    }

    void Update()
    {
        if (isDead) return;

        // Detección de suelo mejorada (Si no hay groundCheck asignado, asume verdadero para no bloquear el salto)
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.3f, groundLayer);
        }
        else
        {
            isGrounded = true; 
        }

        // Detección de teclas del nuevo Input System
        bool jumpInput = false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpInput = true;

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            jumpInput = true;

        if (jumpInput && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Incrementar puntaje
        score += Time.deltaTime * scoreSpeed;
    }

    // Detección de colisión para finalizar el juego
    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckObstacleCollision(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckObstacleCollision(collision.gameObject);
    }

    private void CheckObstacleCollision(GameObject obj)
    {
        if (obj.CompareTag("Obstacle") && !isDead)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        int finalScore = Mathf.RoundToInt(score);
        Debug.Log("¡Game Over! Puntaje final enviado: " + finalScore);

        // ELIMINAR TODOS LOS PREFABS DE OBSTÁCULOS EXISTENTES EN LA ESCENA
        ClearAllObstacles();

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SubmitGameScore(finalScore);
        }
    }

void ClearAllObstacles()
    {
        // Busca todos los GameObjects que tengan la etiqueta "Obstacle"
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        
        foreach (GameObject obstacle in obstacles)
        {
            Destroy(obstacle);
        }
    }
}