using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LobbyPckg
{
    public class MainMenuUIHelper : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _ipAddressInputField;

        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _hostButton;

        // Start is called before the first frame update
        void Start()
        {
            SubscribeToEvents();
            Setup();
        }

        private void SubscribeToEvents()
        {
            _joinButton.onClick.AddListener(OnJoinButtonPressed);
            _hostButton.onClick.AddListener(OnHostButtonPressed);
            _ipAddressInputField.onValueChanged.AddListener(ValidateIpInputField);
        }
        private void Setup()
        {
            _hostButton.interactable = true;
            _joinButton.interactable = false;
        }

        private void ValidateIpInputField(string text)
        {
            if (text.Length >= 9)
            {
                _joinButton.interactable = true;
            }
            else
            {
                _joinButton.interactable = false;
            }
        }
        private void OnHostButtonPressed()
        {
            GameManager.Singleton.StartHost();
        }

        private void OnJoinButtonPressed()
        {
            GameManager.Singleton.SetIp(TryParseIpAddressFromInputField().Host);
            GameManager.Singleton.StartClient();
        }

        private Uri TryParseIpAddressFromInputField()
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme= "kcp";
            if(_ipAddressInputField && IPAddress.TryParse(_ipAddressInputField.text, out IPAddress adress))
            {
                uriBuilder.Host = adress.ToString();
            }
            else
            {
                uriBuilder.Host = "localhost";
            }

            var uri = new Uri(uriBuilder.ToString(), UriKind.Absolute);
            return uri;
        }
    }
}
