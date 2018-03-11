using System;
using System.ComponentModel;

namespace Walton_Scaper
{
    public static class Extensions
    {
        public static void InvokeEx<T>(this T @this, Action<T> action) where T : ISynchronizeInvoke
        {
            if (@this.InvokeRequired)
            {
                @this.Invoke(action, new object[] { @this });
            }
            else
            {
                action(@this);
            }
        }

        public static int ToInt(this string value)
        {
            var intValue = 0;
            int.TryParse(value, out intValue);

            return intValue;
        }
    }
}