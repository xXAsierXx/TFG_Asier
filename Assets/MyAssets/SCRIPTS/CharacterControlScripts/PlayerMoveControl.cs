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

    [Header("Configuración de Agachado (Altura Y)")]
    public Transform playerCamera;     
    public float standingHeight = 2f;  
    public float crouchHeight = 1f;    
    public float standingCamY = 1.6f;  
    public float crouchCamY = 0.8f;    
    public float crouchSmoothTime = 8f; 
    
    [Header("Ajustes de Offset Frontal (Distancia Z)")]
    public float standingForwardOffset = 0.0f; 
    public float crouchForwardOffset = 0.25f; 

    [Header("Configuración de Audio")]
    public AudioSource audioSource;       // El componente altavoz del Player
    
    [Space(5)]
    public AudioClip walkSound;          // Tu archivo "Walk audio"
    [Tooltip("Tiempo en segundos entre cada pisada al caminar.")]
    public float walkStepInterval = 0.5f; 
    [Range(0f, 1f)] public float walkVolume = 0.4f; 
    public float walkMaxDistance = 10f;             
    
    [Space(5)]
    public AudioClip sprintSound;        // Tu archivo de audio para correr
    [Tooltip("Tiempo en segundos entre cada pisada al correr.")]
    public float sprintStepInterval = 0.3f; 
    [Range(0f, 1f)] public float sprintVolume = 0.9f; 
    public float sprintMaxDistance = 25f;             

    [Space(5)]
    [Header("Sonido de Aterrizaje")]
    public AudioClip landSound;          // Tu archivo de audio para cuando cae al suelo
    [Range(0f, 1f)] public float landVolume = 0.7f;   // Volumen del impacto
    public float landMaxDistance = 20f;               // Distancia en metros que alcanza el sonido de caída
    [Tooltip("Velocidad de caída mínima para activar el sonido. Evita que suene al bajar rampas.")]
    public float landVelocityThreshold = -4f; 

    private float footstepTimer = 0f;     // Temporizador interno
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
        // 1. GESTIÓN DE SUELO, GRAVEDAD Y CAÍDAS
        if (controller.isGrounded)
        {
            // DETECCIÓN DE ATERRIZAJE: Si en el frame anterior estaba en el aire y ahora toca el suelo
            if (!wasGrounded && velocity.y < landVelocityThreshold)
            {
                if (audioSource != null && landSound != null)
                {
                    // Configuramos los parámetros físicos del impacto y lo reproducimos
                    audioSource.clip = landSound;
                    audioSource.volume = landVolume;
                    audioSource.maxDistance = landMaxDistance;
                    audioSource.Play();
                }

                // Colocamos un margen en el temporizador para que no se pisen 
                // el sonido del impacto de caída y el primer paso inmediato en el suelo
                footstepTimer = Input.GetKey(KeyCode.LeftShift) ? sprintStepInterval : walkStepInterval;
            }

            if (velocity.y < 0)
            {
                velocity.y = -2f; 
            }
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
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded && !isCrouched)
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

            // GESTIÓN DE AUDIO DINÁMICO (Caminar o Correr en el suelo)
            if (controller.isGrounded && (isWalking || isSprinting))
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
                // Si está en el aire (saltando/cayendo), no deben sonar pasos
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
            animator.SetBool("isGrounded", controller.isGrounded);
            animator.SetBool("isCrouched", isCrouched);
            
            float currentAnimSpeed = animator.GetFloat("Speed");
            animator.SetFloat("Speed", Mathf.MoveTowards(currentAnimSpeed, animSpeed, Time.deltaTime * 5f));
        }

        // Al final del Update, recordamos si estábamos en el suelo para el próximo frame
        wasGrounded = controller.isGrounded;
    }

    void DetenerAudioPasos()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            // Solo detenemos si lo que está sonando es el audio de caminar o correr 
            // (así permitimos que el sonido de aterrizaje termine de reproducirse entero si nos paramos)
            if (audioSource.clip == walkSound || audioSource.clip == sprintSound)
            {
                audioSource.Stop();
            }
        }
        footstepTimer = 0f; 
    }
}