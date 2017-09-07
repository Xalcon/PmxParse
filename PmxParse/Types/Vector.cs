using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PmxParse.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vec2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public override string ToString()
        {
            return $"[{nameof(Vec2)}] {{{nameof(X)}: {X} / {nameof(Y)}: {Y}}}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vec3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public override string ToString()
        {
            return $"[{nameof(Vec3)}] {{{nameof(X)}: {X} / {nameof(Y)}: {Y} / {nameof(Z)}: {Z}}}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vec4
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public override string ToString()
        {
            return $"[{nameof(Vec4)}] {{{nameof(X)}: {X} / {nameof(Y)}: {Y} / {nameof(Z)}: {Z} / {nameof(W)}: {W}}}";
        }
    }
}
