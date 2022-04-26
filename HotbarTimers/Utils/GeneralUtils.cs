using FFXIVClientStructs.FFXIV.Client.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HotbarTimers
{
    public static class GeneralUtils
    {
        public static ByteColor CalculateByteColorFromVector(Vector4 vector)
        {
            return new ByteColor
            {
                R = (byte)(vector.X * 255f),
                G = (byte)(vector.Y * 255f),
                B = (byte)(vector.Z * 255f),
                A = (byte)vector.W,
            };
        }
    }
}
