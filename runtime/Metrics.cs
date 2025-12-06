using System;
using UnityEngine;

namespace Estqes.MapGen
{
    public static class Metrics
    {
        public static float P = 2.5f;
        public static Func<Cell, Cell, float> EucludMetric = (x, y) =>
        {
            return Vector3.Distance(x.Center, y.Center);
        };

        public static Func<Cell, Cell, float> ManhattanMetric = (x, y) =>
        {
            return Mathf.Abs(x.Center.x - y.Center.x) + Mathf.Abs(x.Center.y - y.Center.y) + Mathf.Abs(x.Center.z - y.Center.z);
        };

        public static Func<Cell, Cell, float> ChebyshevMetric = (x, y) =>
        {
            var a = x.Center;
            var b = y.Center;

            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Max(Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z)));
        };

        public static Func<Cell, Cell, float> MinkowskiMetric = (x, y) =>
        {
            var a = x.Center;
            var b = y.Center;

            float dx = Mathf.Abs(a.x - b.x);
            float dy = Mathf.Abs(a.y - b.y);
            float dz = Mathf.Abs(a.z - b.z);

            return (float)Mathf.Pow(Mathf.Pow(dx, P) + Mathf.Pow(dy, P) + Mathf.Pow(dz, P), 1 / P);
        };
    }
}