using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace LobbyPckg
{
    public class SPAAAAAAAAAAAAAAAAAAAAWN : NetworkBehaviour
    {
        [SerializeField]
        private NetworkObject m_PlayerLobbyPrefab;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        [Rpc(SendTo.Server)]
        public void SpawnPlayerServerRPC()
        {
            print("Spawning on Server");
            NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(m_PlayerLobbyPrefab);
        }
    }
}