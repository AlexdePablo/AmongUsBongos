using m17;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.LookDev;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    [Header("Chats")]
    [SerializeField] private GameObject m_ChatPanel;
    [SerializeField] private Transform m_ChatParent;
    [SerializeField] private Button m_EnviarMensajeButton;
    [SerializeField] private TMP_InputField m_MensajeText;
    [Header("Texto Segundos")]
    [SerializeField] private GameObject TextoPrefab;
    [Header("Votaciones")]
    [SerializeField] private TextMeshProUGUI m_SegundosRestantesTexto;
    [SerializeField] private GameObject m_VotacionesPanel;
    [SerializeField] private Transform m_VotacionParent;
    [SerializeField] private GameObject m_VoltaremPrefab;
    [Header("Muerto")]
    [SerializeField] private GameObject m_JugadorMuertoPanel;
    [SerializeField] private Image m_PersonajeMuertoImage;
    [SerializeField] private TextMeshProUGUI m_PersonajeMuertoTexto;
    private Color m_ColorPersonaje;

    private NetworkVariable<int> contador = new NetworkVariable<int>(40);
    private Botoncines[] botoncines;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (m_EnviarMensajeButton) m_EnviarMensajeButton.onClick.AddListener(EnviarMensaje);
        if (NetworkManager.Singleton.IsHost)
            StartCoroutine(Contador());
        contador.OnValueChanged += seCambioElTiempo;
        m_ColorPersonaje = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<PlayerBehaviour>().ColorPersonaje.Value;
        m_ChatPanel.SetActive(true);
        if (!NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerBehaviour>().Vivo.Value)
            m_MensajeText.interactable = false;
        m_VotacionesPanel.SetActive(false);
        m_JugadorMuertoPanel.SetActive(false);
    }

    private void seCambioElTiempo(int previousValue, int newValue)
    {
        m_SegundosRestantesTexto.text = newValue + "";
    }

    private IEnumerator Contador()
    {
        while (contador.Value > 0)
        {
            contador.Value--;
            yield return new WaitForSeconds(1);
        }
        contador.Value = 20;
        StartCoroutine(Contados());
    }
    private IEnumerator Contados()
    {
        RepartirBotones();
        while (contador.Value > 0)
        {
            contador.Value--;
            yield return new WaitForSeconds(1);
        }
        contador.Value = 10;
        StartCoroutine(MatarJugador());
    }

    private IEnumerator MatarJugador()
    {
        int num;
        ulong id = 0;
        Color colorinDelMuerto = Color.white;
        List<int> listaVotaciones = new List<int>();

        for (int i = 0; i < m_VotacionParent.childCount; i++)
        {
            listaVotaciones.Add(Int32.Parse(m_VotacionParent.GetChild(i).GetChild(2).GetComponent<TextMeshProUGUI>().text));
        }
        listaVotaciones.Sort();
        listaVotaciones.Reverse();

        if (listaVotaciones[0] != listaVotaciones[1])
        {
            num = listaVotaciones[0];

            for (int i = 0; i < m_VotacionParent.childCount; i++)
            {
                if (Int32.Parse(m_VotacionParent.GetChild(i).GetChild(2).GetComponent<TextMeshProUGUI>().text) == num)
                {
                    id = m_VotacionParent.GetChild(i).GetChild(1).GetComponent<BotonVotacion>().PlayerID;
                }
            }
            foreach (NetworkClient Player in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (Player.ClientId == id)
                {
                    colorinDelMuerto = Player.PlayerObject.GetComponent<PlayerBehaviour>().ColorPersonaje.Value;
                    Player.PlayerObject.GetComponent<PlayerBehaviour>().SetVivo(false);
                }
            }

            MatarJugadorClientRpc(id, colorinDelMuerto, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
                }
            });
        }
        else
        {
            NoMatarJugadorClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
                }
            });
        }

        while (contador.Value > 0)
        {
            contador.Value--;
            yield return new WaitForSeconds(1);
        }
        NetworkManager.Singleton.SceneManager.LoadScene("GameIp", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    [ClientRpc]
    private void NoMatarJugadorClientRpc( ClientRpcParams clientRpcParams)
    {
        m_VotacionesPanel.SetActive(false);
        m_JugadorMuertoPanel.SetActive(true);
        m_PersonajeMuertoTexto.text = $"Nadie ha sido asesinado";
    }
    [ClientRpc]
    private void MatarJugadorClientRpc(ulong id, Color colorinDelMuerto, ClientRpcParams clientRpcParams)
    {
        m_VotacionesPanel.SetActive(false);
        m_JugadorMuertoPanel.SetActive(true);
        m_PersonajeMuertoImage.color = colorinDelMuerto;
        m_PersonajeMuertoTexto.text = $"El jugador {id} ha sido asesinado";
    }

    private void RepartirBotones()
    {
        botoncines = new Botoncines[NetworkManager.Singleton.ConnectedClients.Count];

        for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
        {
            botoncines[i] = new Botoncines(NetworkManager.Singleton.ConnectedClientsIds[i], NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<PlayerBehaviour>().ColorPersonaje.Value, 0, NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<PlayerBehaviour>().Vivo.Value);
        }
        EnviarBotonesClientRpc(botoncines, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
            }
        });
    }

    [Rpc(SendTo.Server)]
    public void AumentarVotacionPersonajeRpc(ulong idPersonaVotada, ulong idPersonaQueHaVotado)
    {
        for(int i = 0; i < botoncines.Length; i++)
        {
            if (idPersonaVotada == botoncines[i].m_PlayerId)
            {
                botoncines[i].m_Votacion++;
                NuevoVotoClientRpc(i, botoncines[i].m_Votacion, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
                    }
                });
                BloquearBotonesClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { idPersonaQueHaVotado }
                    }
                });
                break;
            }
        }
    }

    [ClientRpc]
    private void BloquearBotonesClientRpc(ClientRpcParams clientRpcParams)
    {
        for(int i = 0; i < m_VotacionParent.childCount; i++)
        {
            m_VotacionParent.GetChild(i).GetChild(1).GetComponent<Button>().interactable = false;
        }
    }

    [ClientRpc]
    private void NuevoVotoClientRpc(int i, int votos, ClientRpcParams parametrillos)
    {
        m_VotacionParent.GetChild(i).GetChild(2).GetComponent<TextMeshProUGUI>().text = votos+"";
    }
    
    [ClientRpc]
    private void EnviarBotonesClientRpc(Botoncines[] botines, ClientRpcParams parametros)
    {
        m_ChatPanel.SetActive(false);
        m_VotacionesPanel.SetActive(true);
        foreach (Botoncines botoncin in botines)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerBehaviour>().Vivo.Value)
            {
                GameObject voltarem = Instantiate(m_VoltaremPrefab, m_VotacionParent);
                voltarem.transform.GetChild(0).GetComponent<Image>().color = botoncin.m_Color;
                voltarem.transform.GetChild(1).GetComponent<BotonVotacion>().setId(botoncin.m_PlayerId);
                if(!botoncin.m_Vivo)
                    voltarem.transform.GetChild(1).GetComponent<Button>().interactable = false;
            }
            else
            {
                GameObject voltarem = Instantiate(m_VoltaremPrefab, m_VotacionParent);
                voltarem.transform.GetChild(0).GetComponent<Image>().color = botoncin.m_Color;
                voltarem.transform.GetChild(1).GetComponent<BotonVotacion>().setId(botoncin.m_PlayerId);
                voltarem.transform.GetChild(1).GetComponent<Button>().interactable = false;
            }
        }
    }
    private void EnviarMensaje()
    {
        EnviarMensajeRpc(m_MensajeText.text, m_ColorPersonaje);
        m_MensajeText.text = "";
    }

    [Rpc(SendTo.Server)]
    private void EnviarMensajeRpc(string texto, Color colorin)
    {
        SendMessageToClientRpc(texto, colorin, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
            }
        });
    }

    [ClientRpc]
    private void SendMessageToClientRpc(string message, Color color, ClientRpcParams parametrillos)
    {
        GameObject textin = Instantiate(TextoPrefab, m_ChatParent);
        textin.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = message;
        textin.transform.GetChild(0).GetComponent<Image>().color = color;
    }

    [Serializable]
    private struct Botoncines : INetworkSerializable
    {
        public ulong m_PlayerId;
        public Color m_Color;
        public int m_Votacion;
        public bool m_Vivo;

        public Botoncines(ulong _PlayerId, Color _Color, int _Votacion, bool _Vivo)
        {
            m_PlayerId = _PlayerId;
            m_Color = _Color;   
            m_Votacion = _Votacion;
            m_Vivo = _Vivo;
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref m_PlayerId);
            serializer.SerializeValue(ref m_Color);
            serializer.SerializeValue(ref m_Votacion);
            serializer.SerializeValue(ref m_Vivo);
        }
    }
}
