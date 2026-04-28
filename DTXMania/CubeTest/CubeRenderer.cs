using System.Numerics;
using DTXMania.Core.OpenGL;
using DTXMania.UI.Drawable;
using Hexa.NET.GLFW;
using Silk.NET.OpenGL;

namespace DTXMania.CubeTest;

internal sealed unsafe class CubeRenderer : OpenGlGame
{
    public static UIDrawable ui = new UIGroup("Root node");
    
    private static readonly float[] Vertices =
    [
        -0.5f, -0.5f, -0.5f, 1.0f, 0.3f, 0.3f,
         0.5f, -0.5f, -0.5f, 1.0f, 0.3f, 0.3f,
         0.5f,  0.5f, -0.5f, 1.0f, 0.3f, 0.3f,
         0.5f,  0.5f, -0.5f, 1.0f, 0.3f, 0.3f,
        -0.5f,  0.5f, -0.5f, 1.0f, 0.3f, 0.3f,
        -0.5f, -0.5f, -0.5f, 1.0f, 0.3f, 0.3f,

        -0.5f, -0.5f,  0.5f, 0.3f, 1.0f, 0.3f,
         0.5f, -0.5f,  0.5f, 0.3f, 1.0f, 0.3f,
         0.5f,  0.5f,  0.5f, 0.3f, 1.0f, 0.3f,
         0.5f,  0.5f,  0.5f, 0.3f, 1.0f, 0.3f,
        -0.5f,  0.5f,  0.5f, 0.3f, 1.0f, 0.3f,
        -0.5f, -0.5f,  0.5f, 0.3f, 1.0f, 0.3f,

        -0.5f,  0.5f,  0.5f, 0.3f, 0.4f, 1.0f,
        -0.5f,  0.5f, -0.5f, 0.3f, 0.4f, 1.0f,
        -0.5f, -0.5f, -0.5f, 0.3f, 0.4f, 1.0f,
        -0.5f, -0.5f, -0.5f, 0.3f, 0.4f, 1.0f,
        -0.5f, -0.5f,  0.5f, 0.3f, 0.4f, 1.0f,
        -0.5f,  0.5f,  0.5f, 0.3f, 0.4f, 1.0f,

         0.5f,  0.5f,  0.5f, 1.0f, 0.9f, 0.3f,
         0.5f,  0.5f, -0.5f, 1.0f, 0.9f, 0.3f,
         0.5f, -0.5f, -0.5f, 1.0f, 0.9f, 0.3f,
         0.5f, -0.5f, -0.5f, 1.0f, 0.9f, 0.3f,
         0.5f, -0.5f,  0.5f, 1.0f, 0.9f, 0.3f,
         0.5f,  0.5f,  0.5f, 1.0f, 0.9f, 0.3f,

        -0.5f, -0.5f, -0.5f, 0.8f, 0.3f, 1.0f,
         0.5f, -0.5f, -0.5f, 0.8f, 0.3f, 1.0f,
         0.5f, -0.5f,  0.5f, 0.8f, 0.3f, 1.0f,
         0.5f, -0.5f,  0.5f, 0.8f, 0.3f, 1.0f,
        -0.5f, -0.5f,  0.5f, 0.8f, 0.3f, 1.0f,
        -0.5f, -0.5f, -0.5f, 0.8f, 0.3f, 1.0f,

        -0.5f,  0.5f, -0.5f, 0.3f, 1.0f, 1.0f,
         0.5f,  0.5f, -0.5f, 0.3f, 1.0f, 1.0f,
         0.5f,  0.5f,  0.5f, 0.3f, 1.0f, 1.0f,
         0.5f,  0.5f,  0.5f, 0.3f, 1.0f, 1.0f,
        -0.5f,  0.5f,  0.5f, 0.3f, 1.0f, 1.0f,
        -0.5f,  0.5f, -0.5f, 0.3f, 1.0f, 1.0f
    ];

    private const string VertexShaderSource = """
        #version 330 core
        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aColor;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec3 vColor;

        void main()
        {
            vColor = aColor;
            gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core
        in vec3 vColor;

        out vec4 fragColor;

        void main()
        {
            fragColor = vec4(vColor, 1.0);
        }
        """;

    private uint _program;
    private uint _vao;
    private uint _vbo;
    private int _modelLocation;
    private int _viewLocation;
    private int _projectionLocation;

    protected override void CreateSharedResources()
    {
        _program = CreateProgram(VertexShaderSource, FragmentShaderSource);
        _vbo = Gl.GenBuffer();
        _modelLocation = Gl.GetUniformLocation(_program, "uModel");
        _viewLocation = Gl.GetUniformLocation(_program, "uView");
        _projectionLocation = Gl.GetUniformLocation(_program, "uProjection");

        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        fixed (float* vertexPtr = Vertices)
        {
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(float)), vertexPtr, BufferUsageARB.StaticDraw);
        }

        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    protected override void CreateContextResources()
    {
        _vao = Gl.GenVertexArray();

        Gl.BindVertexArray(_vao);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        const uint stride = 6 * sizeof(float);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        Gl.EnableVertexAttribArray(0);
        Gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        Gl.EnableVertexAttribArray(1);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindVertexArray(0);
    }

    public override void Init()
    {
        
    }

    public override void Update(float deltaTime, double totalTime)
    {
        
    }

    public override void Render(int width, int height, double totalTime)
    {
        Gl.Enable(GLEnum.DepthTest);
        Gl.Viewport(0, 0, (uint)Math.Max(width, 1), (uint)Math.Max(height, 1));
        Gl.ClearColor(0.08f, 0.09f, 0.12f, 1f);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        float aspectRatio = width / (float)Math.Max(height, 1);
        Span<float> model = stackalloc float[16];
        Span<float> view = stackalloc float[16];
        Span<float> projection = stackalloc float[16];

        MatrixUtils.CreateRotationY(model, (float)totalTime * 0.9f);
        MatrixUtils.Multiply(model, MatrixUtils.CreateRotationX((float)totalTime * 0.55f), model);
        view = MatrixUtils.CreateTranslation(0f, 0f, -3f);
        projection = MatrixUtils.CreatePerspectiveFieldOfView(MathF.PI / 4f, aspectRatio, 0.1f, 100f);

        Gl.UseProgram(_program);
        fixed (float* modelPtr = model)
        fixed (float* viewPtr = view)
        fixed (float* projectionPtr = projection)
        {
            Gl.UniformMatrix4(_modelLocation, 1, false, modelPtr);
            Gl.UniformMatrix4(_viewLocation, 1, false, viewPtr);
            Gl.UniformMatrix4(_projectionLocation, 1, false, projectionPtr);
        }

        Gl.BindVertexArray(_vao);
        Gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        Gl.BindVertexArray(0);
        
        ui.Draw(Matrix4x4.Identity);
    }

    protected override void DestroyContextResources()
    {
        if (_vao != 0)
        {
            Gl.DeleteVertexArray(_vao);
            _vao = 0;
        }
    }

    protected override void DestroySharedResources()
    {
        if (_vbo != 0)
        {
            Gl.DeleteBuffer(_vbo);
            _vbo = 0;
        }

        if (_program != 0)
        {
            Gl.DeleteProgram(_program);
            _program = 0;
        }
    }

    private uint CreateProgram(string vertexShaderSource, string fragmentShaderSource)
    {
        uint vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderSource);
        uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderSource);

        uint program = Gl.CreateProgram();
        Gl.AttachShader(program, vertexShader);
        Gl.AttachShader(program, fragmentShader);
        Gl.LinkProgram(program);

        Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            throw new InvalidOperationException($"Program link failed: {Gl.GetProgramInfoLog(program)}");
        }

        Gl.DetachShader(program, vertexShader);
        Gl.DetachShader(program, fragmentShader);
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);
        return program;
    }

    private uint CompileShader(ShaderType shaderType, string source)
    {
        uint shader = Gl.CreateShader(shaderType);
        Gl.ShaderSource(shader, source);
        Gl.CompileShader(shader);

        Gl.GetShader(shader, ShaderParameterName.CompileStatus, out int compileStatus);
        if (compileStatus == 0)
        {
            throw new InvalidOperationException($"{shaderType} compilation failed: {Gl.GetShaderInfoLog(shader)}");
        }

        return shader;
    }

    public override string name => "CubeTest";

    public override void KeyDown(GlfwKey key, GlfwMod mods)
    {
        
    }

    public override void KeyUp(GlfwKey key, GlfwMod mods)
    {
        
    }
}
