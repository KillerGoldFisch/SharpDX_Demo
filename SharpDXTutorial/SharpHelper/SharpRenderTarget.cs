using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer11 = SharpDX.Direct3D11.Buffer;

namespace SharpHelper
{
    /// <summary>
    /// Contain Render Targets
    /// </summary>
    public class SharpRenderTarget : IDisposable
    {
        /// <summary>
        /// Device pointer
        /// </summary>
        public SharpDevice Device { get; private set; }

        /// <summary>
        /// Render Target
        /// </summary>
        public RenderTargetView Target
        {
            get { return _target; }
        }

        /// <summary>
        /// Depth Buffer for Render Target
        /// </summary>
        public DepthStencilView Zbuffer
        {
            get { return _zbuffer; }
        }

        /// <summary>
        /// Resource connected to Render Target
        /// </summary>
        public ShaderResourceView Resource
        {
            get { return _resource; }
        }

        /// <summary>
        /// Width
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Height
        /// </summary>
        public int Height { get; private set; }

        private RenderTargetView _target;
        private DepthStencilView _zbuffer;
        private ShaderResourceView _resource;

        private Texture2DDescription _target_Description;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="device">Device</param>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="format">Format</param>
        public SharpRenderTarget(SharpDevice device, int width, int height, Format format)
        {
            Device = device;
            Height = height;
            Width = width;

            Texture2D target = new Texture2D(device.Device, _target_Description =  new Texture2DDescription()
            {
                Format = format,
                Width = width,
                Height = height,
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Read,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
            });

            _target = new RenderTargetView(device.Device, target);
            _resource = new ShaderResourceView(device.Device, target);
             target.Dispose();

            var _zbufferTexture = new Texture2D(Device.Device, new Texture2DDescription()
            {
                Format = Format.D16_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

            // Create the depth buffer view
            _zbuffer = new DepthStencilView(Device.Device, _zbufferTexture);
            _zbufferTexture.Dispose();

        }

        /// <summary>
        /// Apply Render Target To Device Context
        /// </summary>
        public void Apply()
        {
            Device.DeviceContext.Rasterizer.SetViewport(0, 0, Width, Height);
            Device.DeviceContext.OutputMerger.SetTargets(_zbuffer, _target);
        }

        /// <summary>
        /// Dispose resource
        /// </summary>
        public void Dispose()
        {
            _resource.Dispose();
            _target.Dispose();
            _zbuffer.Dispose();
        }


        /// <summary>
        /// Clear backbuffer and zbuffer
        /// </summary>
        /// <param name="color">background color</param>
        public void Clear(Color4 color)
        {
            Device.DeviceContext.ClearRenderTargetView(_target, color);
            Device.DeviceContext.ClearDepthStencilView(_zbuffer, DepthStencilClearFlags.Depth, 1.0F, 0);
        }

        // https://github.com/sharpdx/SharpDX-Samples/blob/master/Desktop/Direct3D11.1/ScreenCapture/Program.cs
        public System.Drawing.Bitmap ToBitmap()
        {
            var descr = new Texture2DDescription() {
                Format = _target_Description.Format,
                Width = _target_Description.Width,
                Height = _target_Description.Height,
                ArraySize = _target_Description.ArraySize,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                MipLevels = _target_Description.MipLevels,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
            };

            var screenTexture = new Texture2D(Device.Device, descr);

            var srRegion = new ResourceRegion(0, 0, 0, descr.Width + 1, descr.Height + 1, 1);
            Device.Device.ImmediateContext.CopySubresourceRegion(_resource.Resource, 0, srRegion, screenTexture, 0);

            // Get the desktop capture texture
            var mapSource = Device.Device.ImmediateContext.MapSubresource(screenTexture, 0, SharpDX.Direct3D11.MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

            // Create Drawing.Bitmap
            var bitmap = new System.Drawing.Bitmap(_target_Description.Width, _target_Description.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var boundsRect = new System.Drawing.Rectangle(0, 0, _target_Description.Width, _target_Description.Height);

            // Copy pixels from screen capture Texture to GDI bitmap
            var mapDest = bitmap.LockBits(boundsRect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);
            var sourcePtr = mapSource.DataPointer;
            var destPtr = mapDest.Scan0;
            for (int y = 0; y < _target_Description.Height; y++)
            {
                // Copy a single line 
                Utilities.CopyMemory(destPtr, sourcePtr, _target_Description.Width * 4);

                // Advance pointers
                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
            }

            // Release source and dest locks
            bitmap.UnlockBits(mapDest);
            Device.Device.ImmediateContext.UnmapSubresource(screenTexture, 0);
            screenTexture.Dispose();

            return bitmap;
        }

    }
}
