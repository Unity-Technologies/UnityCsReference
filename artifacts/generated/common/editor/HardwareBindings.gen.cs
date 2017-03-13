// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;


namespace UnityEditor.Hardware
{



[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct UsbDevice
{
    readonly public int    vendorId;
    readonly public int    productId;
    readonly public int    revision;
    readonly public string udid;
    readonly public string name;
    
    
    public override string ToString()
        {
            return name + " (udid:" + udid + ", vid: " + vendorId.ToString("X4") + ", pid: " + productId.ToString("X4") + ", rev: " + revision.ToString("X4") + ")";
        }
    
    
}

public sealed partial class Usb
{
    public delegate void OnDevicesChangedHandler(UsbDevice[] devices);
    
    
    public static event OnDevicesChangedHandler DevicesChanged;
    
    
    public static void OnDevicesChanged(UsbDevice[] devices)
        {
            if ((DevicesChanged != null) && (devices != null))
                DevicesChanged(devices);
        }
    
    
}

public sealed partial class DevDeviceList
{
    public delegate void OnChangedHandler();
    
    
    public static event OnChangedHandler Changed;
    
    
    public static void OnChanged()
        {
            if (Changed != null)
                Changed();
        }
    
    
    public static bool FindDevice(string deviceId, out DevDevice device)
        {
            foreach (var d in GetDevices())
            {
                if (d.id == deviceId)
                {
                    device = d;
                    return true;
                }
            }

            device = new DevDevice();
            return false;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  DevDevice[] GetDevices () ;

    internal static void Update(string target, DevDevice[] devices)
        {
            UpdateInternal(target, devices);
            OnChanged();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void UpdateInternal (string target, DevDevice[] devices) ;

}

public enum DevDeviceState
{
    Disconnected = 0,
    Connected    = 1,
}

[Flags]
public enum DevDeviceFeatures
{
    None             = 0,
    PlayerConnection = 1 << 0,
    RemoteConnection = 1 << 1,
}

[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct DevDevice
{
    readonly public string id;
    readonly public string name;
    readonly public string type;
    readonly public string module;
    readonly public DevDeviceState state;
    readonly public DevDeviceFeatures features;
    
    
    public bool isConnected { get { return state == DevDeviceState.Connected; } }
    
    
    public static DevDevice none { get { return new DevDevice("None", "None", "none", "internal", DevDeviceState.Disconnected, DevDeviceFeatures.None); } }
    
    
    public override string ToString()
        {
            return name + " (id:" + id + ", type: " + type + ", module: " + module + ", state: " + state + ", features: " + features + ")";
        }
    
    
    public DevDevice(string id, string name, string type, string module, DevDeviceState state, DevDeviceFeatures features)
        {
            this.id = id;
            this.name = name;
            this.type = type;
            this.module = module;
            this.state = state;
            this.features = features;
        }
    
    
}


}
