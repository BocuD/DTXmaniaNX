using System.Numerics;
using System.Runtime.InteropServices;
using DTXMania.Core;

namespace DTXMania.UI.Text;

internal static class Ime
{
    private const int ForcePosition = 0x0020;

    private const int ExcludeArea = 0x0080;

    public static void SetCaret(Vector2 position, float height)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        IntPtr window = CDTXMania.app?.maniaGl?.host?.GetWindowHandle() ?? IntPtr.Zero;
        if (window == IntPtr.Zero)
        {
            return;
        }

        IntPtr context = ImmGetContext(window);
        if (context == IntPtr.Zero)
        {
            return;
        }

        try
        {
            Point caret = new() { X = (int)position.X, Y = (int)position.Y };
            Rect line = new()
            {
                Left = caret.X,
                Top = caret.Y,
                Right = caret.X + 1,
                Bottom = caret.Y + (int)MathF.Max(height, 1f)
            };

            CompositionForm composition = new() { Style = ForcePosition, CurrentPos = caret, Area = line };
            ImmSetCompositionWindow(context, ref composition);

            CandidateForm candidate = new() { Index = 0, Style = ExcludeArea, CurrentPos = caret, Area = line };
            ImmSetCandidateWindow(context, ref candidate);
        }
        finally
        {
            ImmReleaseContext(window, context);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CompositionForm
    {
        public int Style;
        public Point CurrentPos;
        public Rect Area;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CandidateForm
    {
        public int Index;
        public int Style;
        public Point CurrentPos;
        public Rect Area;
    }

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetContext(IntPtr window);

    [DllImport("imm32.dll")]
    private static extern bool ImmReleaseContext(IntPtr window, IntPtr context);

    [DllImport("imm32.dll")]
    private static extern bool ImmSetCompositionWindow(IntPtr context, ref CompositionForm form);

    [DllImport("imm32.dll")]
    private static extern bool ImmSetCandidateWindow(IntPtr context, ref CandidateForm form);
}
