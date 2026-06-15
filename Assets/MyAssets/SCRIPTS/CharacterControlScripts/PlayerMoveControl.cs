using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMoveControl : MonoBehaviour
{
    [Header("Configuración de Velocidad")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float crouchSpeed = 2.5f;

    [Header("Físicas y Salto")]
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    private Vector3 velocity;

    [Header("Configuración de Agachado (Altura Y)")] //Offset de la camara agachado
    public Transform playerCamera;     
    public float standingHeight = 2f;  
    public float crouchHeight = 1f;    
    public float standingCamY = 1.6f;  
    public float crouchCamY = 0.8f;    
    public float crouchSmoothTime = 8f; 
    
    [Header("Ajustes de Offset Frontal (Distancia Z)")] //Offset de la camara adelantada (De pie / Agachado)
    public float standingForwardOffset = 0.0f; 
    public float crouchForwardOffset = 0.25f; 

    [Header("Configuración de Audio")]
    public AudioSource audioSource;
    
    [Space(5)]
    public AudioClip walkSound;          // archivo "Walk audio"
    [Tooltip("Tiempo en segundos entre cada pisada al caminar.")]
    public float walkStepInterval = 0.5f; 
    [Range(0f, 1f)] public float walkVolume = 0.4f; 
    public float walkMaxDistance = 10f;             
    
    [Space(5)]
    public AudioClip sprintSound;        // archivo de audio para correr
    [Tooltip("Tiempo en segundos entre cada pisada al correr.")]
    public float sprintStepInterval = 0.3f; 
    [Range(0f, 1f)] public float sprintVolume = 0.9f; 
    public float sprintMaxDistance = 25f;             

    [Space(5)]
    [Header("Sonido de Aterrizaje")]
    public AudioClip landSound;          // archivo de audio para cuando cae al suelo
    [Range(0f, 1f)] public float landVolume = 0.7f;   // volumen del impacto
    public float landMaxDistance = 20f;               // Distancia en metros que alcanza el sonido de caída
    [Tooltip("Velocidad de caída mínima para activar el sonido.")]
    public float landVelocityThreshold = -4f; 

    [Header("Filtro de Pequeños Obstáculos (Piedras/Baches)")]
    [Tooltip("Tiempo mínimo (en segundos) que el jugador debe estar en el aire para considerarse una caída real. Evita ruidos y animaciones en piedras.")]
    public float miniBumpThreshold = 0.2f; // tiempo que esta en el aire sin animacion

    private float footstepTimer = 0f;     // Temporizador interno interno para pasos
    private float airTimer = 0f;          // Temporizador para medir cuánto tiempo real lleva flotando
    private bool wasGrounded = true;      // Guarda el estado del suelo del frame anterior

    private bool isCrouched = false;
    private float targetCamY;           
    private float targetForwardOffset;  

    [Header("Asignación Manual")]
    public Animator animator; 

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(); 
        }

        if (playerCamera != null)
        {
            targetCamY = standingCamY;
            targetForwardOffset = standingForwardOffset;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Creamos una variable local inteligente para el suelo
        bool isGrounded = controller.isGrounded;

        // ASISTENCIA EN RAMPAS: Si el componente dice que flotamos, pero veníamos de estar en el suelo y vamos hacia abajo...
        if (!isGrounded && wasGrounded && velocity.y <= 0)
        {
            Vector3 rayOrigin = transform.position + controller.center;
            float rayDistance = (controller.height / 2f) + 0.6f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance))
            {
                isGrounded = true;
                velocity.y = -5f; // Fuerza de pegado a la rampa
            }
        }

        // 1. GESTIÓN DE SUELO, GRAVEDAD Y CAÍDAS
        if (isGrounded)
        {
            // DETECCIÓN DE ATERRIZAJE REAL: Solo si estuvo en el aire más tiempo que el umbral de baches (miniBumpThreshold)
            if (!wasGrounded && airTimer > miniBumpThreshold && velocity.y < landVelocityThreshold)
            {
                if (audioSource != null && landSound != null)
                {
                    audioSource.clip = landSound;
                    audioSource.volume = landVolume;
                    audioSource.maxDistance = landMaxDistance;
                    audioSource.Play();
                }

                footstepTimer = Input.GetKey(KeyCode.LeftShift) ? sprintStepInterval : walkStepInterval;
            }

            // Reseteamos el contador de tiempo en el aire ya que estamos tocando el suelo
            airTimer = 0f;

            // Bajamos esto a -2f para que la velocidad base en el suelo no interfiera con el umbral de caída de -4f
            if (velocity.y < 0)
            {
                velocity.y = -2f; 
            }
        }
        else
        {
            // Si no está en el suelo, acumulamos el tiempo que lleva flotando
            airTimer += Time.deltaTime;
        }

        // 2. CONTROL DEL AGACHADO (Tecla C)
        if (Input.GetKeyDown(KeyCode.C))
        {
            isCrouched = !isCrouched; 
            
            if (isCrouched)
            {
                controller.height = crouchHeight;
                controller.center = new Vector3(0f, crouchHeight / 2f, 0f);
                targetCamY = crouchCamY;                 
                targetForwardOffset = crouchForwardOffset; 
            }
            else
            {
                controller.height = standingHeight;
                controller.center = new Vector3(0f, standingHeight / 2f, 0f);
                targetCamY = standingCamY;                 
                targetForwardOffset = standingForwardOffset; 
            }
        }

        // INTERPOLACIÓN SUAVE DE LA CÁMARA
        if (playerCamera != null)
        {
            float newCamY = Mathf.Lerp(playerCamera.localPosition.y, targetCamY, Time.deltaTime * crouchSmoothTime);
            float newCamZ = Mathf.Lerp(playerCamera.localPosition.z, targetForwardOffset, Time.deltaTime * crouchSmoothTime);
            playerCamera.localPosition = new Vector3(playerCamera.localPosition.x, newCamY, newCamZ);
        }

        // 3. SALTO
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouched)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }

        // 4. DETECCIÓN DE BOTONES (WASD)
        Vector3 direction = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) direction.z += 1f;
        if (Input.GetKey(KeyCode.S)) direction.z -= 1f;
        if (Input.GetKey(KeyCode.A)) direction.x -= 1f;
        if (Input.GetKey(KeyCode.D)) direction.x += 1f;

        float animSpeed = 0f; 
        Vector3 horizontalMovement = Vector3.zero;

        // Estados de movimiento para el audio
        bool isMoving = direction.magnitude > 0f;
        bool isSprinting = isMoving && !isCrouched && Input.GetKey(KeyCode.LeftShift);
        bool isWalking = isMoving && !isCrouched && !Input.GetKey(KeyCode.LeftShift);

        // 5. MOVIMIENTO HORIZONTAL
        if (isMoving)
        {
            direction.Normalize();
            float currentSpeed = walkSpeed;
            animSpeed = 1f; 

            if (isCrouched)
            {
                currentSpeed = crouchSpeed; 
            }
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed = sprintSpeed;
                animSpeed = 2f; 
            }

            horizontalMovement = (transform.right * direction.x + transform.forward * direction.z) * currentSpeed;

            // GESTIÓN DE AUDIO DINÁMICO
            if (isGrounded && (isWalking || isSprinting))
            {
                AudioClip targetClip = isSprinting ? sprintSound : walkSound;
                float targetInterval = isSprinting ? sprintStepInterval : walkStepInterval;
                float targetVolume = isSprinting ? sprintVolume : walkVolume;
                float targetDistance = isSprinting ? sprintMaxDistance : walkMaxDistance;

                footstepTimer -= Time.deltaTime;

                if (footstepTimer <= 0f)
                {
                    if (audioSource != null && targetClip != null)
                    {
                        audioSource.clip = targetClip;
                        audioSource.volume = targetVolume;
                        audioSource.maxDistance = targetDistance;
                        audioSource.Play();
                    }
                    footstepTimer = targetInterval;
                }
            }
            else
            {
                DetenerAudioPasos();
            }
        }
        else
        {
            DetenerAudioPasos();
        }

        // 6. APLICAR GRAVEDAD
        velocity.y += gravity * Time.deltaTime;

        // 7. MOVIMIENTO UNIFICADO
        Vector3 finalMovement = horizontalMovement + velocity;
        controller.Move(finalMovement * Time.deltaTime);

        // 8. ENVIAR VALORES AL ANIMATOR
        if (animator != null)
        {
            // FILTRO PARA EL ANIMATOR: Solo le diremos que "no está en el suelo" si lleva flotando 
            // más tiempo que nuestro umbral (miniBumpThreshold). Así evitamos animaciones de caída en piedras.
            bool sendGroundedToAnimator = isGrounded || (airTimer < miniBumpThreshold);
            
            animator.SetBool("isGrounded", sendGroundedToAnimator);
            animator.SetBool("isCrouched", isCrouched);
            
            float currentAnimSpeed = animator.GetFloat("Speed");
            animator.SetFloat("Speed", Mathf.MoveTowards(currentAnimSpeed, animSpeed, Time.deltaTime * 5f));
        }

        // Guardamos el estado del suelo corregido para el siguiente frame
        wasGrounded = isGrounded;
    }

    void DetenerAudioPasos()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            if (audioSource.clip == walkSound || audioSource.clip == sprintSound)
            {
                audioSource.Stop();
            }
        }
        footstepTimer = 0f; 
    }
}