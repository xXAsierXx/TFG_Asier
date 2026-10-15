using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona la pantalla de selección y votación de mapas.
/// Selecciona 2 mapas aleatorios de un total de 3 disponibles, sincroniza los votos 
/// y el temporizador en tiempo real entre todos los jugadores conectados.
/// </summary>
public class MapSelectionManager : NetworkBehaviour
{
    // ESTRUCTURA DE DATOS: Permite definir la información de cada mapa en el Inspector de Unity
    [System.Serializable]
    public struct MapData
    {
        public string mapName;          // Nombre visible del mapa en el menú (ej: "Mapa Cyberpunk")
        public string sceneName;        // Nombre exacto de la escena en Build Settings
        public Sprite mapPreview;       // Imagen/Miniatura del mapa
    }

    [Header("Lista de Mapas Totales (Los 3 mapas del juego)")]
    public MapData[] allMaps = new MapData[3];

    [Header("UI - Mapa 1")]
    public Image map1Image;
    public TMP_Text map1NameText;
    public TMP_Text map1VotesText;
    public Button map1Button;

    [Header("UI - Mapa 2")]
    public Image map2Image;
    public TMP_Text map2NameText;
    public TMP_Text map2VotesText;
    public Button map2Button;

    [Header("UI - Temporizador")]
    public TMP_Text timerText;

    [Header("Configuración de Tiempo")]
    public float votingTime = 15f; // Tiempo en segundos para la votación

    // --- VARIABLES DE RED SINCRONIZADAS (NetworkVariables) ---
    // Guardan el índice del mapa elegido dentro del array 'allMaps' (-1 significa no asignado)
    private NetworkVariable<int> map1Index = new NetworkVariable<int>(-1);
    private NetworkVariable<int> map2Index = new NetworkVariable<int>(-1);

    // Contadores de votos sincronizados
    private NetworkVariable<int> map1Votes = new NetworkVariable<int>(0);
    private NetworkVariable<int> map2Votes = new NetworkVariable<int>(0);

    // Tiempo restante sincronizado
    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>(15f);

    // Diccionario exclusivo del servidor: guarda qué opción (1 o 2) ha votado cada jugador según su ID
    private Dictionary<ulong, int> playerVotes = new Dictionary<ulong, int>();
    
    // Evita que se ordene cargar la escena más de una vez cuando se acaba el tiempo
    private bool gameStarting = false;

    /// <summary>
    /// Se ejecuta automáticamente cuando el objeto se activa en la red (tanto en Host como en Clientes).
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // 1. Nos suscribimos a los cambios de las variables de red para actualizar la UI en vivo
        map1Votes.OnValueChanged += (prev, current) => ActualizarTextosVotos();
        map2Votes.OnValueChanged += (prev, current) => ActualizarTextosVotos();
        timeRemaining.OnValueChanged += (prev, current) => ActualizarTimerUI(current);

        // Cuando la red elija los mapas, actualizamos su imagen y nombre en la interfaz de todos
        map1Index.OnValueChanged += (prev, current) => ActualizarOpcionMapaUI(1, current);
        map2Index.OnValueChanged += (prev, current) => ActualizarOpcionMapaUI(2, current);

        // 2. Lógica ejecutada ÚNICAMENTE por el HOST/SERVIDOR al iniciar la sala
        if (IsServer)
        {
            timeRemaining.Value = votingTime;
            ElegirDosMapasAleatorios();
        }

        // 3. Si un cliente entra cuando las variables ya tienen valor, actualiza su interfaz
        if (map1Index.Value != -1) ActualizarOpcionMapaUI(1, map1Index.Value);
        if (map2Index.Value != -1) ActualizarOpcionMapaUI(2, map2Index.Value);
        ActualizarTextosVotos();
    }

    private void Update()
    {
        // Solo el Servidor se encarga de reducir la cuenta atrás del temporizador
        if (!IsServer || gameStarting) return;

        timeRemaining.Value -= Time.deltaTime;

        // Cuando el contador llega a cero, finaliza la votación y se carga el mapa ganador
        if (timeRemaining.Value <= 0f)
        {
            timeRemaining.Value = 0f;
            gameStarting = true;
            CargarMapaGanador();
        }

 
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Clic detectado en pantalla");
        }

    }

    /// <summary>
    /// Selecciona aleatoriamente 2 índices únicos entre 0, 1 y 2 (los 3 mapas disponibles).
    /// </summary>
    private void ElegirDosMapasAleatorios()
    {
        List<int> indices = new List<int> { 0, 1, 2 };
        
        // Seleccionamos el primer mapa y lo sacamos de la lista
        int firstChoice = indices[Random.Range(0, indices.Count)];
        indices.Remove(firstChoice);
        
        // Seleccionamos el segundo mapa entre los restantes
        int secondChoice = indices[Random.Range(0, indices.Count)];

        // Al cambiar .Value, Netcode propaga automáticamente el cambio a todos los clientes
        map1Index.Value = firstChoice;
        map2Index.Value = secondChoice;
    }

    /// <summary>
    /// Muestra la imagen y el nombre del mapa correspondiente en la interfaz gráfica.
    /// </summary>
    private void ActualizarOpcionMapaUI(int optionNumber, int mapDataIndex)
    {
        if (mapDataIndex < 0 || mapDataIndex >= allMaps.Length) return;

        MapData data = allMaps[mapDataIndex];

        if (optionNumber == 1)
        {
            if (map1Image != null) map1Image.sprite = data.mapPreview;
            if (map1NameText != null) map1NameText.text = data.mapName;
        }
        else if (optionNumber == 2)
        {
            if (map2Image != null) map2Image.sprite = data.mapPreview;
            if (map2NameText != null) map2NameText.text = data.mapName;
        }
    }

    /// <summary>
    /// Refresca los contadores de votos en la pantalla de todos los jugadores.
    /// </summary>
    private void ActualizarTextosVotos()
    {
        if (map1VotesText != null) map1VotesText.text = $"Votos: {map1Votes.Value}";
        if (map2VotesText != null) map2VotesText.text = $"Votos: {map2Votes.Value}";
    }

    /// <summary>
    /// Muestra el tiempo restante redondeado en la pantalla.
    /// </summary>
    private void ActualizarTimerUI(float newTime)
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(newTime).ToString();
        }
    }

    // --- MÉTODOS PÚBLICOS ASIGNADOS A LOS BOTONES DE LA UI ---

    public void VotarMapa1()
    {
        Debug.Log("¡Botón Mapa 1 pulsado localmente!");
        VotarServidorRpc(1);
    }

    public void VotarMapa2()
    {
        Debug.Log("¡Botón Mapa 2 pulsado localmente!");
        VotarServidorRpc(2);
    }

    /// <summary>
    /// RPC que se ejecuta en el Servidor cuando un jugador pulsa un botón.
    /// [Rpc(SendTo.Server)] indica que la llamada viaja del Cliente al Servidor.
    /// InvokePermission.Everyone permite que cualquier jugador conectado vote sin ser el dueño del objeto.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void VotarServidorRpc(int optionSelected, RpcParams rpcParams = default)
    {
        // Obtenemos la ID única del jugador que ha pulsado el botón
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Comprobamos si el jugador ya había votado antes para cambiar su voto
        if (playerVotes.ContainsKey(senderId))
        {
            int previousVote = playerVotes[senderId];
            if (previousVote == 1) map1Votes.Value--;
            else if (previousVote == 2) map2Votes.Value--;
            
            playerVotes[senderId] = optionSelected;
        }
        else
        {
            // Registramos el voto inicial del jugador
            playerVotes.Add(senderId, optionSelected);
        }

        // Sumamos el voto a la opción seleccionada
        if (optionSelected == 1) map1Votes.Value++;
        else if (optionSelected == 2) map2Votes.Value++;
    }

    /// <summary>
    /// Evalúa el mapa ganador y utiliza la carga de escenas sincronizada de Netcode.
    /// </summary>
    private void CargarMapaGanador()
    {
        int winningMapDataIndex;

        // Si hay empate o la Opción 1 tiene más votos, gana la Opción 1
        if (map1Votes.Value >= map2Votes.Value)
        {
            winningMapDataIndex = map1Index.Value;
        }
        else
        {
            winningMapDataIndex = map2Index.Value;
        }

        string sceneToLoad = allMaps[winningMapDataIndex].sceneName;
        Debug.Log($"¡Votación finalizada! Cargando mapa ganador: {allMaps[winningMapDataIndex].mapName} ({sceneToLoad})");

        // El Host ordena a Netcode cargar la escena seleccionada para TODOS los clientes conectados
        NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }
}