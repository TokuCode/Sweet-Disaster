using UnityEngine;

namespace Code.UserInterface.Network_UI
{
    public class RTTStats : MonoBehaviour
    {
        [SerializeField] private GameObject netStatsPanel;
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                netStatsPanel.SetActive(!netStatsPanel.activeSelf);
        }
    }
}