using System.Diagnostics;
using System.Numerics;
using System.Windows.Forms;
using DTXMania.Core.Video;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using DTXMania.UI.Skin;
using DTXMania.Updater;
using FDK;

namespace DTXMania.Core;

internal partial class CDTXMania
{
    private bool startupFinished = false;
    private readonly List<(string name, Action initialize)> initializers = [];
    
    void AddInitializer(string name, Action action)
    {
        initializers.Add((name, action));
    }
    
    void RunInitializer((string name, Action initializer) initializer)
    {
        try
        {
            Trace.TraceInformation($"Initializing {initializer.name}");
            initializer.initializer();
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to initialize {initializer.name}: {e}\n{e.StackTrace}");
            MessageBox.Show($"Failed to initialize {initializer.name}: {e}\n{e.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    private void StartupTick()
    {
        //tick through initializers and initialize them one by one
        if (initializers.Count > 0)
        {
            var initializer = initializers.First();
            initializers.RemoveAt(0);
            RunInitializer(initializer);
        }
        
        if (initializers.Count == 0)
        {
            startupFinished = true;

            Trace.TraceInformation("Finished game initialization");
            
            Trace.TraceInformation("----------------------");
            Trace.TraceInformation("■ Startup");
        
            SongDb.StartScan();
        }
    }

    private void SetupInitializers()
    {
        AddInitializer("Skin", () =>
        {
            try
            {
                Trace.Indent();
                Trace.TraceInformation("Initializing Skin Manager");
                SkinManager = new SkinManager();

                Trace.TraceInformation("Initializing Legacy Skin Handler");
                Skin = new CSkin(ConfigIni.strSystemSkinSubfolderFullName, ConfigIni.bUseBoxDefSkin);
                ConfigIni.strSystemSkinSubfolderFullName =
                    Skin.GetCurrentSkinSubfolderFullName(true); // 旧指定のSkinフォルダが消滅していた場合に備える
            }
            finally
            {
                Trace.Unindent();
            }
        });
        
        AddInitializer("StageManager", () => StageManager = new StageManager());
        
        AddInitializer("LoadingStage", () =>
        {
            StageManager.LoadInitialStage();
        });

        AddInitializer("FFmpeg", FFmpegCore.Initialize);

        AddInitializer("Timer", () =>
        {
            Timer = new CTimer(CTimer.EType.MultiMedia); 
            Random = new Random((int)Timer.nシステム時刻);
        });

        AddInitializer("FPS Counter", () => { FPS = new CFPS(); });

        AddInitializer("Character Console", () =>
        {
            actDisplayString = new CCharacterConsole();
            actDisplayString.OnActivate();
        });

        AddInitializer("Input Manager (DirectInput, MIDI)", () =>
        {
            InputManager = new CInputManager(maniaGl.host.GetWindowHandle());
            foreach (IInputDevice device in InputManager.listInputDevices)
            {
                if (device.eInputDeviceType == EInputDeviceType.Joystick &&
                    !ConfigIni.joystickDict.ContainsValue(device.GUID))
                {
                    int key = 0;
                    while (ConfigIni.joystickDict.ContainsKey(key))
                    {
                        key++;
                    }

                    ConfigIni.joystickDict.Add(key, device.GUID);
                }
            }

            foreach (IInputDevice device2 in InputManager.listInputDevices
                         .Where(x => x.eInputDeviceType == EInputDeviceType.Joystick))
            {
                foreach (KeyValuePair<int, string> pair in ConfigIni.joystickDict.Where(pair =>
                             device2.GUID.Equals(pair.Value)))
                {
                    ((CInputJoystick)device2).SetID(pair.Key);
                    break;
                }
            }
        });

        AddInitializer("Pad", () => { Pad = new CPad(ConfigIni, InputManager); });

        AddInitializer("Sound Manager", () =>
        {
            ESoundDeviceType soundDeviceType = ConfigIni.nSoundDriverType switch
            {
                0 => ESoundDeviceType.DirectSound,
                1 => ESoundDeviceType.ASIO,
                2 => ESoundDeviceType.ExclusiveWASAPI,
                3 => ESoundDeviceType.SharedWASAPI,
                _ => ESoundDeviceType.Unknown
            };

            SoundManager = new CSoundManager(maniaGl.host.GetWindowHandle(),
                soundDeviceType,
                ConfigIni.nWASAPIBufferSizeMs,
                ConfigIni.bEventDrivenWASAPI,
                0,
                ConfigIni.nASIODevice,
                ConfigIni.bUseOSTimer
            );
            UpdateWindowTitle();
            CSoundManager.bIsTimeStretch = ConfigIni.bTimeStretch;
            SoundManager.nMasterVolume = ConfigIni.nMasterVolume;

            string strDefaultSoundDeviceBusType = CSoundManager.strDefaultDeviceBusType;
            Trace.TraceInformation($"Bus type of the default sound device = {strDefaultSoundDeviceBusType}");
        });
        
        AddInitializer("Input", () => Input = new Input());

        AddInitializer("DiscordRichPresence", () =>
        {
            if (ConfigIni.bDiscordRichPresenceEnabled && !bCompactMode)
                DiscordRichPresence = new CDiscordRichPresence(ConfigIni.strDiscordRichPresenceApplicationID);
        });
       
        AddInitializer("SongDb", () => SongDb = new SongDb.SongDb());
        
        AddInitializer("SongDBStatus", () =>
        {
            SongDBStatus songDbStatus = persistentUIGroup.AddChild(new SongDBStatus());
            songDbStatus.position = new Vector3(0, 720, 0);
            songDbStatus.anchor = new Vector2(0.0f, 1.0f);
        });
        
        AddInitializer("AnimatedTransition", () => gitadoraTransition = persistentUIGroup.AddChild(new GitaDoraTransition()));
        
        AddInitializer("Load Skin Sounds", () =>
        {
            Skin.bgmTitleScreen.tPlay();
            Skin.ReloadSkin();
        });
        
        AddInitializer("Update Check", () =>
        {
            Task.Run(RunUpdateService);
        });
    }

    public UpdateService updateService { get; private set; }
    public bool isUpdateReady = false;
    public string stagedUpdate;

    private async Task RunUpdateService()
    {
        updateService = new UpdateService(new UpdateOptions { Owner = "BocuD", Repo = "DTXManiaNX" });

        UpdateCheck check = await updateService.CheckAsync();

        switch (check.Status)
        {
            case UpdateCheckStatus.UpToDate:
                Trace.TraceInformation("Running the latest version.");
                return;

            case UpdateCheckStatus.Failed:
                Trace.TraceWarning("Could not check for updates: " + check.Error?.Message);
                return;

            case UpdateCheckStatus.UpdateAvailable:
                break;
        }

        var plan = check.Plan!;
        Trace.TraceInformation($"Update available: {plan.TargetVersion} - " +
            (plan.UseDelta ? $"{plan.StepCount} delta step(s)" : "full download"));

        var updateNotification = persistentUIGroup.AddChild(new UpdateNotification
        {
            position = new Vector3(0, 30, 0),
            name = "UpdateNotification",
            fontSize = 30,
        });
        updateNotification.SetText($"Update available: {plan.TargetVersion} - downloading...");

        try
        {
            try
            {
                stagedUpdate = await updateService.DownloadAsync(plan, updateNotification);
            }
            catch (InvalidOperationException) when (plan.UseDelta)
            {
                Trace.TraceWarning("Delta failed verification; falling back to full download.");
                var full = plan with { UseDelta = false, Steps = Array.Empty<DeltaStep>() };
                stagedUpdate = await updateService.DownloadAsync(full, updateNotification);
            }

            updateNotification.SetText("Update ready. Exit the game to install");
            isUpdateReady = true;
        }
        catch (Exception ex)
        {
            Trace.TraceError("Update download failed: " + ex);
            updateNotification.SetText("Update download failed");
        }
    }
}