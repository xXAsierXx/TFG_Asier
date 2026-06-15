using UnityEngine;
using Unity.Netcode; // --- MULTIJUGADOR: Añadido para Netcode ---

// --- MULTIJUGADOR: Cambiado de 'MonoBehaviour' a 'NetworkBehaviour' ---
public class MouseLook : NetworkBehaviour
{
    [Header("Configuración de Sensibilidad")]
    public float mouseSensitivity = 100f;

    [Header("Referencia al Personaje")]
    public Transform playerBody; // Arrastra aquí al objeto padre "Player"

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // --- SOLUCIÓN AUTOMÁTICA DE REFERENCIA ---
        // Si no has arrastrado el PlayerBody en el inspector, lo buscamos en la raíz del prefab
        if (playerBody == null)
        {
            playerBody = transform.root; 
            // transform.root viaja hacia arriba en la jerarquía hasta encontrar el objeto padre definitivo (tu Player)
        }
    }

    void Update()
    {
        // --- MULTIJUGADOR: Filtro de Autoridad ---
        // Si esta cámara o este objeto no nos pertenece, salimos inmediatamente.
        // No queremos que el ratón de otro jugador mueva nuestra vista ni nuestro cuerpo.
        if (!IsOwner) return;
        // -----------------------------------------

        // 1. Capturar el movimiento del ratón
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2. Calcular la rotación vertical (Mirar arriba y abajo)
        xRotation -= mouseY;
        // Limitamos la rotación para que el jugador no pueda dar una voltereta con el cuello
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); 

        // Aplicar la rotación de arriba/abajo a la propia cámara
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 3. Rotar el cuerpo del personaje de izquierda a derecha (Eje Y)
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}