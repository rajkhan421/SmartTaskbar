﻿using static SmartTaskbar.SafeNativeMethods;

namespace SmartTaskbar;

internal static class TaskbarHelper
{
    /// <summary>
    ///     Main Taskbar Class Name
    /// </summary>
    public const string MainTaskbarClassName = "Shell_TrayWnd";

    /// <summary>
    ///     Initialize taskbar information
    /// </summary>
    /// <returns></returns>
    internal static TaskbarInfo InitTaskbar()
    {
        // Find the main taskbar handle
        var handle = FindWindow(MainTaskbarClassName, null);

        // Get taskbar window rectangle
        _ = GetWindowRect(handle, out var rect);

        // Currently, the taskbar of Windows 11 is only at the bottom,
        // so you only need to calculate the difference between the taskbar and the bottom of the screen
        // to get the rectangle when the taskbar is fully displayed.
        var heightΔ = rect.bottom - Screen.PrimaryScreen.Bounds.Bottom;

        return new TaskbarInfo(handle,
                               new TagRect
                               {
                                   left = rect.left, top = rect.top - heightΔ, right = rect.right,
                                   bottom = rect.bottom - heightΔ
                               });
    }

    #region Show Or Hide Taskbar

    private const uint BarFlag = 0x05D1;

    private const uint MonitorDefaultToPrimary = 1;
    private static readonly TagPoint PointZero = new() {x = 0, y = 0};

    /// <summary>
    ///     Hide the taskbar, in auto-hide mode
    /// </summary>
    /// <param name="taskbar"></param>
    internal static void HideTaskbar(this in TaskbarInfo taskbar)
    {
        // Get taskbar window rectangle
        _ = GetWindowRect(taskbar.Handle, out var rect);

        // Send a message to hide the taskbar, if taskbar is display
        if (rect.bottom == taskbar.Rect.bottom)
            PostMessage(taskbar.Handle,
                        BarFlag,
                        IntPtr.Zero,
                        IntPtr.Zero);
    }

    /// <summary>
    ///     Show the taskbar, in auto-hide mode
    /// </summary>
    /// <param name="taskbar"></param>
    /// <param name="monitor"></param>
    internal static void ShowTaskar(this in TaskbarInfo taskbar)
    {
        // Get taskbar window rectangle
        _ = GetWindowRect(taskbar.Handle, out var rect);

        // Send a message to show the taskbar, if taskbar is hidden
        if (rect.bottom != taskbar.Rect.bottom)
            PostMessage(
                taskbar.Handle,
                BarFlag,
                (IntPtr) 1,
                MonitorFromPoint(PointZero, MonitorDefaultToPrimary));
    }

    #endregion

    #region IsMouseOverTaskbar

    private const uint GaParent = 1;

    /// <summary>
    ///     Determine if the current mouse is above the taskbar
    /// </summary>
    /// <param name="taskbar"></param>
    /// <returns></returns>
    public static bool IsMouseOverTaskbar(this in TaskbarInfo taskbar)
    {
        // Get mouse coordinates
        _ = GetCursorPos(out var point);
        // use the point to get the window below it
        // this method is the fastest
        var currentHandle = WindowFromPoint(point);

        // If the current mouse position is not in the taskbar (in the fully displayed state),
        // it means that the mouse cannot be above the taskbar.
        if (point.y < taskbar.Rect.top
            || point.x > taskbar.Rect.right
            || point.x < taskbar.Rect.left
            || point.y > taskbar.Rect.bottom)
        {
            OnMouseOverLeftCorner?.Invoke(null, false);
            return false;
        }

        // Traverse to get the parent of the current window.
        // If its parent is the taskbar, it means that the mouse is on the taskbar.
        // Otherwise, all the way to the highest level, the desktop, jump out of the loop.

        // Under normal circumstances, there will be no more than three loops,
        // usually the first one is the taskbar or desktop.
        // So don't worry too much about performance.
        var desktopHandle = GetDesktopWindow();
        while (currentHandle != desktopHandle)
        {
            if (taskbar.Handle == currentHandle)
            {
                if (point.x <= taskbar.Rect.left + taskbar.Rect.bottom - taskbar.Rect.top)
                    OnMouseOverLeftCorner?.Invoke(null, true);
                return true;
            }

            currentHandle = GetAncestor(currentHandle, GaParent);
        }

        OnMouseOverLeftCorner?.Invoke(null, false);
        return false;
    }

    public static event EventHandler<bool>? OnMouseOverLeftCorner;

    #endregion
}