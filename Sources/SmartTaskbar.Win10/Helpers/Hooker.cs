﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;
using EasyHook;
using SmartTaskbar.Hook;

namespace SmartTaskbar
{
    public static class Hooker
    {
        private static bool _hookFailed;

        private static string _channelName;

        private static readonly string InjectionLibrary =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException(),
                "SmartTaskbar.Hook.dll");

        private static IpcServerChannel _channel;

        public static void ReleaseHook()
        {
            if (_channel is null) return;

            _channel.StopListening(null);

            _channel = null;
            _channelName = null;
        }

        public static void SetHook(IntPtr handle)
        {
            if (_hookFailed)
                return;

            // if channel is open, no need to hook again.
            if (_channel != null)
                return;

            // If the foreground Window is closing or idle, do nothing
            _ = Fun.GetWindowThreadProcessId(handle, out var pid);

            if (pid == 0)
                return;

            try
            {
                _channel = RemoteHooking.IpcCreateServer<ServerInterface>(
                    ref _channelName,
                    WellKnownObjectMode.Singleton);

                RemoteHooking.Inject(
                    pid,
                    InjectionLibrary,
                    InjectionLibrary,
                    _channelName);

                #if DEBUG
                Debug.WriteLine("Hooked!");

                #endif
            }
            catch (Exception e)
            {
                ReleaseHook();
                _hookFailed = true;


                #if DEBUG
                Debug.WriteLine(e.Message);

                #endif
            }
        }

        public static void ResetHook()
        {
            #if DEBUG
            Debug.WriteLineIf(_hookFailed, "reset Hook.");

            #endif

            _hookFailed = false;
        }
    }
}
