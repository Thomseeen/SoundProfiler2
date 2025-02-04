﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

using Thomsen.SoundProfiler2.Models;
using Thomsen.SoundProfiler2.Properties;

using Vannatech.CoreAudio.Constants;
using Vannatech.CoreAudio.Enumerations;
using Vannatech.CoreAudio.Externals;
using Vannatech.CoreAudio.Interfaces;

namespace Thomsen.SoundProfiler2.Handler {
    public class CoreAudioHandler {
        #region Public Methods
        public static MixerApplicationModel[] GetMixerApplications() {
            List<MixerApplicationModel> mixerApplications = new();

            IMMDevice device = GetActiveMultimediaDevice();
            try {
                foreach (IAudioSessionControl2 session in GetSessions(device)) {
                    try {
                        session.GetDisplayName(out string displayName);
                        session.GetProcessId(out uint pid);
                        ((ISimpleAudioVolume)session).GetMasterVolume(out float volumeLevel);

                        if (pid == 0) {
                            /* Skip idle thread */
                            continue;
                        }

                        Process? process = Process.GetProcessById((int)pid);

                        if (process is null) {
                            /* Skip non retrievable process */
                            continue;
                        }

                        string friendlyName = !string.IsNullOrWhiteSpace(displayName)
                            ? displayName
                            : !string.IsNullOrWhiteSpace(process.MainWindowTitle) ? process.MainWindowTitle : process.ProcessName;

                        Icon? icon = null;
                        try {
                            if (process.MainModule is not null && !string.IsNullOrEmpty(process.MainModule.FileName)) {
                                icon = Icon.ExtractAssociatedIcon(process.MainModule.FileName);
                            }
                        } catch (Win32Exception) { }

                        if (icon is null) {
                            icon = Resources.Win32Project_16x_ico;
                        }

                        mixerApplications.Add(new MixerApplicationModel() {
                            ProcessId = (int)pid,
                            DeviceName = GetFriendlyName(device),
                            FriendlyName = friendlyName,
                            ProcessName = process.ProcessName,
                            ApplicationIcon = icon,
                            VolumeLevel = volumeLevel
                        });
                    } catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) {
                        /* App closed */
                    } finally {
                        Marshal.ReleaseComObject(session);
                    }
                }
            } finally {
                Marshal.ReleaseComObject(device);
            }

            return mixerApplications.ToArray();
        }

        public static void SetMixerApplicationVolume(MixerApplicationModel mixerApplication) {
            foreach (IMMDevice device in GetDevices()) {
                try {
                    foreach (IAudioSessionControl2 session in GetSessions(device)) {
                        try {
                            session.GetProcessId(out uint pid);

                            if (pid == mixerApplication.ProcessId) {
                                ((ISimpleAudioVolume)session).SetMasterVolume(mixerApplication.VolumeLevel, Guid.Empty);
                                return;
                            }
                        } finally {
                            Marshal.ReleaseComObject(session);
                        }
                    }
                } finally {
                    Marshal.ReleaseComObject(device);
                }
            }
        }
        #endregion Public Methods

        #region Private Methods
        private static IAudioSessionControl2[] GetSessions(IMMDevice device) {
            device.Activate(new Guid(ComIIDs.IAudioSessionManager2IID), (uint)CLSCTX.CLSCTX_INPROC_SERVER, IntPtr.Zero, out object objInterface);
            var sessionManager2 = (IAudioSessionManager2)objInterface;

            try {
                sessionManager2.GetSessionEnumerator(out IAudioSessionEnumerator sessionsEnumerator);

                try {
                    List<IAudioSessionControl2> sessions = new();
                    sessionsEnumerator.GetCount(out int sessionsCnt);
                    for (int ii = 0; ii < sessionsCnt; ii++) {

                        sessionsEnumerator.GetSession(ii, out IAudioSessionControl session);
                        sessions.Add((IAudioSessionControl2)session);
                    }
                    return sessions.ToArray();
                } finally {
                    Marshal.ReleaseComObject(sessionsEnumerator);
                }
            } finally {
                Marshal.ReleaseComObject(sessionManager2);
            }
        }

        private static IMMDevice GetActiveMultimediaDevice(bool isInput = false) {
            Type? deviceEnumeratorType = Type.GetTypeFromCLSID(new Guid(ComCLSIDs.MMDeviceEnumeratorCLSID));

            if (deviceEnumeratorType is null) {
                throw new InvalidOperationException("Could not create device enumerator");
            }

            IMMDeviceEnumerator? deviceEnumerator = (IMMDeviceEnumerator?)Activator.CreateInstance(deviceEnumeratorType);

            if (deviceEnumerator is null) {
                throw new InvalidOperationException("Failed to create device enumerator");
            }

            try {
                deviceEnumerator.GetDefaultAudioEndpoint(isInput ? EDataFlow.eCapture : EDataFlow.eRender, ERole.eMultimedia, out IMMDevice device);
                return device;
            } finally {
                Marshal.ReleaseComObject(deviceEnumerator);
            }
        }

        private static IMMDevice[] GetDevices(bool isInput = false) {
            Type? deviceEnumeratorType = Type.GetTypeFromCLSID(new Guid(ComCLSIDs.MMDeviceEnumeratorCLSID));

            if (deviceEnumeratorType is null) {
                throw new InvalidOperationException("Could not create device enumerator");
            }

            IMMDeviceEnumerator? deviceEnumerator = (IMMDeviceEnumerator?)Activator.CreateInstance(deviceEnumeratorType);

            if (deviceEnumerator is null) {
                throw new InvalidOperationException("Failed to create device enumerator");
            }

            try {
                deviceEnumerator.EnumAudioEndpoints(isInput ? EDataFlow.eCapture : EDataFlow.eRender, DEVICE_STATE_XXX.DEVICE_STATE_ACTIVE, out IMMDeviceCollection deviceCollection);

                try {
                    List<IMMDevice> devices = new();
                    deviceCollection.GetCount(out uint devicesCnt);
                    for (uint ii = 0; ii < devicesCnt; ii++) {
                        deviceCollection.Item(ii, out IMMDevice device);
                        devices.Add(device);
                    }
                    return devices.ToArray();
                } finally {
                    Marshal.ReleaseComObject(deviceCollection);
                }
            } finally {
                Marshal.ReleaseComObject(deviceEnumerator);
            }
        }

        private static string GetFriendlyName(IMMDevice device) {
            device.OpenPropertyStore(STGM.STGM_READ, out IPropertyStore properties);

            try {
                properties.GetCount(out uint propertiesCnt);
                for (uint jj = 0; jj < propertiesCnt; jj++) {
                    properties.GetAt(jj, out PROPERTYKEY key);
                    if (key.fmtid == PropertyKeys.PKEY_DeviceInterface_FriendlyName) {
                        properties.GetValue(ref key, out PROPVARIANT value);
                        if ((VarEnum)value.vt == VarEnum.VT_LPWSTR) {
                            return Marshal.PtrToStringUni(value.Data.AsStringPtr) ?? "";
                        } else {
                            /* Unexpected type */
                            throw new NotImplementedException("Unexpected type");
                        }
                    }
                }
            } finally {
                Marshal.ReleaseComObject(properties);
            }

            /* None found */
            return "";
        }
        #endregion Private Methods
    }
}
