﻿namespace SmartTaskbar.Models
{
    internal static class AutoModeTypeHelper
    {
        /// <summary>
        ///     string to AutoModeType
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static AutoModeType AsAutoModeType(this string str)
        {
            switch (str)
            {
                case nameof(AutoModeType.None):
                    return AutoModeType.None;
                case nameof(AutoModeType.Auto):
                    return AutoModeType.Auto;
                default:
                    return AutoModeType.Auto;
            }
        }
    }
}
