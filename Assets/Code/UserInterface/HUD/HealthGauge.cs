using System;
using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using TMPro;
using UnityEngine;

public class HealthGauge : MonoBehaviour
{
    private Health _health;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _healthText;
    
    [Header("Health Parameters")]
    [SerializeField] private Color _idleColor;
    [SerializeField] private Color _overshootColor;

    private void Update() => UpdateText();

    private void UpdateText()
    {
        if (PlayerController.Singleton == null) return;
        
        PlayerController.Singleton.Dependencies.TryGetFeature(out _health);
        
        if (_health == null) return;
        
        var health = _health.HealthAmount;
        var baseHealth = _health.BaseHealth;
        
        _healthText.text = $"{health:N0}%";
        _healthText.color = health < baseHealth ? _idleColor : _overshootColor;
    }
}
