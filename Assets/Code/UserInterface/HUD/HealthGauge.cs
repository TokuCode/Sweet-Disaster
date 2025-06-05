using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using TMPro;
using UnityEngine;

public class HealthGauge : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _healthText;
    
    [Header("Health Parameters")]
    [SerializeField] private Color _idleColor;
    [SerializeField] private Color _overshootColor;

    private void Update() => UpdateText();

    private void UpdateText()
    {
        if (PlayerController.Singleton == null) return;

        if (!PlayerController.Singleton.Dependencies.TryGetFeature(out Health healthFeature)) return;
        
        var health = healthFeature.HealthAmount;
        var baseHealth = healthFeature.BaseHealth;
        
        _healthText.text = $"{health:N0}%";
        _healthText.color = health < baseHealth ? _idleColor : _overshootColor;
    }
}
