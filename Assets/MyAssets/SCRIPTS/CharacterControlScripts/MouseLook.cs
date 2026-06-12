using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Configuración de Sensibilidad")]
    public float mouseSensitivity = 100f;

    [Header("Referencia al Personaje")]
    public Transform playerBody; // Arrastra aquí al objeto padre "Player"

    private float xRotation = 0f;

    void Start()
    {
        // Bloquea el ratón en el centro de la pantalla y lo oculta para que no moleste
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
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
        playerBody.Rotate(Vector3.up * mouseX);
    }
}