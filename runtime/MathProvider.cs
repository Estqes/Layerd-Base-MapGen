using System;
using System.Collections.Generic;
using UnityEngine;

namespace Estqes.MapGen
{
    public interface IMath<T>
    {
        T MaxValue { get; }
        T MinValue { get; }

        T Add(T a, T b);
        T Subtract(T a, T b);
        T Multiply(T a, T b);
        T Divide(T a, T b);
        T MultiplyScalar(T a, float b);
        T Max(T a, T b);
        T Min(T a, T b);

    }

    public class FloatMath : IMath<float>
    {
        public float MaxValue => float.MaxValue;
        public float MinValue => float.MinValue;

        public float Add(float a, float b) => a + b;
        public float Subtract(float a, float b) => a - b;
        public float Multiply(float a, float b) => a * b;
        public float Divide(float a, float b) => b != 0 ? a / b : 0;
        public float MultiplyScalar(float a, float b) => a * b;
        public float Lerp(float a, float b, float t) => Mathf.Lerp(a, b, t);
        public float Max(float a, float b) => Mathf.Max(a, b);
        public float Min(float a, float b) => Mathf.Min(a, b);
    }

    public class ColorMath : IMath<Color>
    {
        public Color MaxValue => Color.black;
        public Color MinValue => Color.white;

        public Color Add(Color a, Color b) => a + b;
        public Color Subtract(Color a, Color b) => a - b;
        public Color Multiply(Color a, Color b) => a * b; 
        public Color Divide(Color a, Color b) => new Color(
            b.r != 0 ? a.r / b.r : 0,
            b.g != 0 ? a.g / b.g : 0,
            b.b != 0 ? a.b / b.b : 0,
            b.a != 0 ? a.a / b.a : 0);

        public Color MultiplyScalar(Color a, float b) => a * b;
        public Color Lerp(Color a, Color b, float t) => Color.Lerp(a, b, t);
        public Color Max(Color a, Color b) => Vector4.Max((Vector4)a, (Vector4)b);
        public Color Min(Color a, Color b) => Vector4.Min((Vector4)a, (Vector4)b);
    }

    public class IntMath : IMath<int>
    {
        public int MaxValue => int.MinValue;
        public int MinValue => int.MaxValue;

        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
        public int Multiply(int a, int b) => a * b;
        public int Divide(int a, int b) => b != 0 ? a / b : 0;
        public int MultiplyScalar(int a, float b) => (int)(a * b);
        public int Lerp(int a, int b, float t) => (int)Mathf.Lerp(a, b, t);
        public int Max(int a, int b) => Math.Max(a, b);
        public int Min(int a, int b) => Math.Min(a, b);
    }

    public static class MathProvider
    {
        private static readonly Dictionary<Type, object> _maths = new();
        static MathProvider()
        {
            RegisterDefaults();
        }

        private static void RegisterDefaults()
        {
            AddMath(new FloatMath());
            AddMath(new ColorMath());
            AddMath(new IntMath());
        }


        public static void AddMath<T>(IMath<T> math)
        {
            if(!_maths.TryAdd(typeof(T), math))
            {
                Debug.LogWarning($"Math implementation for {typeof(T)} is already registered.");

            }
        }

        public static IMath<T> Get<T>()
        {
            if(!_maths.TryGetValue(typeof(T), out var math))
            {
                throw new Exception($"Math implementation for type {typeof(T)} not found! Call MathProvider.AddMath<{typeof(T)}>() first.");
            }
            return (IMath<T>)math;
        }
    }
}