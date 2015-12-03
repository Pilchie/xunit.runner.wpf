using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Xml.Linq;

namespace xunit.runner.wpf
{
    internal static partial class Storage
    {
        private static class WindowPlacement
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct POINT
            {
                public int x;
                public int y;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct WINDOWPLACEMENT
            {
                public int length;
                public int flags;
                public int showCmd;
                public POINT ptMinPosition;
                public POINT ptMaxPosition;
                public RECT rcNormalPosition;
            }

            private const int SW_SHOWNORMAL = 1;
            private const int SW_SHOWMINIMIZED = 2;

            [DllImport("user32.dll")]
            private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

            [DllImport("user32.dll")]
            private static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

            private const string WindowPlacementElementName = "window_placement";
            private const string ShowCommandElementName = "show_command";
            private const string MinPositionElementName = "min_position";
            private const string MaxPositionElementName = "max_position";
            private const string NormalPositionElementName = "normal_position";
            private const string XAttributeName = "x";
            private const string YAttributeName = "y";
            private const string LeftAttributeName = "left";
            private const string TopAttributeName = "top";
            private const string RightAttributeName = "right";
            private const string BottomAttributeName = "bottom";

            public static void Restore(Window window, XElement xml)
            {
                var placement = new WINDOWPLACEMENT();

                placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                placement.flags = 0;

                placement.showCmd = (int)xml.Element(ShowCommandElementName);
                placement.ptMinPosition.x = (int)xml.Element(MinPositionElementName).Attribute(XAttributeName);
                placement.ptMinPosition.y = (int)xml.Element(MinPositionElementName).Attribute(YAttributeName);
                placement.ptMaxPosition.x = (int)xml.Element(MaxPositionElementName).Attribute(XAttributeName);
                placement.ptMaxPosition.y = (int)xml.Element(MaxPositionElementName).Attribute(YAttributeName);
                placement.rcNormalPosition.left = (int)xml.Element(NormalPositionElementName).Attribute(LeftAttributeName);
                placement.rcNormalPosition.top = (int)xml.Element(NormalPositionElementName).Attribute(TopAttributeName);
                placement.rcNormalPosition.right = (int)xml.Element(NormalPositionElementName).Attribute(RightAttributeName);
                placement.rcNormalPosition.bottom = (int)xml.Element(NormalPositionElementName).Attribute(BottomAttributeName);

                var windowInteropHelper = new WindowInteropHelper(window);
                SetWindowPlacement(windowInteropHelper.Handle, ref placement);
            }

            public static XElement Save(Window window)
            {
                var windowInteropHelper = new WindowInteropHelper(window);
                var placement = new WINDOWPLACEMENT();
                GetWindowPlacement(windowInteropHelper.Handle, out placement);

                return
                    new XElement(WindowPlacementElementName,
                        new XElement(ShowCommandElementName, (placement.showCmd == SW_SHOWMINIMIZED ? SW_SHOWNORMAL : placement.showCmd)),
                        new XElement(MinPositionElementName,
                            new XAttribute(XAttributeName, placement.ptMinPosition.x),
                            new XAttribute(YAttributeName, placement.ptMinPosition.y)),
                        new XElement(MaxPositionElementName,
                            new XAttribute(XAttributeName, placement.ptMaxPosition.x),
                            new XAttribute(YAttributeName, placement.ptMaxPosition.y)),
                        new XElement(NormalPositionElementName,
                            new XAttribute(LeftAttributeName, placement.rcNormalPosition.left),
                            new XAttribute(TopAttributeName, placement.rcNormalPosition.top),
                            new XAttribute(RightAttributeName, placement.rcNormalPosition.right),
                            new XAttribute(BottomAttributeName, placement.rcNormalPosition.bottom)));
            }
        }
    }
}
