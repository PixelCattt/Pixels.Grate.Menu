using System.Linq;
using UnityEngine;

namespace Bark.Networking;

internal class RoomSpecific : MonoBehaviour
{
    public NetPlayer? Owner;

    private void FixedUpdate()
    {
        if (!NetworkSystem.Instance.InRoom) Destroy(gameObject);
        if (Owner != null)
            if (!NetworkSystem.Instance.AllNetPlayers.Contains(Owner))
                Destroy(gameObject);
    }
}