using System;
using System.Linq;
using System.Numerics;
using MoonSharp.Interpreter;

namespace Quaver.Shared.Scripting
{
    /// <summary>Contains generalized functions around CLR vectors designed to be exported to Lua.</summary>
    [MoonSharpUserData]
    static class LuaVectorWrapper
    {
        public static DynValue Add(DynValue first, DynValue second) =>
            UserData.Create(
                0 switch
                {
                    _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.Add(x, y),
                    _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Add(x, y),
                    _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector4.Add(x, y),
                    _ => throw Unreachable(first, second),
                }
            );

        public static DynValue Abs(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float f => Vector2.Abs(f.ToVector2()),
                    Vector2 v => Vector2.Abs(v),
                    Vector3 v => Vector3.Abs(v),
                    Vector4 v => Vector4.Abs(v),
                    _ => throw Unreachable(value),
                }
            );

        public static Vector3 Cross(DynValue first, DynValue second) =>
            0 switch
            {
                _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector3.Cross(x.ToVector3(), y.ToVector3()),
                _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Cross(x, y),
                _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector3.Cross(x.ToVector3(), y.ToVector3()),
                _ => throw Unreachable(first, second),
            };

        public static DynValue Clamp(DynValue first, DynValue second, DynValue third) =>
            UserData.Create(
                0 switch
                {
                    _ when TryCoerce<Vector2>(first, second, third) is var (x, y, z) => Vector2.Clamp(x, y, z),
                    _ when TryCoerce<Vector3>(first, second, third) is var (x, y, z) => Vector3.Clamp(x, y, z),
                    _ when TryCoerce<Vector4>(first, second, third) is var (x, y, z) => Vector4.Clamp(x, y, z),
                    _ => throw Unreachable(first, second, third),
                }
            );

        public static float Distance(DynValue first, DynValue second) =>
            0 switch
            {
                _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.Distance(x, y),
                _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Distance(x, y),
                _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector4.Distance(x, y),
                _ => throw Unreachable(first, second),
            };

        public static float DistanceSquared(DynValue first, DynValue second) =>
            0 switch
            {
                _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.DistanceSquared(x, y),
                _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.DistanceSquared(x, y),
                _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector4.DistanceSquared(x, y),
                _ => throw Unreachable(first, second),
            };

        public static DynValue Divide(DynValue first, DynValue second) =>
            UserData.Create(
                0 switch
                {
                    _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.Divide(x, y),
                    _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Divide(x, y),
                    _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector4.Divide(x, y),
                    _ => throw Unreachable(first, second),
                }
            );

        public static DynValue Dot(DynValue first, DynValue second) =>
            UserData.Create(
                0 switch
                {
                    _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.Dot(x, y),
                    _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Dot(x, y),
                    _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector4.Dot(x, y),
                    _ => throw Unreachable(first, second),
                }
            );

        public static int Length(DynValue value) =>
            CoerceToVectorOrFloat(value) switch
            {
                float or Vector2 => 2,
                Vector3 => 3,
                Vector4 => 4,
                _ => throw Unreachable(value),
            };

        public static DynValue Lerp(DynValue first, DynValue second, DynValue third) =>
            UserData.Create(
                0 switch
                {
                    _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.Lerp(x, y, third.CoerceToFloat()),
                    _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Lerp(x, y, third.CoerceToFloat()),
                    _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector4.Lerp(x, y, third.CoerceToFloat()),
                    _ => throw Unreachable(first, second), // 'third' is omitted since it is not part of the conversion.
                }
            );

        public static DynValue Max(DynValue first, DynValue second) =>
            UserData.Create(
                0 switch
                {
                    _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.Max(x, y),
                    _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Max(x, y),
                    _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector4.Max(x, y),
                    _ => throw Unreachable(first, second),
                }
            );

        public static DynValue Min(DynValue first, DynValue second) =>
            UserData.Create(
                0 switch
                {
                    _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.Min(x, y),
                    _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Min(x, y),
                    _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector4.Min(x, y),
                    _ => throw Unreachable(first, second),
                }
            );

        public static DynValue Multiply(DynValue first, DynValue second) =>
            UserData.Create(
                0 switch
                {
                    _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.Multiply(x, y),
                    _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Multiply(x, y),
                    _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector4.Multiply(x, y),
                    _ => throw Unreachable(first, second),
                }
            );

        public static DynValue Negate(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float f => Vector2.Negate(f.ToVector2()),
                    Vector2 v => Vector2.Negate(v),
                    Vector3 v => Vector3.Negate(v),
                    Vector4 v => Vector4.Negate(v),
                    _ => throw Unreachable(value),
                }
            );

        public static DynValue New(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float f => f.ToVector2(),
                    var v => v,
                }
            );

        public static DynValue Normalize(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float f => Vector2.Normalize(f.ToVector2()),
                    Vector2 v => Vector2.Normalize(v),
                    Vector3 v => Vector3.Normalize(v),
                    Vector4 v => Vector4.Normalize(v),
                    _ => throw Unreachable(value),
                }
            );

        public static DynValue One(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float or Vector2 => Vector2.One,
                    Vector3 => Vector3.One,
                    Vector4 => Vector4.One,
                    _ => throw Unreachable(value),
                }
            );

        public static DynValue Reflect(DynValue first, DynValue second) =>
            UserData.Create(
                0 switch
                {
                    _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.Reflect(x, y),
                    _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Reflect(x, y),
                    // Vector4.Reflect does not exist, so we have to do it manually.
                    _ when TryCoerce<Vector4>(first, second) is var (x, y) => x - 2 * Vector4.Dot(x, y) * y,
                    _ => throw Unreachable(first, second),
                }
            );

        public static DynValue SquareRoot(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float f => Vector2.SquareRoot(f.ToVector2()),
                    Vector2 v => Vector2.SquareRoot(v),
                    Vector3 v => Vector3.SquareRoot(v),
                    Vector4 v => Vector4.SquareRoot(v),
                    _ => throw Unreachable(value),
                }
            );

        public static DynValue Subtract(DynValue first, DynValue second) =>
            UserData.Create(
                0 switch
                {
                    _ when TryCoerce<Vector2>(first, second) is var (x, y) => Vector2.Subtract(x, y),
                    _ when TryCoerce<Vector3>(first, second) is var (x, y) => Vector3.Subtract(x, y),
                    _ when TryCoerce<Vector4>(first, second) is var (x, y) => Vector4.Subtract(x, y),
                    _ => throw Unreachable(first, second),
                }
            );

        public static DynValue UnitW(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float or Vector2 => default(Vector2),
                    Vector3 => default(Vector3),
                    Vector4 => Vector4.UnitW,
                    _ => throw Unreachable(value),
                }
            );

        public static DynValue UnitX(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float or Vector2 => Vector2.UnitX,
                    Vector3 => Vector3.UnitX,
                    Vector4 => Vector4.UnitX,
                    _ => throw Unreachable(value),
                }
            );

        public static DynValue UnitY(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float or Vector2 => Vector2.UnitY,
                    Vector3 => Vector3.UnitY,
                    Vector4 => Vector4.UnitY,
                    _ => throw Unreachable(value),
                }
            );

        public static DynValue UnitZ(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float or Vector2 => default(Vector2),
                    Vector3 => Vector3.UnitZ,
                    Vector4 => Vector4.UnitZ,
                    _ => throw Unreachable(value),
                }
            );

        public static DynValue Zero(DynValue value) =>
            UserData.Create(
                CoerceToVectorOrFloat(value) switch
                {
                    float or Vector2 => Vector2.Zero,
                    Vector3 => Vector3.Zero,
                    Vector4 => Vector4.Zero,
                    _ => throw Unreachable(value),
                }
            );

        static float? TryCoerceToFloat(this DynValue value) => value.CastToNumber() is { } v ? (float)v : null;

        static float CoerceToFloat(this DynValue value) => value.CastToNumber() is { } v ? (float)v : 0;

        static IFormattable CoerceToVectorOrFloat(DynValue value) =>
            value.TryCoerceToFloat() ??
            (value.Type is DataType.UserData ? value.UserData.Object as IFormattable :
                value.Type is not DataType.Table || value.TryCoerceToFloat(0, "X", "x") is not { } x ? Vector2.Zero :
                value.TryCoerceToFloat(1, "Y", "y") is not { } y ? new Vector2(x, 0) :
                value.TryCoerceToFloat(2, "Z", "z") is not { } z ? new Vector2(x, y) :
                value.TryCoerceToFloat(3, "W", "w") is not { } w ? new Vector3(x, y, z) :
                new Vector4(x, y, z, w));

        static InvalidOperationException Unreachable(params DynValue[] value) =>
            new(
                $"If you see this, it's a bug. Please report it to https://github.com/Quaver/Quaver/issues " +
                $"along with the plugin source code: {string.Join(", ", value.Select(x => x.ToPrintString()))}"
            );

        static Vector2 ToVector2(this float f) => new(f, f);

        static Vector3 ToVector3(this float f) => new(f, f, f);

        static Vector3 ToVector3(this Vector2 v) => new(v.X, v.Y, 0);

        static Vector3 ToVector3(this Vector4 v) => new(v.X, v.Y, v.Z);

        static Vector4 ToVector4(this float f) => new(f, f, f, f);

        static Vector4 ToVector4(this Vector2 v) => new(v.X, v.Y, 0, 0);

        static Vector4 ToVector4(this Vector3 v) => new(v.X, v.Y, v.Z, 0);

        static float? TryCoerceToFloat(this DynValue value, int index, string name, string otherName) =>
            value.Type switch
            {
                DataType.Tuple => value.Tuple.ElementAtOrDefault(index)?.TryCoerceToFloat(),
                DataType.Table => value.Table.Get(index).TryCoerceToFloat() ??
                    value.Table.Get(name).TryCoerceToFloat() ??
                    value.Table.Get(otherName).TryCoerceToFloat(),
                _ => null,
            };

        static T? TryCoerceTo<T>(DynValue value)
            where T : struct, IFormattable => // ReSharper disable RedundantCast
            CoerceToVectorOrFloat(value) is var val && typeof(T) == typeof(Vector2) ?
                (T?)(object)(Vector2?)(val switch
                {
                    float f => f.ToVector2(),
                    Vector2 v => v,
                    _ => null,
                }) :
                typeof(T) == typeof(Vector3) ? (T?)(object)(Vector3?)(val switch
                    {
                        float f => f.ToVector3(),
                        Vector2 v => v.ToVector3(),
                        Vector3 v => v,
                        _ => null,
                    }) :
                    typeof(T) == typeof(Vector4) ? (T?)(object)(Vector4?)(val switch
                    {
                        float f => f.ToVector4(),
                        Vector2 v => v.ToVector4(),
                        Vector3 v => v.ToVector4(),
                        Vector4 v => v,
                        _ => null,
                    }) : null; // ReSharper restore RedundantCast

        static (T X, T Y)? TryCoerce<T>(DynValue first, DynValue second)
            where T : struct, IFormattable =>
            TryCoerceTo<T>(first) is { } f && TryCoerceTo<T>(second) is { } s ? (f, s) : default;

        static (T X, T Y, T Z)? TryCoerce<T>(DynValue first, DynValue second, DynValue third)
            where T : struct, IFormattable =>
            TryCoerceTo<T>(first) is { } f && TryCoerceTo<T>(second) is { } s && TryCoerceTo<T>(third) is { } t
                ? (f, s, t)
                : default;
    }
}
