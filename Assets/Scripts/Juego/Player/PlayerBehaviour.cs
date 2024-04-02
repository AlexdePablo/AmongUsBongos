using LobbyPckg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static UnityEngine.UI.Image;

namespace m17
{
    public class PlayerBehaviour : NetworkBehaviour
    {
        [Header("Network Variables")]
        private NetworkVariable<float> m_Speed = new NetworkVariable<float>(10);
        private NetworkVariable<bool> m_Sus = new NetworkVariable<bool>(false);
        private NetworkVariable<Color> m_ColorPersonaje = new NetworkVariable<Color>(Color.gray, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<Color> ColorPersonaje => m_ColorPersonaje;
        private NetworkVariable<bool> m_Vivo = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Vivo => m_Vivo;

        [Header("Inputs")]
        [SerializeField] private InputActionAsset m_InputAsset;
        private InputActionAsset m_Input;
        public InputActionAsset Input => m_Input;
        private InputAction m_MovementAction;
        public InputAction MovementAction => m_MovementAction;

        [Header("Animaciones")]
        [SerializeField] private RuntimeAnimatorController m_AnimatorControllerVivo;
        [SerializeField] private RuntimeAnimatorController m_AnimatorControllerMuerto;
        private Animator m_Animator;
        NetworkAnimator m_NetworkAnimator;
        private SpriteRenderer m_SpriteRenderer;

        [Header("Layers")]
        [SerializeField] private string m_LayerVivo;
        [SerializeField] private string m_LayerMuerto;
        [SerializeField] private string m_LayerRecienMuerto;
        [SerializeField] private LayerMask m_CamaraMuerto;
        [SerializeField] private LayerMask m_CamaraVivo;
        [SerializeField] private LayerMask m_DeteccionSus;
        [SerializeField] private LayerMask m_DeteccionMision;
        [SerializeField] private LayerMask m_DeteccionCadaver;

        [Header("InGame")]
        private bool InGame;

        [Header("Estados")]
        private Estado m_EstadoActual;
        public enum Estado { IDLE, WALK, DEAD, QUIET, KILLING }

        [Header("Cositas")]
        private float m_StateDeltaTime;
        Rigidbody2D m_Rigidbody;
        GameObject m_Camera;
        GameObject m_LuzPlayer;
        GameObject m_LuzGlobal;
        private LevleManager LevelManager;

        // No es recomana fer servir perquè estem en el món de la xarxa
        // però podem per initialitzar components i variables per a totes les instàncies
        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody2D>();
            m_NetworkAnimator = GetComponent<NetworkAnimator>();
            m_Animator = GetComponent<Animator>();
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
        }
        // Això sí que seria el nou awake
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_Vivo.OnValueChanged += OnVivoChanged;
            m_ColorPersonaje.OnValueChanged += OnColorChanged;
            GetComponent<SpriteRenderer>().color = m_ColorPersonaje.Value;
            SceneManager.sceneLoaded += OnSceneLoaded;
            //Aquest awake només per a qui li pertany, perquè tocarem variables on només
            //a nosaltres ens interessa llegir el seu valor
            if (!IsOwner)
                return;
            m_Camera = GameObject.FindGameObjectWithTag("MainCamera");
            Assert.IsNotNull(m_InputAsset);
            m_Input = Instantiate(m_InputAsset);
            m_MovementAction = m_Input.FindActionMap("Default").FindAction("Movement");
            m_Input.FindActionMap("Default").Enable();
        }

       

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsOwner)
            {
                TeleportarJugadorIncioRpc();
                InGame = true;
                m_Camera = GameObject.FindGameObjectWithTag("MainCamera");
                if (scene.name == "GameIp")
                {
                    if (m_Vivo.Value == false)
                    {
                        m_Animator.runtimeAnimatorController = m_AnimatorControllerMuerto;
                        int LayerVivo = LayerMask.NameToLayer(m_LayerMuerto);
                        transform.gameObject.layer = LayerVivo;
                    }
                    else
                    {
                        m_Animator.runtimeAnimatorController = m_AnimatorControllerVivo;
                        int LayerVivo = LayerMask.NameToLayer(m_LayerVivo);
                        transform.gameObject.layer = LayerVivo;
                    }
                    m_LuzGlobal = GameObject.Find("LuzGlobal");
                    m_LuzPlayer = GameObject.Find("LuzPersonaje");
                    if (m_Sus.Value == true)
                    {
                        m_LuzGlobal.GetComponent<Light2D>().intensity = 1;
                        m_LuzPlayer.GetComponent<Light2D>().intensity = 0;
                    }
                    else
                    {
                        m_LuzGlobal.GetComponent<Light2D>().intensity = 0;
                        m_LuzPlayer.GetComponent<Light2D>().intensity = 1;
                    }
                    ChangeState(Estado.IDLE);
                    if (LevelManager == null)
                        LevelManager = GameObject.Find("LevelManager").GetComponent<LevleManager>();

                    if (m_Vivo.Value)
                        m_Camera.GetComponent<Camera>().cullingMask = m_CamaraVivo;
                    else
                        m_Camera.GetComponent<Camera>().cullingMask = m_CamaraMuerto;

                }
                if (scene.name == "LobbyIp")
                {
                    InGame = false;
                    RevivirJugadores();
                    m_Animator.runtimeAnimatorController = m_AnimatorControllerVivo;
                    ChangeState(Estado.IDLE);
                }
                if (scene.name == "VotacionesIp")
                {
                    InGame = false;
                    ChangeState(Estado.QUIET);
                    m_Input.FindActionMap("Default").Enable();
                }
            }
            else
            {
                if (scene.name == "GameIp")
                {
                    if (m_Vivo.Value == false)
                    {
                        m_Animator.runtimeAnimatorController = m_AnimatorControllerMuerto;
                        int LayerVivo = LayerMask.NameToLayer(m_LayerMuerto);
                        transform.gameObject.layer = LayerVivo;
                    }
                    else
                    {
                        m_Animator.runtimeAnimatorController = m_AnimatorControllerVivo;
                        int LayerVivo = LayerMask.NameToLayer(m_LayerVivo);
                        transform.gameObject.layer = LayerVivo;
                    }
                }
                if (scene.name == "LobbyIp")
                {
                    m_Animator.runtimeAnimatorController = m_AnimatorControllerVivo;
                }
            }
        }

        [Rpc(SendTo.Server)]
        private void TeleportarJugadorIncioRpc()
        {
            m_Rigidbody.transform.position= Vector3.zero;
        }

        private void RevivirJugadores()
        {
            RevivirJugadorRpc(OwnerClientId);
        }
        [Rpc(SendTo.Server)]
        private void RevivirJugadorRpc(ulong id)
        {
            foreach (NetworkClient Player in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (Player.ClientId == id)
                {
                    Player.PlayerObject.GetComponent<PlayerBehaviour>().SetVivo(true);
                }
            }
        }

        private void OnVivoChanged(bool previousValue, bool newValue)
        {
            if (newValue == false)
            {
                ChangeState(Estado.DEAD);
                int LayerRecienMuerto = LayerMask.NameToLayer(m_LayerRecienMuerto);
                transform.gameObject.layer = LayerRecienMuerto;
            }
            else
            {
                int LayerVivo = LayerMask.NameToLayer(m_LayerVivo);
                transform.gameObject.layer = LayerVivo;
            }
            GameManager.Singleton.ComprobarJugadoresVivosRpc();
        }

     
       
        public void SetVivo(bool vivo)
        {
            m_Vivo.Value = vivo;
        }
        public bool GetVivo()
        {
            return m_Vivo.Value;
        }
        private void OnColorChanged(Color previousValue, Color newValue)
        {
            GetComponent<SpriteRenderer>().color = newValue;
        }
        public ulong KillPlayer()
        {
            ulong idMuerto;
            RaycastHit2D[] hit = Physics2D.CircleCastAll(transform.position, 2, Vector2.zero, 2, m_DeteccionSus);
            if (hit.Length > 0)
            {
                for (int i = 0; i < hit.Length; i++)
                {
                    if (hit[i].transform.GetComponent<PlayerBehaviour>().OwnerClientId == OwnerClientId)
                    {
                        continue;
                    }
                    else
                    {
                        idMuerto = hit[i].transform.GetComponent<PlayerBehaviour>().OwnerClientId;
                        return idMuerto;
                        //MatarJugadorRpc(hit[i].transform.GetComponent<PlayerBehaviour>().OwnerClientId);
                    }
                }
            }
            return 0;
        }

        public void HacerMision()
        {
            HacerMisionRpc(transform.position, OwnerClientId);
        }

        [Rpc(SendTo.Server)]
        private void HacerMisionRpc(Vector3 playerPosition, ulong playerId)
        {
            RaycastHit2D hit = Physics2D.CircleCast(playerPosition, 2, Vector2.zero, 2, m_DeteccionMision);

            if (hit.collider != null)
            {
                AbrirMisionClientRpc(playerPosition, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { playerId }
                    }
                });
            }
        }

        [ClientRpc]
        private void AbrirMisionClientRpc(Vector3 playerPosition, ClientRpcParams clientRpcParams)
        {
            RaycastHit2D hit = Physics2D.CircleCast(playerPosition, 2, Vector2.zero, 2, m_DeteccionMision);

            if (hit.collider != null)
            {
                hit.collider.GetComponent<PrimerMinijuego>().AbrirPanel();
            }
        }

        public void Morirse()
        {
            SetVivo(false);
        }

        public void ReportarCadaver()
        {
            ReportCadaverRpc();
        }
        [Rpc(SendTo.Server)]
        private void ReportCadaverRpc()
        {
            NetworkManager.Singleton.SceneManager.LoadScene("VotacionesIp", LoadSceneMode.Single);
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_Vivo.OnValueChanged -= OnVivoChanged;
            m_ColorPersonaje.OnValueChanged -= OnColorChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;

        }

        void Update()
        {
            if (!IsOwner)
                return;

            UpdateState(m_EstadoActual);
            HandleCollision();
        }

        private void HandleCollision()
        {
            if (!InGame)
                return;
            if (m_Sus.Value)
            {
                RaycastHit2D[] hit = Physics2D.CircleCastAll(transform.position, 2, Vector2.zero, 2, m_DeteccionSus);
                /*if(hit.Length > 0)
                {
                    for(int i = 0; i < hit.Length; i++)
                    {
                        if (hit[i].transform.GetComponent<PlayerBehaviour>().OwnerClientId == OwnerClientId)
                            continue;

                        print($"{hit[i].fraction}  {hit[i].transform.GetComponent<PlayerBehaviour>().OwnerClientId}");
                    }
                }*/
                if (hit.Length > 1)
                {
                    LevelManager.SetMatarButtonInteract(true);
                }
                else
                {
                    LevelManager.SetMatarButtonInteract(false);
                }
            }
            else
            {
                RaycastHit2D hit = Physics2D.CircleCast(transform.position, 2, Vector2.zero, 2, m_DeteccionMision);

                if (hit.collider != null && hit.collider.GetComponent<SpriteRenderer>().color == Color.red)
                {
                    LevelManager.SetMisionButtonInteract(true);
                }
                else
                {
                    LevelManager.SetMisionButtonInteract(false);
                }
            }
            if (m_Vivo.Value)
            {
                RaycastHit2D hitMuerto = Physics2D.CircleCast(transform.position, 2, Vector2.zero, 2, m_DeteccionCadaver);
                if (hitMuerto.collider != null)
                {
                    LevelManager.SetReportarButtonInteract(true);
                }
                else
                {
                    LevelManager.SetReportarButtonInteract(false);
                }
            }
            else
            {
                LevelManager.SetReportarButtonInteract(false);
            }
        }

        [Rpc(SendTo.Server)]
        private void ChangeAnimationRPC(string animation)
        {
            m_NetworkAnimator.Animator.Play(animation);
        }
        //Això seria el moviment a física
        [Rpc(SendTo.Server)]
        private void MoveCharacterPhysicsRpc(Vector3 _Velocity)
        {
            m_Rigidbody.velocity = _Velocity.normalized * m_Speed.Value;

            if (_Velocity.x > 0)
            {
                ChangeAnimationRPC("WalkDerecha");
            }
            else if (_Velocity.x < 0)
            {
                ChangeAnimationRPC("WalkIzquierda");
            }
            if (_Velocity.y == 1)
            {
                ChangeAnimationRPC("WalkTop");
            }
            else if (_Velocity.y == -1)
            {
                ChangeAnimationRPC("WalkDown");
            }
        }

        protected void ChangeState(Estado newState)
        {
            if (newState == m_EstadoActual)
                return;

            ExitState(m_EstadoActual);
            InitState(newState);
        }
        protected void InitState(Estado initState)
        {
            m_EstadoActual = initState;
            m_StateDeltaTime = 0;
            switch (m_EstadoActual)
            {
                case Estado.IDLE:
                    ChangeAnimationRPC("Idle");
                    break;
                case Estado.WALK:

                    break;
                case Estado.KILLING:
                    ChangeAnimationRPC("Kill");
                    break;
                case Estado.QUIET:
                    ChangeAnimationRPC("Idle");
                    break;
                case Estado.DEAD:
                    ChangeAnimationRPC("Dead");

                    break;
                default:
                    break;
            }
        }

        protected void UpdateState(Estado updateState)
        {
            m_StateDeltaTime += Time.deltaTime;
            Vector2 m_Velocity = m_MovementAction.ReadValue<Vector2>();
            m_Camera.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
            if (InGame)
                m_LuzPlayer.transform.position = transform.position;

            switch (updateState)
            {
                case Estado.IDLE:
                    if (m_Velocity != Vector2.zero)
                    {
                        ChangeState(Estado.WALK);
                    }
                    break;
                case Estado.WALK:
                    if (m_Velocity != Vector2.zero)
                    {
                        MoveCharacterPhysicsRpc(m_Velocity);
                    }
                    else
                        ChangeState(Estado.IDLE);
                    break;
                case Estado.KILLING:
                    if (m_StateDeltaTime >= 1)
                        ChangeState(Estado.IDLE);
                    break;
                case Estado.QUIET:

                    break;
                case Estado.DEAD:

                    break;
                default:
                    break;
            }
        }

        public void CambiarColorPersonaje(Color color)
        {
            m_ColorPersonaje.Value = color;
        }

        public void CambiarColorPersonaje()
        {
            m_SpriteRenderer.color = m_ColorPersonaje.Value;
        }

        protected void ExitState(Estado exitState)
        {
            switch (exitState)
            {
                case Estado.IDLE:

                    break;
                case Estado.WALK:
                    MoveCharacterPhysicsRpc(Vector2.zero);
                    break;
                case Estado.KILLING:

                    break;
                case Estado.QUIET:

                    break;
                case Estado.DEAD:

                    break;
                default:
                    break;
            }
        }
        public void SetSus(bool _Sus) { m_Sus.Value = _Sus; }
        public bool GetSus() { return m_Sus.Value; }
      
    }
}
