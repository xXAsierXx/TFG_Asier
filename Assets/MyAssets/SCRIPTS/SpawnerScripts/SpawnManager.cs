using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [Header("Puntos de Aparición")]
    public Transform[] spawnAtrapados;   // 3 puntos para los que se esconden
    public Transform[] spawnAtrapadores; // 3 puntos para los perseguidores

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Devuelve la posición de aparición según el rol y el índice de lista del jugador.
    /// </summary>
    public Vector3 GetSpawnPosition(bool esAtrapador, int playerIndex)
    {
        if (esAtrapador)
        {
            int index = playerIndex % spawnAtrapadores.Length;
            return spawnAtrapadores[index].position;
        }
        else
        {
            int index = playerIndex % spawnAtrapados.Length;
            return spawnAtrapados[index].position;
        }
    }
}