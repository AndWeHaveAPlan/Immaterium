using System;
using System.Collections.Generic;
using System.Text;

namespace Immaterium.Transports.Tests
{
    public class ArrayHelper
    {
        public static byte[] TestArray1 = { 8, 7, 6, 5, 4, 3 };

        public static byte[] TestArray2 = { 5, 7, 45, 8, 3, 8, 78 };

        public static bool ByteArrayEqual(byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }
    }
}
