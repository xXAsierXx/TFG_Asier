using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MapSelectionManager : NetworkBehaviour
{
    [System.Serializable]
    public struct MapData
    {
        public string mapName;          // Nombre que se mostrará en pantalla
        public string sceneName;        // Nombre exacto de la escena en Build Settings
        public Sprite mapPreview;       // Imagen del mapa
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
    public float votingTime = 15f;

    // --- VARIABLES RED SINCRONIZADAS ---
    // Guardan el índice del mapa elegido del array allMaps (0, 1 o 2)
    private NetworkVariable<int> map1Index = new NetworkVariable<int>(-1);
    private NetworkVariable<int> map2Index = new NetworkVariable<int>(-1);

    // Contadores de votos
    private NetworkVariable<int> map1Votes = new NetworkVariable<int>(0);
    private NetworkVariable<int> map2Votes = new NetworkVariable<int>(0);

    // Temporizador
    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>(15f);

    // Diccionario interno del servidor para rastrear qué ha votado cada jugador (ClientId)
    private Dictionary<ulong, int> playerVotes = new Dictionary<ulong, int>();
    private bool gameStarting = false;

    public override void OnNetworkSpawn()
    {
        // 1. Suscribir eventos de red para actualizar la UI en TODOS los clientes automáticamente
        map1Votes.OnValueChanged += (prev, current) => ActualizarTextosVotos();
        map2Votes.OnValueChanged += (prev, current) => ActualizarTextosVotos();
        timeRemaining.OnValueChanged += (prev, current) => ActualizarTimerUI(current);

        map1Index.OnValueChanged += (prev, current) => ActualizarOpcionMapaUI(1, current);
        map2Index.OnValueChanged += (prev, current) => ActualizarOpcionMapaUI(2, current);

        // 2. Si soy el HOST / SERVIDOR, elijo 2 mapas aleatorios de los 3 disponibles
        if (IsServer)
        {
            timeRemaining.Value = votingTime;
            ElegirDosMapasAleatorios();
        }

        // Si los valores ya estaban asignados (por si un cliente entra tarde), actualizamos UI
        if (map1Index.Value != -1) ActualizarOpcionMapaUI(1, map1Index.Value);
        if (map2Index.Value != -1) ActualizarOpcionMapaUI(2, map2Index.Value);
        ActualizarTextosVotos();
    }

    private void Update()
    {
        // Solo el Host ejecuta la cuenta atrás
        if (!IsServer || gameStarting) return;

        timeRemaining.Value -= Time.deltaTime;

        if (timeRemaining.Value <= 0f)
        {
            timeRemaining.Value = 0f;
            gameStarting = true;
            CargarMapaGanador();
        }
    }

    // --- SELECCIÓN ALEATORIA DE MAPAS (SOLO HOST) ---
    private void ElegirDosMapasAleatorios()
    {
        List<int> indices = new List<int> { 0, 1, 2 };
        
        int firstChoice = indices[Random.Range(0, indices.Count)];
        indices.Remove(firstChoice);
        
        int secondChoice = indices[Random.Range(0, indices.Count)];

        map1Index.Value = firstChoice;
        map2Index.Value = secondChoice;
    }

    // --- ACTUALIZACIÓN DE UI EN CLIENTES ---
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

    private void ActualizarTextosVotos()
    {
        if (map1VotesText != null) map1VotesText.text = $"Votos: {map1Votes.Value}";
        if (map2VotesText != null) map2VotesText.text = $"Votos: {map2Votes.Value}";
    }

    private void ActualizarTimerUI(float newTime)
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(newTime).ToString();
        }
    }

    // --- MÉTODOS DE VOTACIÓN (PULSACIÓN DE BOTONES) ---

    public void VotarMapa1()
    {
        VotarServidorServerRpc(1);
    }

    public void VotarMapa2()
    {
        VotarServidorServerRpc(2);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)] //Cualquiera aunque no sea su dueño (Ownership) puede enviar su votacion
    private void VotarServidorRpc(int optionSelected, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Si el jugador ya había votado antes, le restamos el voto anterior
        if (playerVotes.ContainsKey(senderId))
        {
            int previousVote = playerVotes[senderId];
            if (previousVote == 1) map1Votes.Value--;
            else if (previousVote == 2) map2Votes.Value--;
        
            playerVotes[senderId] = optionSelected;
        }
        else
        {
            playerVotes.Add(senderId, optionSelected);
        }

        // Sumar el voto a la nueva opción
        if (optionSelected == 1) map1Votes.Value++;
        else if (optionSelected == 2) map2Votes.Value++;
    }

    // --- FIN DE VOTACIÓN Y CARGA DE LA PARTIDA ---
    private void CargarMapaGanador()
    {
        int winningMapDataIndex;

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

        // El Host carga la escena seleccionada para todos
        NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }
}