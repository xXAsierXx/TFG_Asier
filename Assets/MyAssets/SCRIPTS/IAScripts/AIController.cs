using UnityEngine;
using Unity.Netcode;
//Este script sirve para controlar el prefab que se instancie al principio de la ronda despues de que se hayan escondido los humanos. Y en la segunda para que se pueda esconder la IA directamente. (Activar y desactivar el movimiento dependiendo del momento)
public class AIController : NetworkBehaviour
{
    private Renderer[] renderersIA;
    private bool visibleActualmente = true;

    private void Awake()
    {
        // Guardamos todos los Renderers (mallas 3D) que tenga la IA en sus hijos
        renderersIA = GetComponentsInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Comprobación inicial nada más aparecer en red
        ActualizarVisibilidad();
    }

    private void Update()
    {
        // Solo monitorizamos cambios si el GameManager existe
        if (GameManager.Instance == null) return;

        ActualizarVisibilidad();
    }

    private void ActualizarVisibilidad()
    {
        bool enEscondite = GameManager.Instance.enFaseEscondite.Value;
        int ronda = GameManager.Instance.rondaActual.Value;

        // Determinar si la IA debe verse o no
        bool debeSerVisible = true;

        // En la RONDA 1, la IA es Atrapadora -> Oculta durante la fase de escondite
        if (ronda == 1 && enEscondite)
        {
            debeSerVisible = false;
        }

        // Si el estado de visibilidad ha cambiado, activamos o desactivamos los renderers
        if (debeSerVisible != visibleActualmente)
        {
            visibleActualmente = debeSerVisible;
            SetRenderersState(visibleActualmente);
        }
    }

    private void SetRenderersState(bool state)
    {
        if (renderersIA == null) return;

        foreach (Renderer r in renderersIA)
        {
            if (r != null)
            {
                r.enabled = state;
            }
        }
    }
}