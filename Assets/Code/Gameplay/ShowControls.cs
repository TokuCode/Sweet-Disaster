using System;
using Code.Networking.Session;
using UnityEngine;

namespace Code.Gameplay
{
    public class ShowControls : MonoBehaviour
    {
        [SerializeField] private GameObject controlsPanel;

        private void Start() => controlsPanel.SetActive(SessionManager.Instance.IsPracticeMode);
        
        private void Update()
        {
            if (SessionManager.Instance.IsPracticeMode) return;

            if (Input.GetKeyDown(KeyCode.Tab))
                controlsPanel.SetActive(!controlsPanel.activeSelf);
        }
    }
}