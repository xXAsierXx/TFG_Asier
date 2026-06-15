using Unity.Netcode.Components;
using UnityEngine;

//este script le dice a Netcode que el dueño del personaje tiene el control de sus propias animaciones
public class ClientNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}