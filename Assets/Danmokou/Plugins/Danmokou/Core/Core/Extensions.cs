﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Danmokou.DMath;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Danmokou.Core {
public static class Extensions {
    public static LocalizedString Locale(this string en, string ja) => new LocalizedString(en) {
        jp = ja
    };
    public static int CountOf(this string s, char c) {
        int ct = 0;
        for (int ii = 0; ii < s.Length; ++ii) {
            if (s[ii] == c) ++ct;
        }
        return ct;
    }

    private static Action<Task> WrapRethrow(Action cb) => t => {
        Exception? exc = t.Exception;
        try {
            cb();
        } catch (Exception e) {
            exc = new Exception(e.Message, exc);
        }
        if (exc != null) {
            Log.UnityError("Task exceptions:\n" + Log.StackInnerException(exc).Message);
            throw exc;
        }
    };

    public static Task ContinueWithSync(this Task t, Action done) =>
        t.ContinueWith(WrapRethrow(done), TaskContinuationOptions.ExecuteSynchronously);

    private static T Private<T>(this object obj, string privateField) => (T) obj.GetType()
        .GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj)!;


    public static void Show(this VisualElement ve) => ve.style.display = DisplayStyle.Flex;
    public static void Hide(this VisualElement ve) => ve.style.display = DisplayStyle.None;

    public static string? Or(this string? x, string? y) =>
        string.IsNullOrWhiteSpace(x) ? y : x;
    
    public static Stream ToStream(this string str) {
        MemoryStream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(str);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}

public static class ArrayExtensions {

    public static void Insert<T>(this T[] arr, ref int count, T obj, int at) {
        if (count == arr.Length) throw new IndexOutOfRangeException();
        Array.Copy(arr, at, arr, at + 1, count++ - at);
        arr[at] = obj;
    }

    public static T[] Extend<T>(this T[] first, T[] second) {
        var ret = new T[first.Length + second.Length];
        Array.Copy(first, 0, ret, 0, first.Length);
        Array.Copy(second, 0, ret, first.Length, second.Length);
        return ret;
    }

    public static T ModIndex<T>(this T[] arr, int index) => arr[M.Mod(arr.Length, index)];

    public static T? Try<T>(this IList<T> arr, int index) where T : class {
        if (index >= 0 && index < arr.Count) return arr[index];
        return null;
    }

    public static T? TryN<T>(this IList<T> arr, int index) where T : struct {
        if (index >= 0 && index < arr.Count) return arr[index];
        return null;
    }

    public static bool Try<T>(this T[] arr, int index, out T res) where T : class {
        if (index >= 0 && index < arr.Length) {
            res = arr[index];
            return true;
        }
        res = null!;
        return false;
    }

    public static T Try<T>(this T[] arr, int index, T deflt) {
        if (index >= 0 && index < arr.Length) return arr[index];
        return deflt;
    }

    /// <summary>
    /// Returns -1 if not found
    /// </summary>
    public static int IndexOf<T>(this T[] arr, T obj) where T : class {
        for (int ii = 0; ii < arr.Length; ++ii) {
            if (arr[ii] == obj) return ii;
        }
        return -1;
    }

    /// <summary>
    /// Returns the first T such that the associated priority is LEQ the given priority.
    /// Make sure the array is sorted from lowest to highest priority.
    /// </summary>
    public static T GetBounded<T>(this (int priority, T)[] arr, int priority, T deflt) {
        var result = deflt;
        for (int ii = 0; ii < arr.Length; ++ii) {
            if (priority >= arr[ii].priority) result = arr[ii].Item2;
            else break;
        }
        return result;
    }
}

public interface IUnrollable<T> {
    IEnumerable<T> Values { get; }
}

public static class IEnumExtensions {
    public static IEnumerable<(int idx, T val)> Enumerate<T>(this IEnumerable<T> arr) => arr.Select((x, i) => (i, x));

    public static void ForEach<T>(this IEnumerable<T> arr, Action<T> act) {
        foreach (var ele in arr) {
            act(ele);
        }
    }

    public static void ForEachI<T>(this IEnumerable<T> arr, Action<int, T> act) {
        foreach (var (i, ele) in arr.Enumerate()) {
            act(i, ele);
        }
    }

    public static IEnumerable<T> Unroll<T>(this IEnumerable<T> arr) {
        foreach (var p in arr) {
            if (p is IUnrollable<T> ur) {
                foreach (var res in ur.Values.Unroll()) yield return res;
            } else {
                yield return p;
            }
        }
    }

    public static IEnumerable<int> Range(this int max) {
        for (int ii = 0; ii < max; ++ii) yield return ii;
    }

    public static IEnumerable<int> Range(this (int min, int max) bound) {
        for (int ii = bound.min; ii < bound.max; ++ii) yield return ii;
    }

    public static IEnumerable<double> Step(this (double min, double max) bound, double step) {
        for (double x = bound.min; x < bound.max; x += step) yield return x;
    }
    public static IEnumerable<U> SelectNotNull<T, U>(this IEnumerable<T> arr, Func<T, U?> f) where U : class {
        foreach (var x in arr) {
            var y = f(x);
            if (y != null) yield return y;
        }
    }
    public static IEnumerable<U> SelectNotNull<T, U>(this IEnumerable<T> arr, Func<T, U?> f) where U : struct {
        foreach (var x in arr) {
            var y = f(x);
            if (y.HasValue) yield return y.Value;
        }
    }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> arr) where T : class => arr.Where(x => x != null)!;

    public static int IndexOf<T>(this IEnumerable<T> arr, Func<T, bool> pred) {
        foreach (var (i, x) in arr.Enumerate()) {
            if (pred(x)) return i;
        }
        return -1;
    }

    public static T? FirstOrNull<T>(this IEnumerable<T> arr) where T : struct {
        foreach (var x in arr) return x;
        return null;
    }

    public static IEnumerable<T> FilterNone<T>(this IEnumerable<T?> arr) where T : struct {
        foreach (var x in arr) {
            if (x.Try(out var y)) yield return y;
        }
    }
    public static IEnumerable<T> FilterNone<T>(this IEnumerable<T?> arr) where T : class {
        foreach (var x in arr) {
            if (x != null) yield return x;
        }
    }

    public static IEnumerable<(K key, V value)> Items<K, V>(this Dictionary<K, V> dict) => dict.Keys.Select(k => (k, dict[k]));

    public static IEnumerable<(K key, V[] values)> GroupToArray<K, V>(this IEnumerable<IGrouping<K, V>> grp) =>
        grp.Select(g => (g.Key, g.ToArray()));

    public static (K key, V[] values) MaxByGroupSize<K, V>(this IEnumerable<IGrouping<K, V>> grp) =>
        grp.GroupToArray().MaxBy(g => g.values.Length);

    public static T MaxBy<T, U>(this IEnumerable<T> arr, Func<T, U> selector) where U : IComparable<U> {
        bool first = true;
        T obj = default!;
        U val = default!;
        foreach (var item in arr) {
            if (first) {
                first = false;
                obj = item;
                val = selector(item);
            } else {
                var nextVal = selector(item);
                if (nextVal.CompareTo(val) > 0) {
                    obj = item;
                    val = nextVal;
                }
            }
        }
        return obj;
    }
    public static (T obj, U val) MaxByWith<T, U>(this IEnumerable<T> arr, Func<T, U> selector) where U : IComparable<U> {
        bool first = true;
        T obj = default!;
        U val = default!;
        foreach (var item in arr) {
            if (first) {
                first = false;
                obj = item;
                val = selector(item);
            } else {
                var nextVal = selector(item);
                if (nextVal.CompareTo(val) > 0) {
                    obj = item;
                    val = nextVal;
                }
            }
        }
        return (obj, val);
    }
}

public static class ListExtensions {
    public static void AssignOrExtend<T>(this List<T> from, ref List<T>? into) {
        if (into == null) into = from;
        else into.AddRange(from);
    }

    public static void IncrLoop<T>(this List<T> arr, ref int idx) => arr.Count.IncrLoop(ref idx);

    public static void DecrLoop<T>(this List<T> arr, ref int idx) => arr.Count.DecrLoop(ref idx);

    public static void IncrLoop(this int mod, ref int idx) {
        if (++idx >= mod) idx = 0;
    }

    public static void DecrLoop(this int mod, ref int idx) {
        if (--idx < 0) idx = mod - 1;
    }
}

public static class DictExtensions {
    public static V GetOrThrow<K, V>(this Dictionary<K, V> dict, K key) {
        if (dict.TryGetValue(key, out var res)) return res;
        throw new Exception($"Key \"{key}\" does not exist.");
    }

    public static V GetOrThrow<K, V>(this IReadOnlyDictionary<K, V> dict, K key, string indict) {
        if (dict.TryGetValue(key, out var res)) return res;
        throw new Exception($"Key \"{key}\" does not exist in the dictionary {indict}.");
    }

    public static void AddToList<K, V>(this Dictionary<K, List<V>> dict, K key, V value) {
        if (!dict.TryGetValue(key, out var l)) {
            dict[key] = l = new List<V>();
        }
        l.Add(value);
    }

    public static bool Has2<K, K2, V>(this Dictionary<K, Dictionary<K2, V>> dict, K key, K2 key2) =>
        dict.TryGetValue(key, out var dct2) && dct2.ContainsKey(key2);

    public static bool TryGet2<K, K2, V>(this Dictionary<K, Dictionary<K2, V>> dict, K key, K2 key2, out V val) {
        val = default!;
        return dict.TryGetValue(key, out var dct2) && dct2.TryGetValue(key2, out val);
    }

    public static V SetDefault<K, V>(this Dictionary<K, V> dict, K key) where V : new() {
        if (!dict.TryGetValue(key, out var data)) {
            data = dict[key] = new V();
        }
        return data;
    }

    public static V SetDefault<K, V>(this Dictionary<K, V> dict, K key, V deflt) {
        if (!dict.TryGetValue(key, out var data)) {
            data = dict[key] = deflt;
        }
        return data;
    }

    public static void SetDefaultSet<K, K2, V>(this Dictionary<K, Dictionary<K2, V>> dict, K key, K2 key2, V value) {
        if (!dict.TryGetValue(key, out var data)) {
            data = dict[key] = DictCache<K2, V>.Get();
        }
        data[key2] = value;
    }

    public static void DuplicateIfExists<K, K2, V>(this Dictionary<K, Dictionary<K2, V>> source, K key, K key2) {
        if (source.TryGetValue(key, out var data)) {
            var into = source[key2] = DictCache<K2, V>.Get();
            data.CopyInto(into);
        }
    }

    public static void TryRemoveAndCache<K, K2, V>(this Dictionary<K, Dictionary<K2, V>> dict, K key) {
        if (dict.TryGetValue(key, out var data)) {
            DictCache<K2, V>.Consign(data);
            dict.Remove(key);
        }
    }

    public static void TryRemoveAndCacheAll<K, K2, V>(this Dictionary<K, Dictionary<K2, V>> dict) {
        foreach (var k in dict.Keys.ToArray()) dict.TryRemoveAndCache(k);
    }

    public static void TryRemoveAndCacheAllExcept<K, K2, V>(this Dictionary<K, Dictionary<K2, V>> dict,
        HashSet<K> exceptions) {
        foreach (var k in dict.Keys.ToArray()) {
            if (!exceptions.Contains(k)) dict.TryRemoveAndCache(k);
        }
    }

    public static void ClearExcept<K, V>(this Dictionary<K, V> dict, HashSet<K> exceptions) {
        foreach (var k in dict.Keys.ToArray()) {
            if (!exceptions.Contains(k)) dict.Remove(k);
        }
    }

    public static void ClearExcept<K>(this HashSet<K> dict, HashSet<K> exceptions) {
        foreach (var k in dict.ToArray()) {
            if (!exceptions.Contains(k)) dict.Remove(k);
        }
    }

    public static V GetOrDefault<K, V>(this Dictionary<K, V> dict, K key) {
        if (dict.TryGetValue(key, out var res)) return res;
        return default!;
    }
    public static V GetOrDefault<K, V>(this Dictionary<K, V> dict, K key, V deflt) {
        if (dict.TryGetValue(key, out var res)) return res;
        return deflt;
    }

    public static V GetOrDefault2<K1, K2, V>(this Dictionary<K1, Dictionary<K2, V>> dict, K1 key, K2 key2) {
        if (dict.TryGetValue(key, out var res) && res.TryGetValue(key2, out var res2)) return res2;
        return default!;
    }

    public static V GetOrDefault3<K1, K2, K3, V>(this Dictionary<K1, Dictionary<K2, Dictionary<K3, V>>> dict, K1 key,
        K2 key2, K3 key3) {
        if (dict.TryGetValue(key, out var res) && res.TryGetValue(key2, out var res2)
                                               && res2.TryGetValue(key3, out var res3)) return res3;
        return default!;
    }

    public static void CopyInto<K, V>(this Dictionary<K, V> src, Dictionary<K, V> target) {
        foreach (var kv in src) target[kv.Key] = kv.Value;
    }

    public static V SearchByType<V>(this Dictionary<Type, V> src, object obj, bool searchInterfaces) {
        var t = obj.GetType();
        if (src.TryGetValue(t, out var v) && v != null)
            return v;
        //Search interfaces first so UINodeLR<T> matches interface before matching UINode
        if (searchInterfaces) {
            foreach (var it in obj.GetType().GetInterfaces()) {
                if (src.TryGetValue(it, out v) && v != null)
                    return v;
            }
        }
        while ((t = t.BaseType) != null) {
            if (src.TryGetValue(t, out v) && v != null)
                return v;
        }
        throw new Exception($"Couldn't find type {obj.GetType()} in dictionary");
    }

    public static void Push<K, V>(this Dictionary<K, Stack<V>> dict, K key, V value) {
        if (!dict.TryGetValue(key, out var s)) s = dict[key] = new Stack<V>();
        s.Push(value);
    }

    public static void Pop<K, V>(this Dictionary<K, Stack<V>> dict, K key) {
        var s = dict[key];
        s.Pop();
        if (s.Count == 0) dict.Remove(key);
    }
}

public static class FuncExtensions {
    public static Func<bool> Or(this Func<bool> x, Func<bool> y) => () => x() || y();

    public static Action Void<T>(this Func<T>? x) => () => x?.Invoke();

    public static Action Link(this Action a, Action b) => () => {
        a();
        b();
    };

}

public static class NullableExtensions {
    public static bool? And(this bool? x, bool y) => x.HasValue ? (bool?) (x.Value && y) : null;
    public static bool? Or(this bool? x, bool y) => x.HasValue ? (bool?) (x.Value || y) : null;

    public static U? FMap<T, U>(this T? x, Func<T, U> f) where T : struct where U : struct
        => x.HasValue ? (U?) f(x.Value) : null;

    public static U? Bind<T, U>(this T? x, Func<T, U?> f) where T : struct where U : struct
        => x.HasValue ? f(x.Value) : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Try<T>(this T? x, out T y) where T : struct {
        if (x.HasValue) {
            y = x.Value;
            return true;
        } else {
            y = default;
            return false;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Try<T>(this T? x, out T y) where T : class {
        y = x!;
        return x != null;
    }

    public static T Elvis<T>(this T? x, T y) where T : UnityEngine.Object
        => (x == null) ? y : x;

}

public static class FormattingExtensions {
    public static string PadRight(this int x, int by) => x.ToString().PadRight(by);
    public static string PadLZero(this int x, int by) => x.ToString().PadLeft(by, '0');

    public static string SimpleDate(this DateTime d) =>
        $"{d.Year}/{d.Month.PadLZero(2)}/{d.Day.PadLZero(2)}";
    public static string SimpleTime(this DateTime d) =>
        $"{d.SimpleDate()} " +
        $"{d.Hour.PadLZero(2)}:{d.Minute.PadLZero(2)}:{d.Second.PadLZero(2)}";

    public static string FileableTime(this DateTime d) =>
        $"{d.Year} {d.Month.PadLZero(2)} {d.Day.PadLZero(2)} " +
        $"{d.Hour.PadLZero(2)} {d.Minute.PadLZero(2)} {d.Second.PadLZero(2)}";
}

public static class NumExtensions {

    public static char ToABC(this int x) => (char) ('A' + x);

    public static LocalizedString FramesToTime(this int f) {
        int s = (int) (f / ETime.ENGINEFPS_F);
        int hours = s / 3600;
        s %= 3600;
        int minutes = s / 60;
        s %= 60;
        int seconds = s;
        return hours > 0 ? 
            LocalizedStrings.Generic.render_hoursminssecs_ls(hours, minutes, seconds) : 
            LocalizedStrings.Generic.render_minssecs_ls(minutes, seconds);
    }
}

public static class UnityExtensions {
    
    public static void SetAlpha(this SpriteRenderer sr, float a) {
        var c = sr.color;
        c.a = a;
        sr.color = c;
    }
}

public static class DataStructExtensions {
    public static T? TryPeek<T>(this Stack<T> stack) where T : class =>
        stack.Count > 0 ? stack.Peek() : null;
}

}