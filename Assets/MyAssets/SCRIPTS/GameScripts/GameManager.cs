using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

//Esto vale para asignar el rol de cada jugador.
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
    public GameObject humanPrefab; // Tu prefab de jugador humano (NetworkObject)
    public GameObject aiPrefab;    // Tu prefab de IA (NetworkObject)

    [Header("Configuración de Tiempos")]
    public float tiempoEscondite = 15f; // Tiempo para esconderse al inicio de la ronda
    public float tiempoRonda = 120f;     // Tiempo de caza de la ronda

    // Variables de red sincronizadas para UI
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
        StartCoroutine(EsperarYSpawnear());
    }

    private IEnumerator EsperarYSpawnear()
    {
        // Esperamos un frame para garantizar que el Awake() de SpawnManager ya se ha ejecutado
        yield return null; 

        if (SpawnManager.Instance == null)
        {
            Debug.LogError("¡ERROR! No se encontró el SpawnManager en la escena.");
            yield break;
        }

        SpawnearEntidadesRonda1();
        StartCoroutine(BucleRonda());
    }

    //Codigo hecho para que solo spawnee tantas IAs como humanos haya en partida.
    private void SpawnearEntidadesRonda1()
    {
        if (humanPrefab == null || aiPrefab == null)
        {
            Debug.LogError("¡Faltan prefabs por asignar en el GameManager!");
            return;
        }

        DespawnearEntidades();

        // Contamos los clientes humanos conectados actualmente
        int numHumanos = NetworkManager.Singleton.ConnectedClientsList.Count;

        // 1. Instanciar a todos los Jugadores Humanos conectados
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

        // 2. Instanciar EXACTAMENTE el mismo número de IAs que de Humanos
        for (int i = 0; i < numHumanos; i++)
        {
            Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition(true, i);
            GameObject aiObj = Instantiate(aiPrefab, spawnPos, Quaternion.identity);
        
            NetworkObject netObj = aiObj.GetComponent<NetworkObject>();
            netObj.Spawn(); // Pertenecen al Host/Servidor
            spawnedPlayers.Add(netObj);
        }

        Debug.Log($"[Partida Adaptativa] Spawneados: {numHumanos} Humano(s) y {numHumanos} IA(s).");
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

    private IEnumerator BucleRonda()
    {
        // --- FASE 1: TIEMPO DE ESCONDITE ---
        enFaseEscondite.Value = true;
        tiempoRestante.Value = tiempoEscondite;

        while (tiempoRestante.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            tiempoRestante.Value -= 1f;
        }

        // --- FASE 2: TIEMPO DE CAZA ---
        enFaseEscondite.Value = false;
        tiempoRestante.Value = tiempoRonda;

        while (tiempoRestante.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            tiempoRestante.Value -= 1f;
        }

        Debug.Log("Fin de la Ronda " + rondaActual.Value);

        // Si estamos en la ronda 1, pasar a la ronda 2
        if (rondaActual.Value == 1)
        {
            SiguienteRonda();
        }
        else
        {
            Debug.Log("FIN DE LA PARTIDA");
        }
    }

    private void SiguienteRonda()
    {
        rondaActual.Value = 2;
        // En la Ronda 2 invertiremos la lógica de instanciación/posiciones
        // (Humano = Atrapador | IA = Atrapado)
        DespawnearEntidades();
        // Aquí llamaremos a SpawnearEntidadesRonda2()
        StartCoroutine(BucleRonda());
    }
}