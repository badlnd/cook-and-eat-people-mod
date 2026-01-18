using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LC.CEPM.CEPMLoggingUtils
{
    public static class LoggingUtils
    {

        private static byte Byte(float v)
        {
            return (byte)(v * Mathf.Clamp01(v));
        }

        private static string ToHexString(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", Byte(c.r), Byte(c.g), Byte(c.b));
        }

        /// <summary>
        /// Returns a colour formatted for use in logging.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string Colour(object str, Color c)
        {
            return string.Format("$<color={0}>{1}</color>", ToHexString(c), str);
        }
    }
}
