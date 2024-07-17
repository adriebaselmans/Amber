using System.Drawing;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.Gamut;
using Q42.HueApi.ColorConverters.Original;
using Color = Vortice.Mathematics.Color;

namespace HueInterface;

public class Bridge : IDisposable
{
    private readonly string _ip;
    private readonly string _appKey;
    private LocalHueClient _hueApi;

    private Color _colorLeft = new Color();
    private Color _colorRight = new Color();
    private readonly object _lockObject = new object();
    private readonly CancellationToken _cancelationToken;
    private readonly CancellationTokenSource _cancelationTokenSource;
    private readonly Thread _thread;

    public Bridge(string ip, string appKey)
    {
        _ip = ip;
        _appKey = appKey;

        _cancelationTokenSource = new CancellationTokenSource();

        _cancelationToken = _cancelationTokenSource.Token;
        
        _thread = new Thread(ThreadLoop);
        _thread.Start();
    }

    public void Shutdown()
    {
        _cancelationTokenSource.Cancel();
        _thread.Join();
    }

    private async void ThreadLoop()
    {
        _hueApi = new LocalHueClient(_ip, _appKey);

        var frameTime = 1000 / 10;
        
        var commandInit = new LightCommand
        {
            Effect = Effect.None,
            TransitionTime = TimeSpan.FromMilliseconds(0),
        };

        await _hueApi.SendCommandAsync(commandInit); // all lights
        
        while (!_cancelationToken.IsCancellationRequested)
        {
            try
            {
                (int h, int s, int l) localColorLeft;
                (int h, int s, int l) localColorRight;
                
                lock (_lockObject)
                {
                    localColorLeft = ColorHelper.HslConverter.RgbToHsl(_colorLeft);
                    localColorRight = ColorHelper.HslConverter.RgbToHsl(_colorRight);
                }

                var leftIsBlack = IsBlackish(localColorLeft);
                var commandLeft = new LightCommand
                {
                    Hue = (ushort)(localColorLeft.h / 255.0 * ushort.MaxValue),
                    Saturation = leftIsBlack ? 0 : 254,
                    Brightness = leftIsBlack ? (byte)0 : (byte)200
                };
                var taskLeft = _hueApi.SendCommandAsync(commandLeft, new []{ "7" }); //left        
                
                var rightIsBlack = IsBlackish(localColorRight);
                var commandRight = new LightCommand()
                {
                    Hue = (ushort)(localColorRight.h / 255.0 * ushort.MaxValue),
                    Saturation = rightIsBlack ? 0 : 254,
                    Brightness = rightIsBlack ? (byte)0 : (byte)200
                };
                var  taskRight = _hueApi.SendCommandAsync(commandRight, new []{ "9" }); //right 
                
                await Task.WhenAll(taskLeft, taskRight);
                await Task.Delay(frameTime, _cancelationToken);
            }
            catch 
            {
                await _cancelationTokenSource.CancelAsync();
            }
        }
    }
  
    public static bool IsBlackish((int h, int s, int l) hsl)
    {
        if (hsl.l <= 20)
        {
            return true;
        }
        if (hsl.s <= 20)
        {
            return true;
        }
        return false;
    }
    
    public void UpdateLight(Color colorLeft, Color colorRight)
    {
        lock (_lockObject)
        {
            _colorLeft = new Color(colorLeft.ToVector3());
            _colorRight = new Color(colorRight.ToVector3());
        }
    }

    //public async Task RegisterWithBridge()
    //{
        //var localHueClient = new LocalHueClient(_ip.ToString());
        
        // BREAK HERE AND PRESS BUTTON ON BRIDGE...
        
        //var result = await localHueClient.RegisterAsync("Amber", "Akira");
    //}
    
    public void Dispose()
    {
        _cancelationTokenSource?.Dispose();
    }
}