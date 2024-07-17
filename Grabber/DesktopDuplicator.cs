using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;
using ColorHelper;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Color = Vortice.Mathematics.Color;


namespace Grabber
{
    public class DesktopDuplicator
    {
        private ID3D11Device _mDevice;
        private Texture2DDescription _mTextureDesc;
        private OutputDescription _mOutputDesc;
        private IDXGIOutputDuplication _mDeskDupl;

        private ID3D11Texture2D _mDesktopImageTexture = null;

        IDXGIAdapter1 _mAdapter = null;
        private int _mWhichOutputDevice = -1;
   
        private int _mWidth;
        private int _mHeight;

        private int _mBoxWidth;
        private int _mBoxHeight;

        private byte[] _leftBoxBytes;
        private IntPtr _leftBoxPixels;
        private GCHandle _gcHandleLeftBoxPixels;

        private byte[] _rightBoxBytes;
        private IntPtr _rightBoxPixels;
        private GCHandle _gcHandleRightBoxPixels;

        Texture2DDescription _mBoxLeftTextureDesc;
        private ID3D11Texture2D _mBoxLeftTexture;

        Texture2DDescription _mBoxRightTextureDesc;
        private ID3D11Texture2D _mBoxRightTexture;
        private Box _rightRegion;
        private Box _leftRegion;
        private ColorAnalyzer _colorAnalyzer;

        /// <summary>
        /// Duplicates the output of the specified monitor.
        /// </summary>
        /// <param name="whichMonitor">The output device to duplicate (i.e. monitor). Begins with zero, which seems to correspond to the primary monitor.</param>
        public DesktopDuplicator(int whichMonitor)
            : this(0, whichMonitor) { }

        /// <summary>
        /// Duplicates the output of the specified monitor on the specified graphics adapter.
        /// </summary>
        /// <param name="whichGraphicsCardAdapter">The adapter which contains the desired outputs.</param>
        /// <param name="whichOutputDevice">The output device to duplicate (i.e. monitor). Begins with zero, which seems to correspond to the primary monitor.</param>
        public DesktopDuplicator(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            _mWhichOutputDevice = whichOutputDevice;

            DXGI.CreateDXGIFactory1(out IDXGIFactory1? dXgiFactory1);
            dXgiFactory1?.EnumAdapters1(whichGraphicsCardAdapter, out _mAdapter);

            _mDevice = D3D11.D3D11CreateDevice(DriverType.Hardware, DeviceCreationFlags.Debug);
 
            CreateDesktopDuplication();
        }

        private void CreateDesktopDuplication()
        {
            var result = _mAdapter.EnumOutputs(_mWhichOutputDevice, out var output);
              
            var output1 = output.QueryInterface<IDXGIOutput1>();

            _mOutputDesc = output.Description;

            _mWidth =  Math.Abs(_mOutputDesc.DesktopCoordinates.Right - _mOutputDesc.DesktopCoordinates.Left);
            _mHeight = Math.Abs(_mOutputDesc.DesktopCoordinates.Bottom - _mOutputDesc.DesktopCoordinates.Top);

            _mTextureDesc = new Texture2DDescription()
            {
                CPUAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = _mWidth,
                Height = _mHeight,
                MiscFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            _mDesktopImageTexture = _mDevice.CreateTexture2D(_mTextureDesc);

            _mBoxWidth = _mWidth / 8;
            _mBoxHeight = (int)(_mHeight * 0.66);

            _mBoxLeftTextureDesc = new Texture2DDescription()
            {
                CPUAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = _mBoxWidth,
                Height = _mBoxHeight,
                MiscFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            _mBoxLeftTexture = _mDevice.CreateTexture2D(_mBoxLeftTextureDesc);

            _leftBoxBytes = new byte[_mBoxWidth * _mBoxHeight * 4];
            _gcHandleLeftBoxPixels = GCHandle.Alloc(_leftBoxBytes, GCHandleType.Pinned);
            _leftBoxPixels = _gcHandleLeftBoxPixels.AddrOfPinnedObject();


            _mBoxRightTextureDesc = new Texture2DDescription()
            {
                CPUAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = _mBoxWidth,
                Height = _mBoxHeight,
                MiscFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            _mBoxRightTexture = _mDevice.CreateTexture2D(_mBoxRightTextureDesc);

            _rightBoxBytes = new byte[_mBoxWidth * _mBoxHeight * 4];
            _gcHandleRightBoxPixels = GCHandle.Alloc(_rightBoxBytes, GCHandleType.Pinned);
            _rightBoxPixels = _gcHandleRightBoxPixels.AddrOfPinnedObject();

            var dy = (_mHeight - _mBoxHeight) / 2;
            
            _leftRegion = new Box(
                0,
                dy,
                0,
                _mBoxWidth,
                dy + _mBoxHeight,
                1);
    
            _rightRegion = new Box( 
                _mWidth - _mBoxWidth,               
                dy, 
                0,
                _mWidth,
                dy + _mBoxHeight, 
                1);

            _colorAnalyzer = new ColorAnalyzer();
            
            try
            {
                _mDeskDupl?.Dispose();
                _mDeskDupl = output1.DuplicateOutput(_mDevice);
            }
            finally
            {
                output.Dispose();
            }
        }

        /// <summary>
        /// Retrieves the latest desktop image and associated metadata.
        /// </summary>
        public void GetLatestFrame(ref Color leftColor, ref Color rightColor)
        {
            if (RetrieveFrame())
            {
                GetAverageColors(ref leftColor, ref rightColor);
                ReleaseFrame();
            }
        }

        private bool RetrieveFrame()
        {
            SharpGen.Runtime.Result result;
            IDXGIResource? desktopResource; 
                
            try
            {
                result = _mDeskDupl.AcquireNextFrame(0, out _, out desktopResource);
            }
            catch
            {
                result = SharpGen.Runtime.Result.Fail;
                desktopResource = null; 
            }

            if (result == SharpGen.Runtime.Result.Ok)
            {
                using var tempTexture = desktopResource?.QueryInterface<ID3D11Texture2D>();
                _mDevice.ImmediateContext.CopyResource(_mDesktopImageTexture, tempTexture);
                return true;
            }
     
            return false;
        }

        private void GetAverageColors(ref Color leftColor, ref Color rightColor)
        {
            leftColor = GetRegionColor(_mBoxLeftTexture, _leftRegion, _leftBoxBytes, _leftBoxPixels);
            rightColor = GetRegionColor(_mBoxRightTexture, _rightRegion, _rightBoxBytes, _rightBoxPixels);
        }

        private unsafe Color GetRegionColor(ID3D11Texture2D texture, Box resourceRegion, byte[] boxBytes, nint boxPixels)
        {
            _mDevice.ImmediateContext.CopySubresourceRegion(texture, 0, 0, 0, 0, _mDesktopImageTexture, 0, resourceRegion);
            var dataBox = _mDevice.ImmediateContext.Map(texture, 0);
            Utilities.MemCopy(dataBox.DataPointer.ToPointer(), boxPixels.ToPointer(), _mBoxWidth * _mBoxHeight * 4);
            _mDevice.ImmediateContext.Unmap(texture, 0);
         
            var color = GetColorForBox(boxBytes, boxPixels);
            //var color = _colorAnalyzer.GetDominantColor(boxBytes, boxPixels);
     
            return color;
        }
        
        private unsafe Color GetColorForBox(byte[] boxBytes, IntPtr boxBytesPtr)
        {
            long r = 0;
            long g = 0;
            long b = 0;

            var bpp = 4;
            var numPixels = boxBytes.Length / bpp;

            var data = (byte*)boxBytesPtr.ToPointer();
            for (var i = 0; i < numPixels; i++)
            {
                r += data[i * 4];
                g += data[i * 4 + 1];
                b += data[i * 4 + 2];
            }

            var reciprocal = 1.0 / numPixels;
            var bAvg = (byte)(r * reciprocal);
            var gAvg = (byte)(g * reciprocal);
            var rAvg = (byte)(b * reciprocal);

            return new Color(rAvg, gAvg, bAvg);
        }

        private void ReleaseFrame()
        {
            SharpGen.Runtime.Result result;
            result = _mDeskDupl.ReleaseFrame();
        }
    }
}
