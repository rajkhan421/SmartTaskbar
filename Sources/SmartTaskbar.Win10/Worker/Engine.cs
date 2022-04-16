﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace SmartTaskbar
{
    internal sealed class Engine
    {
        private static Timer _timer;

        private static int _timerCount;
        private static TaskbarInfo _taskbar;

        private static readonly HashSet<IntPtr> DesktopHandleSet = new HashSet<IntPtr>();
        private static readonly Stack<IntPtr> LastHideForegroundHandle = new Stack<IntPtr>();
        private static ForegroundWindowInfo _currentForegroundWindow;


        public Engine(Container container)
        {
            // 125 milliseconds is a balance between user-acceptable perception and system call time.
            _timer = new Timer(container)
            {
                Interval = 125
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private static void Timer_Tick(object sender, EventArgs e)
        {
            if (UserSettings.AutoModeType != AutoModeType.Auto)
                return;

            // get taskbar every 1.25 second.
            if (_timerCount % 5 == 0)
            {
                // Make sure the taskbar has been automatically hidden, otherwise it will not work
                Fun.SetAutoHide();

                _taskbar = TaskbarHelper.InitTaskbar();

                // Some users will kill the explorer.exe under certain situation.
                // In this case, the taskbar cannot be found, just return and wait for the user to reopen the file explorer.
                if (_taskbar.Handle == IntPtr.Zero)
                {
                    Hooker.ReleaseHook();
                    return;
                }

                Hooker.SetHook(_taskbar.Handle);
            }

            switch (_taskbar.CheckIfMouseOver())
            {
                case TaskbarBehavior.DoNothing:
                    break;
                case TaskbarBehavior.Pending:
                    CheckCurrentWindow();

                    break;
                case TaskbarBehavior.Show:
                    #if DEBUG
                    Debug.WriteLine("Show the tasbkar because of Mouse Over.");
                    #endif

                    _taskbar.ShowTaskar();
                    break;
            }

            ++_timerCount;

            // clear cache and reset stable every 5 min.
            if (_timerCount <= 2400) return;

            _timerCount = 0;

            DesktopHandleSet.Clear();
            Hooker.ResetHook();
        }

        private static void CheckCurrentWindow()
        {
            var behavior = _taskbar.CheckIfForegroundWindowIntersectTaskbar(DesktopHandleSet, out var info);

            switch (behavior)
            {
                case TaskbarBehavior.DoNothing:
                    break;
                case TaskbarBehavior.Pending:
                    if (_taskbar.CheckIfDesktopShow(DesktopHandleSet))
                    {
                        #if DEBUG
                        Debug.WriteLine("try SHOW because of Desktop Show.");
                        #endif

                        BeforeShowBar();
                    }

                    break;
                case TaskbarBehavior.Show:
                    // #if DEBUG
                    // Debug.WriteLine(
                    //     $"try SHOW because of {info.Handle.ToString("x8")} Class Name: {info.Handle.GetClassName()}");
                    // #endif
                    BeforeShowBar();
                    break;
                case TaskbarBehavior.Hide:
                    if (info == _currentForegroundWindow) return;

                    // Some third-party taskbar plugins will be attached to the taskbar location, but not embedded in the taskbar or desktop.

                    if (!LastHideForegroundHandle.Contains(info.Handle)
                        && info.Rect.AreaCompare())
                        LastHideForegroundHandle.Push(info.Handle);

                    #if DEBUG
                    Debug.WriteLine(
                        $"HIDE because of {info.Handle.ToString("x8")} Class Name: {info.Handle.GetClassName()}");
                    #endif

                    _taskbar.HideTaskbar();
                    break;
            }

            _currentForegroundWindow = info;
        }

        private static void BeforeShowBar()
        {
            while (LastHideForegroundHandle.Count != 0)
            {
                if (_taskbar.CheckIfWindowShouldHideTaskbar(LastHideForegroundHandle.Peek()))
                {
                    #if DEBUG
                    Debug.WriteLine(
                        $"HIDE LAST because of {LastHideForegroundHandle.Peek().ToString("x8")} Class Name: {LastHideForegroundHandle.Peek().GetClassName()}");
                    #endif
                    return;
                }


                LastHideForegroundHandle.Pop();
            }

            _taskbar.ShowTaskar();
        }
    }
}
