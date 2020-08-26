/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.UI;
using Simulator.Database.Services;
using System.Linq;
using System.Collections.Generic;

namespace Simulator.Web
{
    public class ConnectionUI : MonoBehaviour
    {
        public Text statusText;
        public Button statusButton;
        public Text statusButtonText;
        public Image statusButtonIcon;
        public Button linkButton;
        public Text linkButtonText;
        public static ConnectionUI instance;
        public Color offlineColor;
        public Color onlineColor;
        public Dropdown offlineDropdown;
        public Button offlineStartButton;
        public Button offlineStopButton;
        public Text CloudTypeText;
        public Button VSEButton;

        public enum LoaderUIStateType { START, PROGRESS, READY };
        public LoaderUIStateType LoaderUIState = LoaderUIStateType.START;

        private SimulationService simulationService = new SimulationService();
        private List<SimulationData> simulationData;
        private int selectedSim;

        public void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            ColorUtility.TryParseHtmlString("#1F2940", out offlineColor);
            ColorUtility.TryParseHtmlString("#FFFFFF", out onlineColor);
            statusButtonIcon.material.color = Color.white;
            instance = this;
            statusButton.onClick.AddListener(OnStatusButtonClicked);
            linkButton.onClick.AddListener(OnLinkButtonClicked);
            offlineStartButton.onClick.AddListener(OnOfflineStartButtonClicked);
            offlineStopButton.onClick.AddListener(OnOfflineStopButtonClicked);
            UpdateDropdown();
            offlineDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            UpdateStatus();
        }

        public void UpdateDownloadProgress(string name, float percentage)
        {
            if (statusText != null)
                statusText.text = $"Downloading {name}... {percentage}%";
        }

        public void UpdateStatus()
        {
            if (statusText == null || linkButton == null || statusButtonIcon == null || statusButtonText == null || linkButtonText == null || statusButton == null)
                return; // fix for editor stop playmode null

            switch (ConnectionManager.Status)
            {
                case ConnectionManager.ConnectionStatus.Connecting:
                    statusText.text = $"Connecting to the cloud...";
                    linkButton.gameObject.SetActive(false);
                    statusButtonIcon.color = offlineColor;
                    offlineDropdown.gameObject.SetActive(false);
                    offlineStartButton.gameObject.SetActive(false);
                    VSEButton.gameObject.SetActive(false);
                    CloudTypeText.text = ConnectionManager.API?.CloudType;
                    break;
                case ConnectionManager.ConnectionStatus.Connected:
                    statusText.text = "";
                    statusButtonText.text = "Online";
                    statusButtonIcon.color = offlineColor;
                    linkButtonText.text = "LINK TO CLOUD";
                    linkButton.gameObject.SetActive(true);
                    statusButton.interactable = true;
                    offlineDropdown.gameObject.SetActive(false);
                    offlineStartButton.gameObject.SetActive(false);
                    VSEButton.gameObject.SetActive(false);
                    CloudTypeText.text = ConnectionManager.API?.CloudType;
                    break;
                case ConnectionManager.ConnectionStatus.Offline:
                    statusButtonText.text = "Offline";
                    statusText.text = "Go Online to start new simulation or run previous simulations while being Offline";
                    statusButtonIcon.color = offlineColor;
                    linkButton.gameObject.SetActive(false);
                    statusButton.interactable = true;
                    offlineDropdown.gameObject.SetActive(true);
                    offlineStartButton.gameObject.SetActive(true);
                    VSEButton.gameObject.SetActive(false);
                    UpdateDropdown();
                    CloudTypeText.text = "OFFLINE";
                    break;
                case ConnectionManager.ConnectionStatus.Online:
                    statusButtonText.text = "Online";
                    statusButtonIcon.color = onlineColor;
                    statusText.text = "";
                    linkButtonText.text = "OPEN BROWSER";
                    linkButton.gameObject.SetActive(true);
                    statusButton.interactable = true;
                    offlineDropdown.gameObject.SetActive(false);
                    offlineStartButton.gameObject.SetActive(false);
                    VSEButton.gameObject.SetActive(true);
                    CloudTypeText.text = ConnectionManager.API?.CloudType;
                    break;
            }
        }

        public void UpdateDropdown()
        {
            simulationData = simulationService.List().ToList();
            offlineDropdown.ClearOptions();
            offlineDropdown.AddOptions(simulationData.Select(s => s.Name).ToList());
            offlineDropdown.value = 0;
            selectedSim = 0;
            if(simulationData.Count == 0)
            {
                offlineDropdown.gameObject.SetActive(false);
                offlineStartButton.gameObject.SetActive(false);
            }
        }

        public void OnDropdownValueChanged(int value)
        {
            selectedSim = value;
        }

        public void OnOfflineStartButtonClicked()
        {
            Loader.StartSimulation(simulationData[selectedSim]);
            if (simulationData[selectedSim].ApiOnly)
            {
                offlineStopButton.gameObject.SetActive(true);
            }
        }

        public void OnOfflineStopButtonClicked()
        {
            Loader.StopAsync();
        }

        public void SetLinkingButtonActive(bool active)
        {
            linkButton.gameObject.SetActive(active);
        }

        public void SetVSEButtonActive(bool active)
        {
            VSEButton.gameObject.SetActive(active);
        }

        public void OnStatusButtonClicked()
        {
            ConnectionManager.instance.ConnectionStatusEvent();
        }

        public void OnLinkButtonClicked()
        {
            if (ConnectionManager.Status == ConnectionManager.ConnectionStatus.Connected)
            {
                Application.OpenURL(ConnectionManager.instance.LinkUrl);
            }
            else if (ConnectionManager.Status == ConnectionManager.ConnectionStatus.Online)
            {
                Application.OpenURL(Simulator.Web.Config.CloudUrl);
                SIM.LogSimulation(SIM.Simulation.ApplicationClick, "Open Browser");
            }
        }

        public void SetLoaderUIState(LoaderUIStateType state)
        {
            LoaderUIState = state;
            switch (LoaderUIState)
            {
                case LoaderUIStateType.START:
                    if (Config.RunAsMaster)
                        statusButton.interactable = true;
                    else
                    {
                        statusButton.interactable = false;
                        statusButtonText.text = "Client ready";
                        statusText.text = "Client ready";
                    }
                    break;
                case LoaderUIStateType.PROGRESS:
                    statusButton.interactable = false;
                    statusButtonText.text = "Loading...";
                    statusText.text = "Loading...";
                    break;
                case LoaderUIStateType.READY:
                    statusButton.interactable = false;
                    statusButtonText.text = "API ready!";
                    statusText.text = "API ready!";
                    break;
            }
        }

        public void EnterScenarioEditor()
        {
            Loader.EnterScenarioEditor();
        }
    }
}
