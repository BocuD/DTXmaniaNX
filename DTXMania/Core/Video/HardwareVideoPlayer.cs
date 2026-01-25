// using System.Diagnostics;
// using System.Runtime.InteropServices;
// using FFmpeg.AutoGen.Abstractions;
// using SharpDX.Direct3D9;
//
// namespace DTXMania.Core.Video;
//
// public unsafe class HardwareVideoPlayer : FFmpegVideoPlayer
// {
//     private AVBufferRef* hwDeviceCtx;
//     private AVPixelFormat hw_pix_fmt;
//     private AVFrame* sw_frame;
//
//     private SwsContext* swsContext;
//     private AVFrame* bgraFrame;
//     
//     private Texture texture;
//
//     public override void DeviceCreated()
//     {
//         texture = new Texture(CDTXMania.app.Device, codecContext->width, codecContext->height, 1,
//             Usage.Dynamic, Format.A8R8G8B8, Pool.Default);
//     }
//
//     public override void DeviceReset()
//     {
//         if (texture != null)
//         {
//             texture.Dispose();
//             texture = null;
//         }
//     }
//     
//     //some dumb shit idfk
//     private const int AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX = 1 << 0;
//     private AVPixelFormat GetHWPixelFormat(AVCodec* codec, AVHWDeviceType deviceType)
//     {
//         // Iterate over all possible hw_configs
//         for (int i = 0; ; i++)
//         {
//             AVCodecHWConfig* config = ffmpeg.avcodec_get_hw_config(codec, i);
//             if (config == null)
//                 break;
//             
//             if ((config->methods & AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX) != 0 &&
//                 config->device_type == deviceType)
//             {
//                 return config->pix_fmt;
//             }
//         }
//
//         throw new InvalidOperationException($"Hardware device type {deviceType} is not supported for this codec.");
//     }
//
//
//     public override void CreateResources()
//     {
//         hw_pix_fmt = GetHWPixelFormat(codecContext->codec, AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2);
//         AVBufferRef* tmp = hwDeviceCtx;
//         ffmpeg.av_hwdevice_ctx_create(&tmp, AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2, null, null, 0);
//         hwDeviceCtx = tmp;
//         codecContext->hw_device_ctx = ffmpeg.av_buffer_ref(hwDeviceCtx);
//
//         sw_frame = ffmpeg.av_frame_alloc();
//
//         // Setup swsContext if converting from YUV to BGRA
//         bgraFrame = ffmpeg.av_frame_alloc();
//         
//         swsContext = ffmpeg.sws_getContext(
//             codecContext->width,
//             codecContext->height,
//             codecContext->pix_fmt,
//             codecContext->width,
//             codecContext->height,
//             AVPixelFormat.AV_PIX_FMT_BGRA,
//             ffmpeg.SWS_BILINEAR,
//             null, null, null
//         );
//         
//         //setup bgraFrame
//         bgraFrame->format = (int)AVPixelFormat.AV_PIX_FMT_BGRA;
//         bgraFrame->width = codecContext->width;
//         bgraFrame->height = codecContext->height;
//         if (ffmpeg.av_frame_get_buffer(bgraFrame, 0) < 0)
//             throw new Exception("Couldn't allocate frame buffer");
//         
//         // Allocate memory for the BGRA frame data
//         int numBytes = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_BGRA, codecContext->width, codecContext->height, 1);
//         byte* rgbBuffer = (byte*)ffmpeg.av_malloc((ulong)numBytes);
//         byte_ptr8 data_ptr8 = bgraFrame->data;
//         int8 linesize8 = bgraFrame->linesize;
//         var data = *(byte_ptr4*) &data_ptr8;
//         var linesize = *(int4*) &linesize8;
//         ffmpeg.av_image_fill_arrays(ref data, ref linesize, rgbBuffer, AVPixelFormat.AV_PIX_FMT_BGRA, codecContext->width, codecContext->height, 1);
//         
//         DeviceCreated();
//     }
//
//     public override Texture GetUpdatedTexture()
//     {
//         if (TryDecodeNextFrame(out byte[] frameData))
//         {
//             UploadToD3D9Texture(texture, frameData);
//         }
//
//         return texture;
//     }
//     
//     public void UploadToD3D9Texture(Texture texture, byte[] data)
//     {
//         var rect = texture.LockRectangle(0, LockFlags.Discard);
//         Marshal.Copy(data, 0, rect.DataPointer, data.Length);
//         texture.UnlockRectangle(0);
//     }
//
//     public bool TryDecodeNextFrame(out byte[] bgraFrameData)
//     {
//         bgraFrameData = null;
//
//         AVPacket* packet = ffmpeg.av_packet_alloc();
//
//         while (ffmpeg.av_read_frame(formatContext, packet) >= 0)
//         {
//             if (packet->stream_index == videoStreamIndex)
//             {
//                 int send = ffmpeg.avcodec_send_packet(codecContext, packet);
//                 if (send < 0)
//                 {
//                     ffmpeg.av_packet_unref(packet);
//                     continue;
//                 }
//
//                 int receive = ffmpeg.avcodec_receive_frame(codecContext, frame);
//                 if (receive == 0)
//                 {
//                     if ((AVPixelFormat)frame->format == hw_pix_fmt)
//                     {
//                         // Transfer to system memory
//                         ffmpeg.av_frame_unref(sw_frame);
//                         if (ffmpeg.av_hwframe_transfer_data(sw_frame, frame, 0) < 0)
//                         {
//                             Console.WriteLine("Failed to transfer frame from GPU to CPU");
//                             ffmpeg.av_packet_unref(packet);
//                             return false;
//                         }
//                     }
//                     else
//                     {
//                         ffmpeg.av_frame_unref(sw_frame);
//                         ffmpeg.av_frame_ref(sw_frame, frame);
//                     }
//
//                     int stride = bgraFrame->linesize[0];
//                     int height = codecContext->height;
//                     int dataSize = stride * height;
//
//                     ffmpeg.sws_scale(
//                         swsContext,
//                         sw_frame->data,
//                         sw_frame->linesize,
//                         0,
//                         height,
//                         bgraFrame->data,
//                         bgraFrame->linesize
//                     );
//
//                     bgraFrameData = new byte[dataSize];
//                     Marshal.Copy((IntPtr)bgraFrame->data[0], bgraFrameData, 0, dataSize);
//
//                     ffmpeg.av_packet_unref(packet);
//                     return true;
//                 }
//             }
//
//             ffmpeg.av_packet_unref(packet);
//         }
//
//         return false;
//     }
//
//     public override void Dispose()
//     {
//         if (hwDeviceCtx != null)
//         {
//             AVBufferRef* tmp = hwDeviceCtx;
//             ffmpeg.av_buffer_unref(&tmp);
//             hwDeviceCtx = null;
//         }
//
//         base.Dispose();
//     }
// }
//
//
//
// // using System.Diagnostics;
// // using System.Runtime.InteropServices;
// // using SharpDX.Direct3D9;
// // using FFmpeg.AutoGen.Abstractions;
// //
// // namespace DTXMania.Core.Video;
// //
// // public unsafe class HardwareVideoPlayer : FFmpegVideoPlayer
// // {
// //     private AVBufferRef* hwDeviceCtx;
// //     private AVPixelFormat hw_pix_fmt;
// //     private AVFrame* hw_frame;
// //
// //     public override void CreateResources()
// //     {
// //         hw_pix_fmt = GetHWPixelFormat(codecContext->codec, AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2);
// //         getFormatCallback = GetHWFormat;
// //         codecContext->get_format = new AVCodecContext_get_format_func
// //         {
// //             Pointer = Marshal.GetFunctionPointerForDelegate(getFormatCallback),
// //         };
// //
// //         AVBufferRef* tmp = null;
// //         int err = ffmpeg.av_hwdevice_ctx_create(&tmp, AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2, null, null, 0);
// //         if (err < 0)
// //         {
// //             throw new ApplicationException($"Failed to create DXVA2 device: {FFmpegCore.AV_StrError(err)}");
// //         }
// //
// //         hwDeviceCtx = tmp;
// //         codecContext->hw_device_ctx = ffmpeg.av_buffer_ref(hwDeviceCtx);
// //
// //         hw_frame = ffmpeg.av_frame_alloc();
// //
// //         DeviceCreated();
// //     }
// //
// //     [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
// //     private unsafe delegate AVPixelFormat GetFormatCallback(AVCodecContext* ctx, AVPixelFormat* pix_fmts);
// //
// //     private static GetFormatCallback getFormatCallback;
// //
// //     private static unsafe AVPixelFormat GetHWFormat(AVCodecContext* ctx, AVPixelFormat* pix_fmts)
// //     {
// //         for (AVPixelFormat* p = pix_fmts; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
// //         {
// //             if (*p == AVPixelFormat.AV_PIX_FMT_DXVA2_VLD)
// //             {
// //                 return *p;
// //             }
// //         }
// //
// //         return AVPixelFormat.AV_PIX_FMT_NONE;
// //     }
// //
// //
// //     private AVPixelFormat GetHWPixelFormat(AVCodec* codec, AVHWDeviceType deviceType)
// //     {
// //         for (int i = 0;; i++)
// //         {
// //             AVCodecHWConfig* config = ffmpeg.avcodec_get_hw_config(codec, i);
// //             if (config == null) break;
// //
// //             const int HW_METHOD_DEVICE_CTX = 1 << 0; // value of AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX
// //             if ((config->methods & HW_METHOD_DEVICE_CTX) != 0 && config->device_type == deviceType)
// //                 return config->pix_fmt;
// //         }
// //
// //         throw new NotSupportedException(
// //             $"Hardware device type {deviceType} is not supported for codec {ffmpeg.avcodec_get_name(codec->id)}");
// //     }
// //
// //     public override Texture GetUpdatedTexture()
// //     {
// //         if (TryDecodeAndRenderFrame())
// //             return texture;
// //
// //         return null;
// //     }
// //
// //     private bool TryDecodeAndRenderFrame()
// //     {
// //         if (formatContext == null || codecContext == null)
// //             return false;
// //
// //         AVPacket* packet = ffmpeg.av_packet_alloc();
// //         while (ffmpeg.av_read_frame(formatContext, packet) >= 0)
// //         {
// //             if (packet->stream_index == videoStreamIndex)
// //             {
// //                 int sendErr = ffmpeg.avcodec_send_packet(codecContext, packet);
// //                 ffmpeg.av_packet_unref(packet);
// //                 if (sendErr < 0)
// //                 {
// //                     Trace.TraceWarning($"avcodec_send_packet failed: {FFmpegCore.AV_StrError(sendErr)}");
// //                     return false;
// //                 }
// //
// //                 int receiveErr = ffmpeg.avcodec_receive_frame(codecContext, hw_frame);
// //                 if (receiveErr == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveErr == ffmpeg.AVERROR_EOF)
// //                     continue;
// //
// //                 if (receiveErr < 0)
// //                 {
// //                     Trace.TraceError($"avcodec_receive_frame failed: {FFmpegCore.AV_StrError(receiveErr)}");
// //                     return false;
// //                 }
// //
// //                 if ((AVPixelFormat)hw_frame->format == hw_pix_fmt)
// //                 {
// //                     var surfacePtr = (IntPtr)hw_frame->data[3];
// //                     if (surfacePtr == IntPtr.Zero)
// //                     {
// //                         Trace.TraceError("Surface pointer was null");
// //                         return false;
// //                     }
// //
// //                     // Wrap the IDirect3DSurface9* pointer in SharpDX.Surface
// //                     var surface = new Surface(surfacePtr);
// //                     var device = CDTXMania.app.Device;
// //                     var renderTarget = texture.GetSurfaceLevel(0);
// //                     
// //                     //render the surface to the backbuffer
// //                     device.StretchRectangle(surface, renderTarget, TextureFilter.Point);
// //
// //                     ffmpeg.av_frame_unref(hw_frame);
// //                     return true;
// //                 }
// //
// //                 ffmpeg.av_frame_unref(hw_frame);
// //             }
// //             else
// //             {
// //                 ffmpeg.av_packet_unref(packet);
// //             }
// //         }
// //
// //         return false;
// //     }
// //
// //     public override void Dispose()
// //     {
// //         if (hwDeviceCtx != null)
// //         {
// //             AVBufferRef* tmp = hwDeviceCtx;
// //             ffmpeg.av_buffer_unref(&tmp);
// //             hwDeviceCtx = null;
// //         }
// //
// //         if (hw_frame != null)
// //         {
// //             AVFrame* tmp = hw_frame;
// //             ffmpeg.av_frame_free(&tmp);
// //             hw_frame = null;
// //         }
// //
// //         base.Dispose();
// //     }
// //
// //     private Texture texture;
// //
// //     public override void DeviceCreated()
// //     {
// //         texture = new Texture(CDTXMania.app.Device, 1280, 720, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
// //     }
// //
// //     public override void DeviceReset()
// //     {
// //         if (texture != null)
// //         {
// //             texture.Dispose();
// //             texture = null;
// //         }
// //     }
// // }
