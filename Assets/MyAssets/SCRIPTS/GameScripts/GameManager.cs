using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// Asignación de roles y tipos de entidad
public enum PlayerRole 
{
    Atrapado,   // Se esconde
    Atrapador   // Persigue
}

public enum PlayerType
{
    Humano,
    IA
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Prefabs")]
    public GameObject humanPrefab; // Prefab de jugador humano (NetworkObject)
    public GameObject aiPrefab;    // Prefab de IA (NetworkObject)

    [Header("Configuración de Tiempos")]
    public float tiempoEscondite = 15f; // Tiempo para esconderse al inicio de cada ronda
    public float tiempoRonda = 120f;    // Tiempo de caza de cada ronda

    // Variables de red sincronizadas para UI y clientes
    public NetworkVariable<int> rondaActual = new NetworkVariable<int>(1);
    public NetworkVariable<float> tiempoRestante = new NetworkVariable<float>(0f);
    public NetworkVariable<bool> enFaseEscondite = new NetworkVariable<bool>(true);

    private List<NetworkObject> spawnedPlayers = new List<NetworkObject>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // Solo el Servidor/Host gestiona la aparición y el bucle de partida
        if (IsServer)
        {
            IniciarPartida();
        }
    }

    private void IniciarPartida()
    {
        rondaActual.Value = 1;
        StartCoroutine(EsperarYSpawnearRonda1());
    }

    private IEnumerator EsperarYSpawnearRonda1()
    {
        // Esperamos un frame para garantizar que Awake() de SpawnManager se ha ejecutado
        yield return null; 

        if (SpawnManager.Instance == null)
        {
            Debug.LogError("¡ERROR! No se encontró el SpawnManager en la escena.");
            yield break;
        }

        SpawnearEntidadesRonda1();
        StartCoroutine(BucleRonda());
    }

    // --- RONDA 1: Humanos (Atrapados) vs IAs (Atrapadores) ---
    private void SpawnearEntidadesRonda1()
    {
        if (humanPrefab == null || aiPrefab == null)
        {
            Debug.LogError("¡Faltan prefabs por asignar en el GameManager!");
            return;
        }

        DespawnearEntidades();

        int numHumanos = NetworkManager.Singleton.ConnectedClientsList.Count;

        // 1. Instanciar Humanos conectados como ATRAPADOS (esAtrapador = false)
        int indexHumano = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition(false, indexHumano);
            GameObject playerObj = Instantiate(humanPrefab, spawnPos, Quaternion.identity);
            
            NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
            netObj.SpawnWithOwnership(client.ClientId);
            spawnedPlayers.Add(netObj);

            indexHumano++;
        }

        // 2. Instanciar el mismo número de IAs como ATRAPADORES (esAtrapador = true)
        for (int i = 0; i < numHumanos; i++)
        {
            Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition(true, i);
            GameObject aiObj = Instantiate(aiPrefab, spawnPos, Quaternion.identity);
            
            NetworkObject netObj = aiObj.GetComponent<NetworkObject>();
            netObj.Spawn(); // Pertenecen al Servidor
            spawnedPlayers.Add(netObj);
        }

        Debug.Log($"[Ronda 1 Adaptativa] Spawneados: {numHumanos} Humano(s) Atrapado(s) y {numHumanos} IA(s) Atrapadora(s).");
    }

    // --- RONDA 2: Humanos (Atrapadores) vs IAs (Atrapadas) ---
    private void SpawnearEntidadesRonda2()
    {
        if (humanPrefab == null || aiPrefab == null) return;

        DespawnearEntidades();

        int numHumanos = NetworkManager.Singleton.ConnectedClientsList.Count;

        // 1. Instanciar Humanos conectados como ATRAPADORES (esAtrapador = true)
        int indexHumano = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition(true, indexHumano);
            GameObject playerObj = Instantiate(humanPrefab, spawnPos, Quaternion.identity);
            
            NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
            netObj.SpawnWithOwnership(client.ClientId);
            spawnedPlayers.Add(netObj);

            indexHumano++;
        }

        // 2. Instanciar IAs como ATRAPADAS (esAtrapador = false)
        for (int i = 0; i < numHumanos; i++)
        {
            Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition(false, i);
            GameObject aiObj = Instantiate(aiPrefab, spawnPos, Quaternion.identity);
            
            NetworkObject netObj = aiObj.GetComponent<NetworkObject>();
            netObj.Spawn();
            spawnedPlayers.Add(netObj);
        }

        Debug.Log($"[Ronda 2 Adaptativa] Roles invertidos: {numHumanos} Humano(s) Atrapador(es) vs {numHumanos} IA(s) Atrapada(s).");
    }

    private void DespawnearEntidades()
    {
        foreach (var player in spawnedPlayers)
        {
            if (player != null && player.IsSpawned)
            {
                player.Despawn();
            }
        }
        spawnedPlayers.Clear();
    }

    // --- BUCLE DE CONTROL DE TIEMPOS Y RONDAS ---
    private IEnumerator BucleRonda()
    {
        // FASE 1: TIEMPO DE ESCONDITE
        enFaseEscondite.Value = true;
        tiempoRestante.Value = tiempoEscondite;

        while (tiempoRestante.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            tiempoRestante.Value -= 1f;
        }

        // FASE 2: TIEMPO DE CAZA
        enFaseEscondite.Value = false;
        tiempoRestante.Value = tiempoRonda;

        while (tiempoRestante.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            tiempoRestante.Value -= 1f;
        }

        Debug.Log("Fin de la Ronda " + rondaActual.Value);

        // Transición de Ronda
        if (rondaActual.Value == 1)
        {
            SiguienteRonda();
        }
        else
        {
            Debug.Log("--- FIN DE LA PARTIDA ---");
            DespawnearEntidades();
        }
    }

    private void SiguienteRonda()
    {
        rondaActual.Value = 2;
        SpawnearEntidadesRonda2();
        StartCoroutine(BucleRonda()); // Inicia el temporizador para la Ronda 2
    }
}