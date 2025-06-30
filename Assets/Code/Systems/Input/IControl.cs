using UnityEngine;

namespace Code.Systems.Input
{
    public interface IControl
    {
        float Move { get; }
        bool Jump { get; }
        bool Crouch { get; }
        bool Shoot { get; }
        bool Reload { get; }
        bool Throw { get; }
        bool Melee { get; }
        bool Shield { get; }
        bool Free { get; }
        Vector3 HandlePosition { get; }
        Vector3 HandleDirection { get; }
    }
}