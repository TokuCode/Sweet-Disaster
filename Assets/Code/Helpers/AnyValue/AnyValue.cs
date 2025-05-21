using System;
using UnityEngine;

namespace Code.Helpers.AnyValue
{
    public struct AnyValue
    {
        public ValueType type;
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public Vector3 vector3Value;
        public string stringValue;
        
        public T CastTo<T>()
        {
            if (typeof(T) == typeof(object)) CastToObject<T>();
            return type switch
            {
                ValueType.Bool => AsBool<T>(boolValue),
                ValueType.Int => AsInt<T>(intValue),
                ValueType.Float => AsFloat<T>(floatValue),
                ValueType.Vector3 => AsVector3<T>(vector3Value),
                ValueType.String => AsString<T>(stringValue),
                _ => throw new InvalidCastException(
                    $"Cannot cast AnyValue of type {type} to the specified type {typeof(T).Name}.")
            };
        }
        
        T AsBool<T>(bool value) => typeof(T) == typeof(bool) && value is T correctType ? correctType : default;
        T AsInt<T>(int value) => typeof(T) == typeof(int) && value is T correctType ? correctType : default;
        T AsFloat<T>(float value) => typeof(T) == typeof(float) && value is T correctType ? correctType : default;
        T AsVector3<T>(Vector3 value) => typeof(T) == typeof(Vector3) && value is T correctType ? correctType : default;
        T AsString<T>(string value) => typeof(T) == typeof(string) && value is T correctType ? correctType : default;
        
        public T CastToObject<T>()
        {
            return type switch
            {
                ValueType.Bool => (T)(object)boolValue,
                ValueType.Int => (T)(object)intValue,
                ValueType.Float => (T)(object)floatValue,
                ValueType.Vector3 => (T)(object)vector3Value,
                ValueType.String => (T)(object)stringValue,
                _ => throw new InvalidCastException("Cannot cast to the specified type.")
            };
        }
        
        public static implicit operator bool(AnyValue value) => value.CastTo<bool>();
        public static implicit operator int(AnyValue value) => value.CastTo<int>();
        public static implicit operator float(AnyValue value) => value.CastTo<float>();
        public static implicit operator Vector3(AnyValue value) => value.CastTo<Vector3>();
        public static implicit operator string(AnyValue value) => value.CastTo<string>();

        public static Type TypeOf(ValueType valueType)
        {
            return valueType switch
            {
                ValueType.Bool => typeof(bool),
                ValueType.Int => typeof(int),
                ValueType.Float => typeof(float),
                ValueType.Vector3 => typeof(Vector3),
                ValueType.String => typeof(string),
                ValueType.Void => typeof(void),
                _ => throw new NotSupportedException($"Unsupported ValueType: {valueType}")
            };
        }
        
        public static ValueType ValueTypeOf(Type type)
        {
            return type switch
            {
                _ when type == typeof(bool) => ValueType.Bool,
                _ when type == typeof(int) => ValueType.Int,
                _ when type == typeof(float) => ValueType.Float,
                _ when type == typeof(Vector3) => ValueType.Vector3,
                _ when type == typeof(string) => ValueType.String,
                _ when type == typeof(void) => ValueType.Void,
                _ => throw new NotSupportedException($"Unsupported type: {type}")
            };
        }
    }
}