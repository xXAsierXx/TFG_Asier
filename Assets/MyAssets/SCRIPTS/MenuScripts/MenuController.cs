using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement; // Necesario si quieres referencias de escenas

public class MenuController : MonoBehaviour
{
    [Header("Paneles de los Menús")]
    public GameObject firstMenu;
    public GameObject joinMenu;

    [Header("Elementos del Segundo Menú")]
    public Button hostButton;
    public Button clientButton;
    public TMP_InputField ipInputField;
    public GameObject finalJoinButton;
    public NetworkManager networkManager;

    [Header("Nombre de la Escena de Selección")]
    [SerializeField] private string mapSelectionSceneName = "Seleccion Mapa";

    private bool isHostSelected = false;
    private bool isClientSelected = false;

    void Start()
    {
        MostrarFirstMenu();
        ipInputField.onValueChanged.AddListener(AlEscribirIP);
    }

    // --- FUNCIONES PARA CAMBIAR DE MENÚ ---

    public void MostrarFirstMenu()
    {
        firstMenu.SetActive(true);
        joinMenu.SetActive(false);
    }

    public void MostrarJoinMenu()
    {
        Debug.Log("¡El botón Join ha sido pulsado!");
        firstMenu.SetActive(false);
        joinMenu.SetActive(true);

        ReiniciarJoinMenu();
    }

    private void ReiniciarJoinMenu()
    {
        isHostSelected = false;
        isClientSelected = false;

        hostButton.interactable = true;
        clientButton.interactable = true;

        ipInputField.gameObject.SetActive(false);
        ipInputField.text = "";
        finalJoinButton.SetActive(false);
    }

    // --- FUNCIONES DE LOS BOTONES HOST Y CLIENT ---

    public void SeleccionarHost()
    {
        isHostSelected = true;
        isClientSelected = false;

        hostButton.interactable = false;
        clientButton.interactable = true;

        ipInputField.gameObject.SetActive(false);
        finalJoinButton.SetActive(true);
    }

    public void SeleccionarClient()
    {
        isHostSelected = false;
        isClientSelected = true;

        hostButton.interactable = true;
        clientButton.interactable = false;

        ipInputField.gameObject.SetActive(true);
        ComprobarBotonJoin(ipInputField.text);
    }

    // --- LÓGICA DE LA CAJA DE TEXTO ---

    private void AlEscribirIP(string texto)
    {
        if (isClientSelected)
        {
            ComprobarBotonJoin(texto);
        }
    }

    private void ComprobarBotonJoin(string texto)
    {
        if (!string.IsNullOrEmpty(texto))
        {
            finalJoinButton.SetActive(true);
        }
        else
        {
            finalJoinButton.SetActive(false);
        }
    }

    // --- CONEXIÓN Y CAMBIO DE ESCENA ---

    public void BotonJoinFinalPulsado()
    {
        if (isHostSelected)
        {
            // 1. EL HOST INICIA EL SERVIDOR Y CREA LA PARTIDA
            if (networkManager.StartHost())
            {
                Debug.Log("Host iniciado con éxito. Cargando escena de Selección de Mapa...");
                
                // 2. EL HOST CARGA LA ESCENA MEDIANTE EL SCENEMANAGER DE NETCODE
                // Esto hará que el Host cambie de escena y que cualquier cliente que se conecte sea teletransportado a ella.
                NetworkManager.Singleton.SceneManager.LoadScene(mapSelectionSceneName, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("Error al iniciar el Host.");
            }
        }
        else if (isClientSelected)
        {
            // EL CLIENTE CONFIGURA LA IP Y SE UNE
            var transport = networkManager.GetComponent<UnityTransport>();
            
            // Asignamos la IP introducida
            string ipAddress = string.IsNullOrWhiteSpace(ipInputField.text) ? "127.0.0.1" : ipInputField.text;
            transport.ConnectionData.Address = ipAddress;
            
            Debug.Log("Intentando conectar como CLIENTE a: " + ipAddress);
            networkManager.StartClient();

            // NOTA: No hace falta llamar a LoadScene aquí. 
            // Netcode sincroniza automáticamente la escena del Host en cuanto el Cliente se conecta.
        }
    }

    public void SalirDelJuego()
    {
        Debug.Log("Cerrando aplicación...");
        Application.Quit();
    }
}