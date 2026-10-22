using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

public class ClassicDrumsChipFire : UIDrawable, IPerfFire
{
    private const int FireMax = 64;
    private const int StarMax = 240;
    private const int SplashMax = 8;
    private const int WaveMax = 20;

    private struct Fire
    {
        public bool inUse;
        public int lane;
        public CCounter counter;
        public float angle;
        public float size;
    }

    private struct Particle
    {
        public bool inUse;
        public int lane;
        public CCounter counter;
        public int lastValue;
        public float x;
        public float y;
        public float speedX;
        public float speedY;
        public float dragX;
        public float dragY;
        public float gravity;
        public float radius;
    }

    private struct Splash
    {
        public bool inUse;
        public int lane;
        public CCounter counter;
        public int lastValue;
        public float xLeft;
        public float xRight;
        public float y;
        public float speedX;
        public float speedY;
    }

    private struct Wave
    {
        public bool inUse;
        public int lane;
        public CCounter counter;
        public float tiltX;
        public float tiltY;
        public float radius;
    }

    private readonly Fire[] fires = new Fire[FireMax];
    private readonly Particle[] stars = new Particle[StarMax];
    private readonly Splash[] splashes = new Splash[SplashMax];
    private readonly Wave[] bigWaves = new Wave[WaveMax];
    private readonly Wave[] smallWaves = new Wave[WaveMax];

    private bool bigWaveFlip;
    private bool smallWaveFlip;
    private int judgeLineOffset;

    public int iPosY { get; set; }

    //LC HH SD BD HT LT FT CY LP RD LBD
    private static readonly float[] waveMinTilt = [-130f, -126f, -120f, -118f, -110f, -108f, -103f, -97f, -85f, -91f, -91f];
    private static readonly float[] waveMaxTilt = [70f, 72f, 77f, 84f, 89f, 91f, 99f, 107f, 117f, 112f, 112f];
    private static readonly int[] noteLeft = [448 + 90, 60 + 10, 106 + 20, 0, 160 + 30, 206 + 40, 252 + 50, 298 + 60, 550 + 110, 362 + 70, 400 + 80];
    private static readonly int[] noteWidth = [64, 46, 54, 60, 46, 46, 46, 60, 48, 48, 48];


    private static readonly string[] textureLanes = ["LC", "HH", "SD", "BD", "HT", "LT", "FT", "CY", "LP", "RD"];

    private readonly BaseTexture?[] fireTextures = new BaseTexture?[textureLanes.Length];
    private readonly BaseTexture?[] starTextures = new BaseTexture?[textureLanes.Length];
    private readonly BaseTexture? fireSheet;
    private readonly BaseTexture? bonusFire;
    private readonly BaseTexture? bigWave;
    private readonly BaseTexture? smallWave;
    private readonly BaseTexture? notes;

    public ClassicDrumsChipFire()
    {
        dontSerialize = true;

        for (int i = 0; i < textureLanes.Length; i++)
        {
            fireTextures[i] = Load($@"Graphics\ScreenPlayDrums chip fire_{textureLanes[i]}.png");
            starTextures[i] = Load($@"Graphics\ScreenPlayDrums chip star_{textureLanes[i]}.png");
        }

        if (CDTXMania.ConfigIni.nExplosionFrames >= 2)
        {
            fireSheet = Load(@"Graphics\ScreenPlayDrums chip fire.png");
        }

        bonusFire = Load(@"Graphics\ScreenPlayDrums chip fire_Bonus.png");
        bigWave = Load(@"Graphics\ScreenPlayDrums chip wave.png");
        smallWave = Load(@"Graphics\ScreenPlayDrums chip wave2.png");
        notes = Load(@"Graphics\7_Chips_drums.png");
    }

    private static BaseTexture? Load(string path)
    {
        BaseTexture? texture = BaseTexture.LoadFromPath(CSkin.Path(path));
        if (texture != null)
        {
            texture.blendMode = BlendMode.Additive;
        }

        return texture;
    }

    //the fire art is drawn 320px right of this
    private static float LaneX(int lane) => DrumLanes.Centre((ELane)lane) - 320;

    public void Start(ELane lane, bool fillIn, bool bigWaves, bool smallWaves, int judgeLineOffset = 0, bool display = true)
    {
        int index = (int)lane;
        EType effect = CDTXMania.ConfigIni.eAttackEffect.Drums;
        this.judgeLineOffset = judgeLineOffset;

        if (effect != EType.D)
        {
            StartFire(index, display);
        }

        if (display && effect is EType.A or EType.B)
        {
            for (int i = 0; i < 16; i++)
            {
                int angle = CDTXMania.Random.Next(360);
                SpawnStar(index, 40, 0.9 * Math.Cos(angle * Math.PI / 180), 0.9 * Math.Sin(angle * Math.PI / 180) - 0.1,
                    1f, 1.01f, 0.0204f, 0.3f);
            }
        }

        if (display && effect == EType.A)
        {
            StartSplash(index);
        }

        if (fillIn)
        {
            for (int i = 0; i < 16; i++)
            {
                int angle = CDTXMania.Random.Next(360);
                double spread = 0.9 + CDTXMania.Random.Next(40) / 100.0;
                SpawnStar(index, 100, spread * Math.Cos(angle * Math.PI / 180), spread * (Math.Sin(angle * Math.PI / 180) - 0.2),
                    0.995f, 0.995f, 0.00355f, 0.5f);
            }
        }

        if (bigWaves)
        {
            for (int i = 0; i < 4; i++)
            {
                SpawnWave(bigWaves: true, index, 10, i * 10);
            }
        }

        if (smallWaves)
        {
            SpawnWave(bigWaves: false, index, 8, 0);
        }
    }

    //two fires per hit. A new hit on a lane replaces its fires
    private void StartFire(int lane, bool display)
    {
        for (int i = 0; i < FireMax; i++)
        {
            if (fires[i].inUse && fires[i].lane == lane)
            {
                fires[i].counter.tStop();
                fires[i].inUse = false;
            }
        }

        float startAngle = CDTXMania.Random.Next(360);
        int frames = CDTXMania.ConfigIni.nExplosionFrames;

        for (int n = 0; n < 2; n++)
        {
            int i = Array.FindIndex(fires, f => !f.inUse);
            if (i < 0)
            {
                return;
            }

            fires[i] = new Fire
            {
                inUse = true,
                lane = lane,
                counter = frames == 1
                    ? new CCounter(0, 70, 3, CDTXMania.Timer)
                    : new CCounter(0, frames - 1, CDTXMania.ConfigIni.nExplosionInterval, CDTXMania.Timer),
                angle = CConversion.DegreeToRadian(startAngle + n * 90f),
                size = display ? 1f : 0f
            };
        }
    }

    private void SpawnStar(int lane, int duration, double speedX, double speedY, float dragX, float dragY, float gravity, float minRadius)
    {
        int i = Array.FindIndex(stars, s => !s.inUse);
        if (i < 0)
        {
            return;
        }

        stars[i] = new Particle
        {
            inUse = true,
            lane = lane,
            counter = new CCounter(0, duration, 7, CDTXMania.Timer),
            x = LaneX(lane) + 320,
            y = iPosY + 350 + judgeLineOffset - 170f,
            speedX = (float)speedX,
            speedY = (float)speedY,
            dragX = dragX,
            dragY = dragY,
            gravity = gravity,
            radius = minRadius + CDTXMania.Random.Next(30) / 100f
        };
    }

    //two halves of the chip fly apart, left and right
    private void StartSplash(int lane)
    {
        int i = Array.FindIndex(splashes, s => !s.inUse);
        if (i < 0)
        {
            return;
        }

        float x = LaneX(lane) + noteWidth[lane] + 312;
        const double spread = 0.91;
        const double angle = Math.PI / 180;

        splashes[i] = new Splash
        {
            inUse = true,
            lane = lane,
            counter = new CCounter(0, 44, 10, CDTXMania.Timer),
            xLeft = x + lane switch { 0 or 3 or 7 => 0, 1 => 20, _ => 10 },
            xRight = x,
            y = iPosY + 359 + judgeLineOffset - 170f,
            speedX = (float)(spread * Math.Cos(angle) + 0.3),
            speedY = (float)(spread * (Math.Sin(angle) - 0.8))
        };
    }

    private void SpawnWave(bool bigWaves, int lane, int speedMs, int startValue)
    {
        Wave[] waves = bigWaves ? this.bigWaves : smallWaves;
        int i = Array.FindIndex(waves, w => !w.inUse);
        if (i < 0)
        {
            return;
        }

        //waves alternate between leaning one way and the other
        bool flip = bigWaves ? bigWaveFlip : smallWaveFlip;
        CCounter counter = new(0, 100, speedMs, CDTXMania.Timer) { nCurrentValue = startValue };

        waves[i] = new Wave
        {
            inUse = true,
            lane = lane,
            counter = counter,
            radius = (20 - CDTXMania.Random.Next(40) + 100) / 100f,
            tiltX = CConversion.DegreeToRadian((float)(CDTXMania.Random.Next(100) * 50 / 100.0 + 30.0)),
            tiltY = CConversion.DegreeToRadian(flip
                ? waveMinTilt[lane] + CDTXMania.Random.Next(30)
                : waveMaxTilt[lane] - CDTXMania.Random.Next(30))
        };

        if (bigWaves)
        {
            bigWaveFlip = !bigWaveFlip;
        }
        else
        {
            smallWaveFlip = !smallWaveFlip;
        }
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        DrawStars();

        if (CDTXMania.ConfigIni.eAttackEffect.Drums == EType.A)
        {
            DrawSplashes();
        }

        DrawFires();
        DrawBigWaves();
        DrawSmallWaves();
    }

    //returns false once the counter has finished; the effect still draws that frame
    private static bool Advance(ref bool inUse, CCounter counter)
    {
        counter.tUpdate();
        if (counter.bReachedEndValue)
        {
            counter.tStop();
            inUse = false;
        }

        return inUse;
    }

    private void DrawStars()
    {
        for (int i = 0; i < StarMax; i++)
        {
            ref Particle star = ref stars[i];
            if (!star.inUse)
            {
                continue;
            }

            star.lastValue = star.counter.nCurrentValue;
            Advance(ref star.inUse, star.counter);

            for (int n = star.lastValue; n < star.counter.nCurrentValue; n++)
            {
                star.x += star.speedX;
                star.y -= star.speedY;
                star.speedX *= star.dragX;
                star.speedY *= star.dragY;
                star.speedY -= star.gravity;
            }

            float scale = (float)(star.radius * Math.Cos(Math.PI / 2 * star.counter.nCurrentValue / 100.0));
            Matrix4x4 matrix = Matrix4x4.CreateScale(scale, scale, 1f)
                               * Matrix4x4.CreateTranslation(star.x - GameWindowSize.Width / 2f, -(star.y - GameWindowSize.Height / 2f), 0f);

            if (star.lane < starTextures.Length)
            {
                starTextures[star.lane]?.tDraw3D(matrix);
            }
        }
    }

    private void DrawSplashes()
    {
        Color4 color = new(1f, 1f, 1f, 120 / 255f);

        for (int i = 0; i < SplashMax; i++)
        {
            ref Splash splash = ref splashes[i];
            if (!splash.inUse)
            {
                continue;
            }

            splash.lastValue = splash.counter.nCurrentValue;
            Advance(ref splash.inUse, splash.counter);

            for (int n = splash.lastValue; n < splash.counter.nCurrentValue; n++)
            {
                splash.xLeft += (float)(splash.speedX * Math.Cos(120.0 * Math.PI / 180.0) * 5);
                splash.xRight += (float)(splash.speedX * Math.Cos(60.0 * Math.PI / 180.0) * 5);
                splash.y += (float)(splash.speedY * Math.Sin(60.0 * Math.PI / 180.0) * 10.0f - Math.Exp(0.031f * 2.0f) / 2.0f);
                splash.speedX *= 0.995f;
                splash.speedY += 0.031f;
            }

            if (notes == null)
            {
                continue;
            }

            float spin = 0.09f * splash.counter.nCurrentValue;
            float y = -(splash.y + judgeLineOffset - GameWindowSize.Height / 2f);
            RectangleF half = new(noteLeft[splash.lane], 640, (noteWidth[splash.lane] + 10) / 2, 64);

            notes.tDraw3D(Matrix4x4.CreateRotationZ(spin) * Matrix4x4.CreateTranslation(splash.xLeft - 50f - GameWindowSize.Width / 2f, y, 0f), half, color);
            notes.tDraw3D(Matrix4x4.CreateRotationZ(-spin) * Matrix4x4.CreateTranslation(splash.xRight - 50f - GameWindowSize.Width / 2f, y, 0f), half, color);
        }
    }

    private void DrawFires()
    {
        bool chorus = CDTXMania.stagePerfDrumsScreen.bChorusSection;
        float baseY = iPosY + judgeLineOffset;

        for (int i = 0; i < FireMax; i++)
        {
            ref Fire fire = ref fires[i];
            if (!fire.inUse)
            {
                continue;
            }

            Advance(ref fire.inUse, fire.counter);
            int value = fire.counter.nCurrentValue;
            float x = LaneX(fire.lane);

            //one sprite that grows and drifts outwards
            if (CDTXMania.ConfigIni.nExplosionFrames <= 1)
            {
                float progress = value / 70f;
                float grow = (float)(0.2 + 0.8 * Math.Cos(value / 50.0 * Math.PI / 2)) * fire.size;
                float drift = (float)(0.8 * Math.Sin(progress * Math.PI / 2)) * fire.size;

                Matrix4x4 matrix = Matrix4x4.CreateScale(0.2f + grow, 0.2f + grow, 1f)
                                   * Matrix4x4.CreateTranslation(x + MathF.Cos(fire.angle) * drift - 320f,
                                       -(baseY + MathF.Sin(fire.angle) * drift - 170f), 0f);

                BaseTexture? texture = fire.lane < fireTextures.Length ? fireTextures[fire.lane] : null;
                if (texture != null)
                {
                    texture.tDraw3D(matrix);
                    if (chorus)
                    {
                        bonusFire?.tDraw3D(matrix);
                    }
                }

                continue;
            }

            //an animation sheet: one row per lane, the bonus row at 10
            if (fireSheet != null)
            {
                int width = CDTXMania.ConfigIni.nExplosionWidgh;
                int height = CDTXMania.ConfigIni.nExplosionHeight;
                Matrix4x4 matrix = Matrix4x4.CreateTranslation(x - 320f, -(baseY - 170f), 0f);

                fireSheet.tDraw3D(matrix, new RectangleF(width * value, fire.lane * height, width, height), Color4.White);
                if (chorus && bonusFire != null)
                {
                    fireSheet.tDraw3D(matrix, new RectangleF(width * value, 10 * height, width, height), Color4.White);
                }
            }
        }
    }

    private Matrix4x4 WaveTranslation(int lane) => Matrix4x4.CreateTranslation(LaneX(lane) + 280 - GameWindowSize.Width / 2f,
        -(iPosY + judgeLineOffset + 200 - GameWindowSize.Height / 2f), 0f);

    private void DrawBigWaves()
    {
        for (int i = 0; i < WaveMax; i++)
        {
            ref Wave wave = ref bigWaves[i];
            if (!wave.inUse)
            {
                continue;
            }

            Advance(ref wave.inUse, wave.counter);
            if (wave.counter.nCurrentValue < 0 || bigWave == null)
            {
                continue;
            }

            float p = wave.counter.nCurrentValue / 100f;
            float scale = p < 0.4f ? 2.5f * p
                : p < 0.8f ? (float)(1.0 + 10.1 * (1.0 - Math.Cos(Math.PI / 2 * (p - 0.4) * 2.5)))
                : 11.1f + 12.5f * (p - 0.8f);
            float alpha = p < 0.75f ? 55 : 55f * (1f - p) / 0.25f;

            Matrix4x4 matrix = Matrix4x4.CreateScale(scale * wave.radius, scale * wave.radius, 1f)
                               * Matrix4x4.CreateRotationZ(CConversion.DegreeToRadian(60f * p))
                               * Matrix4x4.CreateRotationX(wave.tiltX)
                               * Matrix4x4.CreateRotationY(wave.tiltY)
                               * WaveTranslation(wave.lane);

            bigWave.tDraw3D(matrix, new Color4(1f, 1f, 1f, alpha / 255f));
        }
    }

    private void DrawSmallWaves()
    {
        for (int i = 0; i < WaveMax; i++)
        {
            ref Wave wave = ref smallWaves[i];
            if (!wave.inUse)
            {
                continue;
            }

            Advance(ref wave.inUse, wave.counter);
            if (wave.counter.nCurrentValue < 0 || smallWave == null)
            {
                continue;
            }

            float p = wave.counter.nCurrentValue / 100f;
            float scale = 14f * p;
            float alpha = p < 0.5f ? 155 : 155f * (1f - p);

            Matrix4x4 matrix = Matrix4x4.CreateScale(scale * wave.radius, scale * wave.radius, 1f)
                               * Matrix4x4.CreateRotationX(wave.tiltX)
                               * Matrix4x4.CreateRotationY(wave.tiltY)
                               * WaveTranslation(wave.lane);

            smallWave.tDraw3D(matrix, new Color4(1f, 1f, 1f, alpha / 255f));
        }
    }

    public override void Dispose()
    {
        foreach (BaseTexture? texture in fireTextures) texture?.Dispose();
        foreach (BaseTexture? texture in starTextures) texture?.Dispose();
        fireSheet?.Dispose();
        bonusFire?.Dispose();
        bigWave?.Dispose();
        smallWave?.Dispose();
        notes?.Dispose();

        base.Dispose();
    }
}
