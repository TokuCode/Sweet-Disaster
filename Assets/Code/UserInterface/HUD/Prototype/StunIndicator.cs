using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using UnityEngine;
using UnityEngine.UI;

public class StunIndicator : MonoBehaviour
{
    private Health _health;
    
    [Header("UI Elements")]
    [SerializeField] private Image _stunIndicatorImage;
    
    [Header("Stun Parameters")]
    [SerializeField] private Color _stunColor;
    [SerializeField] private Color _normalColor;

    private void Update() => UpdateColor();

    private void UpdateColor()
    {
        if(PlayerController.Singleton == null) return;
        
        PlayerController.Singleton.Dependencies.TryGetFeature(out _health);
        
        if(_health == null) return;
        
        _stunIndicatorImage.color = _health.IsStunned ? _stunColor : _normalColor;
    }
}
