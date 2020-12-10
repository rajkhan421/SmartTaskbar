﻿using System;
using Microsoft.Win32;
using SmartTaskbar.Models;
using static SmartTaskbar.PlatformInvoke.SafeNativeMethods;

namespace SmartTaskbar.Engines.Helpers
{
    internal static class ButtonSizeHelper
    {
        private const int HwndBroadcast = 0xffff;
        private const int WmSettingChange = 0x001a;

        private static readonly RegistryKey Key =
            Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true)
            ?? throw new InvalidOperationException("OpenSubKey Failed.");


        public static void SetIconSize(IconSize size)
        {
            Key.SetValue("TaskbarSmallIcons", (int) size);
            // https://github.com/cprcrack/AdaptiveTaskbar/blob/4a1ce94044ae3de47ba63877558794dd698ad9e5/Program.cs#L165
            SendNotifyMessage((IntPtr) HwndBroadcast, WmSettingChange, UIntPtr.Zero, "TraySettings");
        }


        public static IconSize GetIconSize()
            => (IconSize) Key.GetValue("TaskbarSmallIcons", (int) IconSize.IconLarge)!;
    }
}
