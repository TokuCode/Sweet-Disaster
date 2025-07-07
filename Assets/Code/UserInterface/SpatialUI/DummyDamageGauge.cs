using Code.Gameplay.Objects;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Code.UserInterface.SpatialUI
{
    public class DummyDamageGauge : NetworkBehaviour
    {
        [SerializeField] private Dummy _dummy;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _health;

        private void Update()
        {
            if (_dummy == null)
            {
                _health.text = string.Empty;
                return;
            }

            _health.text = $"{_dummy.CurrentDamage:N0}%";
        }
    }
}