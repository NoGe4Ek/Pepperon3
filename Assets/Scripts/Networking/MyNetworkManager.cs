using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Networking
{
    public class MyNetworkManager : NetworkManager
    {
        [SerializeField] private GameObject unitSpawnerPrefab = null;

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);

            var transform1 = conn.identity.transform;
            GameObject unitSpawnerInstance = Instantiate(unitSpawnerPrefab, transform1.position, transform1.rotation);

            NetworkServer.Spawn(unitSpawnerInstance, conn);
        }
    }
}