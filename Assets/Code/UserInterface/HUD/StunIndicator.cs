using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using UnityEngine;
using UnityEngine.UI;

public class StunIndicator : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image _stunIndicatorImage;
    
    [Header("Stun Parameters")]
    [SerializeField] private Color _stunColor;
    [SerializeField] private Color _normalColor;

    private void Update() => UpdateColor();

    private void UpdateColor()
    {
        if(PlayerController.Singleton == null)return;
        
        if(!PlayerController.Singleton.Dependencies.TryGetFeature(out Health healthFeature)) return;
        
        _stunIndicatorImage.color = healthFeature.IsStunned ? _stunColor : _normalColor;
    }
}
