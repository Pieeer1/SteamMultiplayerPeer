using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Steam;
internal static class SteamExtensions
{
    public static byte[] ToBytes<T>(this T t) where T : struct
    {
        int size = Marshal.SizeOf(t);
        byte[] arr = new byte[size];

        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(t, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        return arr;

    }
    public static T? ToStruct<T>(this byte[] bytes)
    {
        int size = Marshal.SizeOf(typeof(T));
        if (bytes.Length < size)
            throw new Exception("Invalid parameter");

        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(bytes, 0, ptr, size);
            return Marshal.PtrToStructure<T>(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items,
                                                   int maxItems)
    {
        return items.Select((item, inx) => new { item, inx })
                    .GroupBy(x => x.inx / maxItems)
                    .Select(g => g.Select(x => x.item));
    }
}
