using System;
using Code.Helpers.AnyValue;
using Code.Helpers.SerializableGuid;

namespace Code.Gameplay.Character.Framework
{
    public struct Property
    {
        public SerializableGuid Id { get; }
        private Func<AnyValue> Getter;
        private Action<AnyValue> Setter;

        public Property(string name, Func<AnyValue> getter, Action<AnyValue> setter = null)
        {
            Id = SerializableGuid.NewGuid();
            Getter = getter;
            Setter = setter;
        }

        public AnyValue Value
        {
            get => Getter();
            set
            {
                if (Setter != null)
                {
                    Setter(value);
                }
            }
        }
    }
}