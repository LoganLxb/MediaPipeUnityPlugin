using System;

using GpuResourcesPtr = System.IntPtr;
using MpGpuResources = System.IntPtr;

namespace Mediapipe {
  public class GpuResources : ResourceHandle {
    private bool _disposed = false;

    public GpuResources(MpGpuResources ptr) : base(ptr) {}

    protected override void Dispose(bool disposing) {
      if (_disposed) return;

      if (OwnsResource()) {
        UnsafeNativeMethods.MpGpuResourcesDestroy(ptr);
      }

      ptr = IntPtr.Zero;

      _disposed = true;
    }

    public GpuResourcesPtr GetRawPtr() {
      return UnsafeNativeMethods.MpGpuResourcesGet(ptr);
    }

    public GlContext GlContext() {
      return new GlContext(UnsafeNativeMethods.MpGpuResourcesGlContext(ptr));
    }

    public static StatusOrGpuResources Create() {
      var ptr = UnsafeNativeMethods.MpGpuResourcesCreate();

      return new StatusOrGpuResources(ptr);
    }
  }
}
