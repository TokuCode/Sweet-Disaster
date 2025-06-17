using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using UnityEngine;
using UnityEngine.UI;

public class ThrowUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image _chargeFillImage;
    
    [Header("Color Values")]
    [SerializeField] private Color _throwReady;
    [SerializeField] private Color _throwing;
    [SerializeField] private Color _throwOnCooldown;
    
    [Header("Parameters")]
    [SerializeField] private float _minimumFillValue;
    
    private void Update()
    {
        UpdateFillValue();
        UpdateFillColor();
    }

    private void UpdateFillValue()
    {
        if(PlayerController.Singleton == null) return;

        if (!PlayerController.Singleton.Dependencies.TryGetFeature(out Bomb bomb))
        {
            return;
        }
        
        if (bomb.IsThrowing) _chargeFillImage.fillAmount = Mathf.Max(Mathf.Clamp01(bomb.ThrowChargeTimer / bomb.ThrowChargeTimeSeconds), _minimumFillValue);
        else _chargeFillImage.fillAmount = 1;
    }

    private void UpdateFillColor()
    {
        if(PlayerController.Singleton == null) return;

        if (!PlayerController.Singleton.Dependencies.TryGetFeature(out Bomb bomb))
        {
            return;
        }
        
        if (bomb.IsThrowing) _chargeFillImage.color = _throwing;
        else if (bomb.IsOnCooldown) _chargeFillImage.color = _throwOnCooldown;else _chargeFillImage.color = _throwReady;
    }
}
