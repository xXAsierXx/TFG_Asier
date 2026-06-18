using UnityEngine;
using UnityEngine.UI;
using TMPro; // Necesario para la caja de texto de TextMeshPro
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // <--- NECESARIO PARA CAMBIAR LA IP

public class MenuController : MonoBehaviour
{
    [Header("Paneles de los Menús")]
    public GameObject firstMenu; // Tu objeto "FirstMenu"
    public GameObject joinMenu;  // Tu objeto "JoinMenu"

    [Header("Elementos del Segundo Menú")]
    public Button hostButton;
    public Button clientButton;
    public TMP_InputField ipInputField; // La caja de texto (asegúrate de que sea TMP_InputField)
    public GameObject finalJoinButton;  // El botón "JOIN" del segundo menú (lo tratamos como GameObject para ocultarlo/mostrarlo)
    public NetworkManager networkManager; // Objeto NetworkManager

    // Variables internas para saber qué hemos elegido
    private bool isHostSelected = false;
    private bool isClientSelected = false;

    void Start()
    {
        // Al darle al Play, nos aseguramos de que empiece en el menú 1
        MostrarFirstMenu();

        // Le decimos a la caja de texto que nos avise cada vez que escribamos una letra
        ipInputField.onValueChanged.AddListener(AlEscribirIP);
    }

    // --- FUNCIONES PARA CAMBIAR DE MENÚ ---

    public void MostrarFirstMenu()
    {
        firstMenu.SetActive(true);
        joinMenu.SetActive(false);
    }

    public void MostrarJoinMenu() // Esta irá en el botón JOIN del primer menú
    {
        Debug.Log("¡El botón Join ha sido pulsado!");
        firstMenu.SetActive(false);
        joinMenu.SetActive(true);

        // Reiniciamos el menú 2 para que empiece limpio
        ReiniciarJoinMenu();
    }

    private void ReiniciarJoinMenu()
    {
        isHostSelected = false;
        isClientSelected = false;

        // Dejamos ambos botones "clicables"
        hostButton.interactable = true;
        clientButton.interactable = true;

        // Ocultamos la caja de texto y el botón JOIN final
        ipInputField.gameObject.SetActive(false);
        ipInputField.text = ""; // Limpiamos la IP por si había algo escrito antes
        finalJoinButton.SetActive(false);
    }

    // --- FUNCIONES DE LOS BOTONES HOST Y CLIENT ---

    public void SeleccionarHost() // Esta irá en el botón HOST
    {
        isHostSelected = true;
        isClientSelected = false;

        // Para que no estén los dos seleccionados a la vez, desactivamos la interacción del que hemos pulsado
        // (Visualmente se verá más oscuro o presionado, indicando que es la opción elegida)
        hostButton.interactable = false;
        clientButton.interactable = true;

        // Lógica visual:
        ipInputField.gameObject.SetActive(false); // Ocultamos la IP porque el Host no la necesita
        finalJoinButton.SetActive(true);          // Mostramos el botón JOIN directamente
    }

    public void SeleccionarClient() // Esta irá en el botón CLIENT
    {
        isHostSelected = false;
        isClientSelected = true;

        // Bloqueamos este botón y liberamos el de Host
        hostButton.interactable = true;
        clientButton.interactable = false;

        // Lógica visual:
        ipInputField.gameObject.SetActive(true); // Aparece la caja para meter la IP
        
        // Comprobamos si ya hay una IP escrita para mostrar u ocultar el botón JOIN
        ComprobarBotonJoin(ipInputField.text);
    }

    // --- LÓGICA DE LA CAJA DE TEXTO ---

    private void AlEscribirIP(string texto)
    {
        // Si estamos en modo Cliente, comprobamos la IP cada vez que el jugador teclea algo
        if (isClientSelected)
        {
            ComprobarBotonJoin(texto);
        }
    }

    private void ComprobarBotonJoin(string texto)
    {
        // Si el texto NO está vacío, mostramos el botón JOIN. Si está vacío, lo ocultamos.
        if (!string.IsNullOrEmpty(texto))
        {
            finalJoinButton.SetActive(true);
        }
        else
        {
            finalJoinButton.SetActive(false);
        }
    }

    public void BotonJoinFinalPulsado() // Boton que lleva a la partida
    {
        if (isHostSelected)
        {
            // EL HOST INICIA EL SERVIDOR
            networkManager.StartHost();
            Debug.Log("Partida creada como HOST");
        }
        else if (isClientSelected)
        {
            // EL CLIENTE CONFIGURA LA IP Y SE UNE
            var transport = networkManager.GetComponent<UnityTransport>();
            transport.ConnectionData.Address = ipInputField.text; // Toma la IP del campo de texto
            
            networkManager.StartClient();
            Debug.Log("Intentando conectar como CLIENTE a: " + ipInputField.text);
        }
    }

    // --- BOTÓN PARA SALIR DEL JUEGO ---

    public void SalirDelJuego()
    {
        Debug.Log("Cerrando aplicación...");
        Application.Quit();
    }
}