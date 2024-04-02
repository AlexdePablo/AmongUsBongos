using m17;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static PrimerGame;

namespace LobbyPckg
{
    public class GameManager : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkManager networkManager;
        public static GameManager Singleton;
        public NetworkObject GetPlayerObject(ulong id) => networkManager.ConnectedClients[id].PlayerObject.GetComponent<NetworkObject>();
        public ulong LocalId => networkManager.LocalClient.ClientId;
        private UnityTransport Transport;

        [Header("Scriptables")]
        [SerializeField]
        private PrimerGame ColoresElegirYow;
        [SerializeField]
        private PrimerGame m_PrimerGame;
        public NetworkVariable<PrimerGame.ListaColores> ColoresElegir = new NetworkVariable<PrimerGame.ListaColores>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        [SerializeField] private NetworkVariable<MisionesHechas> m_MisionesHechas = new NetworkVariable<MisionesHechas>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private LobbyIpUIHelper m_LobbyIpUIHelper;
        private bool inGame;

        private bool ComprobandoJugadores;

        private void Awake()
        {
            inGame = false;
            ComprobandoJugadores = false;
            if (Singleton != null && Singleton != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Singleton = this;
            }

            DontDestroyOnLoad(this);
        }

        void Start()
        {
            networkManager = NetworkManager.Singleton;
            Transport = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<UnityTransport>();
            m_PrimerGame.EverythingAvailable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (networkManager.IsHost)
            {
                networkManager.OnClientConnectedCallback += CuandoAlguienSeConecta;
                networkManager.OnClientDisconnectCallback += CuandoAlguienSeDesconecta;
                ColoresElegir.Value = ColoresElegirYow.ListaColorines;
                for (int i = 0; i < 6; i++)
                {
                    m_MisionesHechas.Value.m_MisionsPassed[i].m_MisionesHechas = 0;
                }
            }
        }
        public void StartHost()
        {
            networkManager.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
            networkManager.StartHost();
            networkManager.SceneManager.LoadScene("LobbyIp", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        public void StartClient()
        {
            networkManager.StartClient();
        }

        private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest arg1, NetworkManager.ConnectionApprovalResponse arg2)
        {
            if (inGame)
                arg2.Approved = false;
            else
            {
                arg2.Approved = true;
                arg2.CreatePlayerObject = true;
            }
        }

        private void CuandoAlguienSeDesconecta(ulong playerId)
        {
            PlayerBehaviour playercin = null;
            ulong[] ids = new ulong[networkManager.ConnectedClientsList.Count];

            for (int i = 0; i < networkManager.ConnectedClientsList.Count; i++)
            {
                ids[i] = networkManager.ConnectedClientsIds[i];
            }

            foreach (NetworkClient player in networkManager.ConnectedClientsList)
            {
                if (player.ClientId == playerId)
                {
                    playercin = player.PlayerObject.GetComponent<PlayerBehaviour>();
                    LiberarColorRpc(playercin.ColorPersonaje.Value);
                }
            }
            if (inGame)
            {
                int numerinMisiones = 0;
                foreach (MisionesColores misioncita in m_MisionesHechas.Value.m_MisionsPassed)
                {
                    if (misioncita.m_PlayerID == playerId)
                    {
                        numerinMisiones = misioncita.m_MisionesHechas;
                        break;
                    }
                }

                ComprobarJugadoresVivosRpc();

                if (GameObject.Find("LevelManager").TryGetComponent<LevleManager>(out var levelManager) && NetworkManager.Singleton)
                {
                    try
                    {
                        levelManager.RestarMisionesRpc(numerinMisiones);
                        levelManager.ComprobarMisionesFinales();
                    }
                    catch (Exception ex)
                    {
                        print(ex);
                    }
                }

            }


        }


        public void SetIp(string ip)
        {
            var connectionData = Transport.ConnectionData;
            connectionData.Address = ip;
            Transport.ConnectionData = connectionData;
        }

        private void CuandoAlguienSeConecta(ulong obj)
        {
            if (networkManager.ConnectedClientsList.Count > 6)
            {
                networkManager.DisconnectClient(obj);
            }

        }

        [Rpc(SendTo.Server)]
        public void SubirMisionRpc(ulong id)
        {
            for (int i = 0; i < 6; i++)
            {
                if (m_MisionesHechas.Value.m_MisionsPassed[i].m_PlayerID == id)
                    m_MisionesHechas.Value.m_MisionsPassed[i].m_MisionesHechas++;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

            if (scene.name == "InicioIp")
            {
                inGame = false;
                m_PrimerGame.EverythingAvailable();
            }
            if (scene.name == "LobbyIp")
            {
                for (int i = 0; i < 6; i++)
                {
                    m_MisionesHechas.Value.m_MisionsPassed[i].m_MisionesHechas = 0;
                }
                m_LobbyIpUIHelper = GameObject.Find("LobbyIpUiHelper").GetComponent<LobbyIpUIHelper>();
                inGame = false;
            }
            if (scene.name == "GameIp")
            {
                inGame = true;
                if (networkManager.IsHost)
                {
                    StartCoroutine(MandarMisiones());
                   
                }
            }
        }

        private IEnumerator MandarMisiones()
        {
            yield return new WaitForEndOfFrame();

            int num = 0;
            for (int i = 0; i < 6; i++)
            {
                num += m_MisionesHechas.Value.m_MisionsPassed[i].m_MisionesHechas;
            }
            if (GameObject.Find("LevelManager").TryGetComponent<LevleManager>(out var levelManager))
                levelManager.SumarMisionesRpc(num);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void LiberarColorRpc(Color colorinLibre)
        {
            if (NetworkManager.Singleton != null)
            {
                for (int i = 0; i < ColoresElegir.Value.m_Colores.Length; i++)
                {
                    if (ColoresElegir.Value.m_Colores[i].m_ColorPersonaje == colorinLibre)
                    {
                        ColoresElegir.Value.m_Colores[i].m_ColorDisponible = true;
                        break;
                    }
                }
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        public void ChangeRpc(int numeroAnterior, int numeroActual)
        {
            ColoresElegir.Value.m_Colores[numeroAnterior].m_ColorDisponible = true;
            ColoresElegir.Value.m_Colores[numeroActual].m_ColorDisponible = false;
            ColoresElegirYow.ListaColorines.m_Colores[numeroAnterior].m_ColorDisponible = true;
            ColoresElegirYow.ListaColorines.m_Colores[numeroActual].m_ColorDisponible = false;
        }
        [Rpc(SendTo.Server)]
        public void DarColorinInicialRpc(ulong id)
        {
            for (int i = 0; i < ColoresElegir.Value.m_Colores.Length; i++)
            {
                if (ColoresElegir.Value.m_Colores[i].m_ColorDisponible)
                {
                    SendColorToCLientRpc(ColoresElegir.Value.m_Colores[i].m_ColorPersonaje, (ulong)i, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { id }
                        }
                    }
                                );
                    break;
                }
            }

        }
        [Rpc(SendTo.Server)]
        public void DarColorinRpc(ulong id, int numeroDeColorin)
        {
            foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (player.ClientId == id)
                {
                    SendColorToCLientRpc(ColoresElegir.Value.m_Colores[numeroDeColorin].m_ColorPersonaje, ColoresElegir.Value.m_Colores[numeroDeColorin].m_ColorDisponible, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { id }
                        }
                    }
                        );
                    break;
                }
            }
        }
        [ClientRpc]
        private void SendColorToCLientRpc(Color m_ColorPersonaje, ulong _Numerin, ClientRpcParams clientRpcParams)
        {
            m_LobbyIpUIHelper.SendColor(m_ColorPersonaje, (int)_Numerin);
        }
        [ClientRpc]
        private void SendColorToCLientRpc(Color m_ColorPersonaje, bool ColorDsiponible, ClientRpcParams clientRpcParams)
        {
            m_LobbyIpUIHelper.SendColor(m_ColorPersonaje, ColorDsiponible);
        }
        public override void OnNetworkDespawn()
        {
            SceneManager.LoadScene("InicioIp");
            base.OnNetworkDespawn();
        }

        [Rpc(SendTo.Server)]
        public void ComprobarJugadoresVivosRpc()
        {
            if (ComprobandoJugadores == false)
                StartCoroutine(AFinDelJuego());
        }

        private IEnumerator AFinDelJuego()
        {
            ComprobandoJugadores = true;
            yield return new WaitForSeconds(1);

            int vivos = 0;
            int sus = 0;

            foreach (NetworkClient player in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (!player.PlayerObject.GetComponent<PlayerBehaviour>().GetSus() && player.PlayerObject.GetComponent<PlayerBehaviour>().GetVivo())
                    vivos++;
                if (player.PlayerObject.GetComponent<PlayerBehaviour>().GetSus())
                    sus++;
                if (player.PlayerObject.GetComponent<PlayerBehaviour>().GetSus() && !player.PlayerObject.GetComponent<PlayerBehaviour>().GetVivo())
                    StartCoroutine(FinDelJuego());
            }

            if (vivos == 1 || sus == 0)
                NetworkManager.Singleton.SceneManager.LoadScene("LobbyIp", LoadSceneMode.Single);

            ComprobandoJugadores = false;
        }
        private IEnumerator FinDelJuego()
        {
            yield return new WaitForSeconds(1);
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyIp", LoadSceneMode.Single);
        }

        [Serializable]
        public struct MisionesColores : INetworkSerializable
        {
            public ulong m_PlayerID;
            public int m_MisionesHechas;

            public MisionesColores(ulong _PlayerID, int _MisionesHechas)
            {
                m_PlayerID = _PlayerID;
                m_MisionesHechas = _MisionesHechas;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref m_PlayerID);
                serializer.SerializeValue(ref m_MisionesHechas);
            }
        }
        [Serializable]
        public struct MisionesHechas : INetworkSerializable
        {
            public MisionesColores[] m_MisionsPassed;

            public MisionesHechas(MisionesColores[] _MisionsPassed)
            {
                m_MisionsPassed = _MisionsPassed;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref m_MisionsPassed);
            }
        }

    }
}
