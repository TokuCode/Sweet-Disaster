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
        bool Switch { get; }
        bool Shield { get; }
        Vector3 HandlePosition { get; }
        Vector3 HandleDirection { get; }
    }
}