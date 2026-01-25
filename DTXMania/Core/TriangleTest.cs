using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;
using Rectangle = SharpDX.Rectangle;

namespace DTXMania.Core;

internal sealed class TriangleTest : IGameClient
{
    private readonly GameContext context;
    private readonly Device device;
    private bool contentLoaded;
    private float timeSeconds;
    private string overlayState = "Running";
    private Font? overlayFont;

    public TriangleTest(GameContext context)
    {
        this.context = context;
        device = context.Device;
    }

    public void Initialize()
    {
        Trace.TraceInformation("TriangleTest.Initialize()");
    }

    public void LoadContent()
    {
        if (contentLoaded)
        {
            return;
        }

        contentLoaded = true;
        overlayState = "Running";

        device.SetRenderState(RenderState.Lighting, false);
        device.SetRenderState(RenderState.ZEnable, false);
        device.SetRenderState(RenderState.CullMode, Cull.None);

        var fontDesc = new FontDescription
        {
            FaceName = "Consolas",
            Height = 16,
            Weight = FontWeight.Normal,
            Quality = FontQuality.ClearType
        };
        overlayFont = new Font(device, fontDesc);
    }

    public void UnloadContent()
    {
        if (!contentLoaded)
        {
            return;
        }

        contentLoaded = false;
        if (overlayFont != null)
        {
            overlayFont.Dispose();
            overlayFont = null;
        }
    }

    public void Draw()
    {
        if (!contentLoaded)
        {
            return;
        }
        
        timeSeconds += 1f / 60f;

        device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, new ColorBGRA(20, 20, 30, 255), 1f, 0);
        device.BeginScene();

        var centerX = device.Viewport.Width * 0.5f;
        var centerY = device.Viewport.Height * 0.5f;
        var radius = Math.Min(device.Viewport.Width, device.Viewport.Height) * 0.25f;
        float angle = timeSeconds;

        var v1 = new TransformedColoredVertex(
            centerX + (float)Math.Cos(angle) * radius,
            centerY + (float)Math.Sin(angle) * radius,
            new ColorBGRA(255, 0, 0, 255));
        var v2 = new TransformedColoredVertex(
            centerX + (float)Math.Cos(angle + 2.09f) * radius,
            centerY + (float)Math.Sin(angle + 2.09f) * radius,
            new ColorBGRA(0, 255, 0, 255));
        var v3 = new TransformedColoredVertex(
            centerX + (float)Math.Cos(angle + 4.18f) * radius,
            centerY + (float)Math.Sin(angle + 4.18f) * radius,
            new ColorBGRA(0, 0, 255, 255));

        device.VertexFormat = TransformedColoredVertex.Format;
        device.DrawUserPrimitives(PrimitiveType.TriangleList, 1, new[] { v1, v2, v3 });

        if (overlayFont != null)
        {
            string text = $"TriangleTest\nState: {overlayState}\nF1: Toggle Game\nAlt+Enter: Fullscreen (if supported)";
            var rect = new Rectangle(8, 8, device.Viewport.Width - 16, device.Viewport.Height - 16);
            overlayFont.DrawText(null, text, rect, FontDrawFlags.Left | FontDrawFlags.Top, new ColorBGRA(240, 240, 240, 255));
        }

        device.EndScene();
    }

    public void OnExiting()
    {
    }

    public void OnDeviceLost()
    {
        overlayState = "DeviceLost";
        UnloadContent();
    }

    public void OnDeviceReset()
    {
        overlayState = "DeviceReset";
        LoadContent();
    }

    public void OnActivated()
    {
    }

    public void OnDeactivated()
    {
    }

    public void OnResizeEnd()
    {
    }

    public void ToggleFullscreen()
    {
        context.ToggleFullscreen();
    }

    public void OnKeyDown(Keys keyCode, bool isAlt)
    {
    }

    public void OnKeyUp(Keys keyCode)
    {
    }

    public void OnKeyChar(char keyChar)
    {
    }

    public void OnMouseMove(int x, int y)
    {
    }

    public void OnMouseDown(MouseButtons button, int x, int y)
    {
    }

    public void OnMouseUp(MouseButtons button, int x, int y)
    {
    }

    public void OnMouseWheel(int delta)
    {
    }

    public void OnMouseDoubleClick(MouseButtons button, int x, int y)
    {
    }

    public void Dispose()
    {
    }

    private readonly struct TransformedColoredVertex
    {
        public const VertexFormat Format = VertexFormat.Diffuse | VertexFormat.PositionRhw;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float Rhw;
        public readonly int Color;

        public TransformedColoredVertex(float x, float y, ColorBGRA color)
        {
            X = x;
            Y = y;
            Z = 0.5f;
            Rhw = 1.0f;
            Color = color.ToRgba();
        }
    }
}
