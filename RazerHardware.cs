using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RazerDiamondbackController
{
    public class RazerDevice
    {
        public const ushort RAZER_VID = 0x1532;
        public const ushort DIAMONDBACK_PID = 0x004C;

        private const uint DIGCF_PRESENT = 0x02;
        private const uint DIGCF_DEVICEINTERFACE = 0x10;
        private const uint FILE_SHARE_READ = 0x01;
        private const uint FILE_SHARE_WRITE = 0x02;
        private const uint OPEN_EXISTING = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public uint cbSize;
            public Guid interfaceClassGuid;
            public uint flags;
            public IntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public uint cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            public string devicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [DllImport("hid.dll", SetLastError = true)]
        private static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, string enumerator, IntPtr hwndParent, uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, out uint requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetPreparsedData(SafeFileHandle hFile, out IntPtr preparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern int HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS capabilities);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_SetFeature(SafeFileHandle hFile, byte[] lpReportBuffer, uint reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetFeature(SafeFileHandle hFile, byte[] lpReportBuffer, uint reportBufferLength);

        public string DevicePath { get; private set; }
        public bool IsConnected
        {
            get { return !string.IsNullOrEmpty(DevicePath); }
        }

        public bool Connect()
        {
            DevicePath = FindDevicePath();
            return IsConnected;
        }

        private static byte CalculateCrc(byte[] report)
        {
            byte crc = 0;
            for (int i = 2; i <= 87; i++)
            {
                crc ^= report[i];
            }
            return crc;
        }

        public string FindDevicePath()
        {
            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);
            IntPtr devInfo = SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (devInfo == (IntPtr)(-1)) return null;

            try
            {
                SP_DEVICE_INTERFACE_DATA ifData = new SP_DEVICE_INTERFACE_DATA();
                ifData.cbSize = (uint)Marshal.SizeOf(ifData);
                uint index = 0;

                while (SetupDiEnumDeviceInterfaces(devInfo, IntPtr.Zero, ref hidGuid, index++, ref ifData))
                {
                    SP_DEVICE_INTERFACE_DETAIL_DATA detail = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                    detail.cbSize = (IntPtr.Size == 8) ? 8u : 5u;
                    uint reqSize;
                    if (SetupDiGetDeviceInterfaceDetail(devInfo, ref ifData, ref detail, (uint)Marshal.SizeOf(detail), out reqSize, IntPtr.Zero))
                    {
                        string path = detail.devicePath;
                        if (path.IndexOf("vid_1532", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            path.IndexOf("pid_004c", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            using (var h = CreateFile(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
                            {
                                if (!h.IsInvalid)
                                {
                                    IntPtr prep;
                                    if (HidD_GetPreparsedData(h, out prep))
                                    {
                                        HIDP_CAPS caps;
                                        HidP_GetCaps(prep, out caps);
                                        HidD_FreePreparsedData(prep);
                                        // Razer control interface uses a 91-byte feature report
                                        if (caps.FeatureReportByteLength == 91)
                                        {
                                            return path;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(devInfo);
            }
            return null;
        }

        public bool GetDpi(out int dpiX, out int dpiY)
        {
            dpiX = 0;
            dpiY = 0;
            if (!Connect()) return false;

            using (var handle = CreateFile(DevicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid) return false;

                byte[] packet = new byte[91];
                packet[0] = 0x00; // Report ID
                packet[1] = 0x00; // Status
                packet[2] = 0xFF; // Tx ID
                packet[3] = 0x00; // Rem H
                packet[4] = 0x00; // Rem L
                packet[5] = 0x00; // Proto
                packet[6] = 0x07; // Data size
                packet[7] = 0x04; // Command Class: Mouse
                packet[8] = 0x85; // Command ID: Get DPI
                packet[9] = 0x01; // Arg0: VarStore

                byte[] report = new byte[90];
                Array.Copy(packet, 1, report, 0, 90);
                packet[89] = CalculateCrc(report);

                if (!HidD_SetFeature(handle, packet, 91)) return false;
                System.Threading.Thread.Sleep(10);

                byte[] resp = new byte[91];
                if (!HidD_GetFeature(handle, resp, 91)) return false;

                dpiX = (resp[10] << 8) | resp[11];
                dpiY = (resp[12] << 8) | resp[13];
                return (dpiX > 0 && dpiY > 0);
            }
        }

        public bool SetDpi(int dpiX, int dpiY)
        {
            if (dpiX < 100) dpiX = 100;
            if (dpiX > 16000) dpiX = 16000;
            if (dpiY < 100) dpiY = 100;
            if (dpiY > 16000) dpiY = 16000;

            if (!Connect()) return false;

            using (var handle = CreateFile(DevicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid) return false;

                byte[] packet = new byte[91];
                packet[0] = 0x00; // Report ID
                packet[1] = 0x00; // Status
                packet[2] = 0xFF; // Tx ID
                packet[3] = 0x00; // Rem H
                packet[4] = 0x00; // Rem L
                packet[5] = 0x00; // Proto
                packet[6] = 0x07; // Data size
                packet[7] = 0x04; // Command Class: Mouse
                packet[8] = 0x05; // Command ID: Set DPI
                packet[9] = 0x01; // Arg0: VarStore (Save permanently to mouse memory)
                packet[10] = (byte)((dpiX >> 8) & 0xFF);
                packet[11] = (byte)(dpiX & 0xFF);
                packet[12] = (byte)((dpiY >> 8) & 0xFF);
                packet[13] = (byte)(dpiY & 0xFF);

                byte[] report = new byte[90];
                Array.Copy(packet, 1, report, 0, 90);
                packet[89] = CalculateCrc(report);

                return HidD_SetFeature(handle, packet, 91);
            }
        }

        public bool GetPollingRate(out int pollingRateHz)
        {
            pollingRateHz = 0;
            if (!Connect()) return false;

            using (var handle = CreateFile(DevicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid) return false;

                byte[] packet = new byte[91];
                packet[0] = 0x00;
                packet[1] = 0x00;
                packet[2] = 0xFF;
                packet[6] = 0x01; // Data size
                packet[7] = 0x00; // Command Class: General
                packet[8] = 0x85; // Command ID: Get Polling Rate

                byte[] report = new byte[90];
                Array.Copy(packet, 1, report, 0, 90);
                packet[89] = CalculateCrc(report);

                if (!HidD_SetFeature(handle, packet, 91)) return false;
                System.Threading.Thread.Sleep(10);

                byte[] resp = new byte[91];
                if (!HidD_GetFeature(handle, resp, 91)) return false;

                byte rateCode = resp[9];
                if (rateCode == 1) pollingRateHz = 1000;
                else if (rateCode == 2) pollingRateHz = 500;
                else if (rateCode == 8) pollingRateHz = 125;
                else pollingRateHz = 1000; // default fallback

                return true;
            }
        }

        public bool SetPollingRate(int pollingRateHz)
        {
            byte rateCode;
            if (pollingRateHz == 125) rateCode = 0x08;
            else if (pollingRateHz == 500) rateCode = 0x02;
            else if (pollingRateHz == 1000) rateCode = 0x01;
            else return false;

            if (!Connect()) return false;

            using (var handle = CreateFile(DevicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid) return false;

                byte[] packet = new byte[91];
                packet[0] = 0x00;
                packet[1] = 0x00;
                packet[2] = 0xFF;
                packet[6] = 0x01; // Data size
                packet[7] = 0x00; // Command Class: General
                packet[8] = 0x05; // Command ID: Set Polling Rate
                packet[9] = rateCode; // Arg0: Polling code

                byte[] report = new byte[90];
                Array.Copy(packet, 1, report, 0, 90);
                packet[89] = CalculateCrc(report);

                return HidD_SetFeature(handle, packet, 91);
            }
        }

        private static bool SendReport(SafeFileHandle handle, byte[] report)
        {
            byte[] packet = new byte[91];
            packet[0] = 0x00;
            Array.Copy(report, 0, packet, 1, 90);
            packet[89] = CalculateCrc(report);
            bool res = HidD_SetFeature(handle, packet, 91);
            System.Threading.Thread.Sleep(10);
            return res;
        }

        private static bool SendReport(SafeFileHandle handle, byte[] args, byte cmdId, byte dataSize)
        {
            byte[] report = new byte[90];
            report[0] = 0x00;
            report[1] = 0xFF; // Tx ID 0xFF
            report[5] = dataSize;
            report[6] = 0x03; // Class 0x03
            report[7] = cmdId;
            Array.Copy(args, 0, report, 8, Math.Min(args.Length, 80));
            return SendReport(handle, report);
        }

        private int _currentBrightness = 100;
        public int CurrentBrightness
        {
            get { return _currentBrightness; }
            set { _currentBrightness = value; }
        }

        public bool SetBrightness(int percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            CurrentBrightness = percent;

            if (!Connect()) return false;

            byte bri = (byte)Math.Round(percent * 255.0 / 100.0);

            using (var handle = CreateFile(DevicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid) return false;

                foreach (byte led in new byte[] { 0x01, 0x04, 0x05 })
                {
                    byte[] repBri3F = new byte[90];
                    repBri3F[1] = 0x3F;
                    repBri3F[5] = 0x03;
                    repBri3F[6] = 0x03;
                    repBri3F[7] = 0x03;
                    repBri3F[8] = 0x01;
                    repBri3F[9] = led;
                    repBri3F[10] = bri;
                    SendReport(handle, repBri3F);

                    byte[] repBriFF = new byte[90];
                    repBriFF[1] = 0xFF;
                    repBriFF[5] = 0x03;
                    repBriFF[6] = 0x03;
                    repBriFF[7] = 0x03;
                    repBriFF[8] = 0x01;
                    repBriFF[9] = led;
                    repBriFF[10] = bri;
                    SendReport(handle, repBriFF);
                }

                if (bri == 0)
                {
                    byte[] repOff3F = new byte[90];
                    repOff3F[1] = 0x3F;
                    repOff3F[5] = 0x01;
                    repOff3F[6] = 0x03;
                    repOff3F[7] = 0x0A;
                    repOff3F[8] = 0x00;
                    SendReport(handle, repOff3F);

                    byte[] repOffFF = new byte[90];
                    repOffFF[1] = 0xFF;
                    repOffFF[5] = 0x01;
                    repOffFF[6] = 0x03;
                    repOffFF[7] = 0x0A;
                    repOffFF[8] = 0x00;
                    SendReport(handle, repOffFF);
                }

                return true;
            }
        }

        public bool SetStaticColor(byte r, byte g, byte b)
        {
            if (r == 0 && g == 0 && b == 0)
            {
                return SetLightingOff();
            }

            if (!Connect()) return false;

            byte bri = (byte)Math.Round(CurrentBrightness * 255.0 / 100.0);

            using (var handle = CreateFile(DevicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid) return false;

                // 1. Ensure requested brightness on Scroll (0x01), Logo (0x04), and Backlight/Perimeter Strip (0x05)
                foreach (byte led in new byte[] { 0x01, 0x04, 0x05 })
                {
                    byte[] repBri3F = new byte[90];
                    repBri3F[1] = 0x3F;
                    repBri3F[5] = 0x03; // Data size
                    repBri3F[6] = 0x03; // Class: Lighting
                    repBri3F[7] = 0x03; // CMD: Set Brightness
                    repBri3F[8] = 0x01; // VARSTORE
                    repBri3F[9] = led;
                    repBri3F[10] = bri;
                    SendReport(handle, repBri3F);

                    byte[] repBriFF = new byte[90];
                    repBriFF[1] = 0xFF;
                    repBriFF[5] = 0x03;
                    repBriFF[6] = 0x03;
                    repBriFF[7] = 0x03;
                    repBriFF[8] = 0x01;
                    repBriFF[9] = led;
                    repBriFF[10] = bri;
                    SendReport(handle, repBriFF);
                }

                // 2. Hardware Matrix Effect STATIC (0x06): Native engine that lights ALL LEDs uniformly!
                byte[] repStatic3F = new byte[90];
                repStatic3F[1] = 0x3F;
                repStatic3F[5] = 0x04;
                repStatic3F[6] = 0x03;
                repStatic3F[7] = 0x0A;
                repStatic3F[8] = 0x06; // STATIC EFFECT
                repStatic3F[9] = r;
                repStatic3F[10] = g;
                repStatic3F[11] = b;
                SendReport(handle, repStatic3F);

                byte[] repStaticFF = new byte[90];
                repStaticFF[1] = 0xFF;
                repStaticFF[5] = 0x04;
                repStaticFF[6] = 0x03;
                repStaticFF[7] = 0x0A;
                repStaticFF[8] = 0x06; // STATIC EFFECT
                repStaticFF[9] = r;
                repStaticFF[10] = g;
                repStaticFF[11] = b;
                SendReport(handle, repStaticFF);

                // 3. Set classic zones for Scroll (0x01) and Logo (0x04) directly
                foreach (byte led in new byte[] { 0x01, 0x04 })
                {
                    byte[] repRgb = new byte[90];
                    repRgb[1] = 0x3F;
                    repRgb[5] = 0x05;
                    repRgb[6] = 0x03;
                    repRgb[7] = 0x01; // Set LED RGB
                    repRgb[8] = 0x01; // VARSTORE
                    repRgb[9] = led;
                    repRgb[10] = r;
                    repRgb[11] = g;
                    repRgb[12] = b;
                    SendReport(handle, repRgb);

                    byte[] repEff = new byte[90];
                    repEff[1] = 0x3F;
                    repEff[5] = 0x03;
                    repEff[6] = 0x03;
                    repEff[7] = 0x02; // Set LED Effect
                    repEff[8] = 0x01; // VARSTORE
                    repEff[9] = led;
                    repEff[10] = 0x00; // STATIC
                    SendReport(handle, repEff);
                }

                return true;
            }
        }

        public bool SetSpectrumCycling()
        {
            if (!Connect()) return false;

            byte bri = (byte)Math.Round(CurrentBrightness * 255.0 / 100.0);

            using (var handle = CreateFile(DevicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid) return false;

                // 1. Ensure requested brightness
                foreach (byte led in new byte[] { 0x01, 0x04, 0x05 })
                {
                    byte[] repBri = new byte[90];
                    repBri[1] = 0x3F;
                    repBri[5] = 0x03;
                    repBri[6] = 0x03;
                    repBri[7] = 0x03;
                    repBri[8] = 0x01;
                    repBri[9] = led;
                    repBri[10] = bri;
                    SendReport(handle, repBri);

                    byte[] repBriFF = new byte[90];
                    repBriFF[1] = 0xFF;
                    repBriFF[5] = 0x03;
                    repBriFF[6] = 0x03;
                    repBriFF[7] = 0x03;
                    repBriFF[8] = 0x01;
                    repBriFF[9] = led;
                    repBriFF[10] = bri;
                    SendReport(handle, repBriFF);
                }

                // 2. Hardware Matrix Effect SPECTRUM (0x04)
                byte[] repSpec3F = new byte[90];
                repSpec3F[1] = 0x3F;
                repSpec3F[5] = 0x01;
                repSpec3F[6] = 0x03;
                repSpec3F[7] = 0x0A;
                repSpec3F[8] = 0x04; // SPECTRUM
                SendReport(handle, repSpec3F);

                byte[] repSpecFF = new byte[90];
                repSpecFF[1] = 0xFF;
                repSpecFF[5] = 0x01;
                repSpecFF[6] = 0x03;
                repSpecFF[7] = 0x0A;
                repSpecFF[8] = 0x04; // SPECTRUM
                SendReport(handle, repSpecFF);

                // 3. Classic zone spectrum
                foreach (byte led in new byte[] { 0x01, 0x04 })
                {
                    byte[] repEff = new byte[90];
                    repEff[1] = 0x3F;
                    repEff[5] = 0x03;
                    repEff[6] = 0x03;
                    repEff[7] = 0x02;
                    repEff[8] = 0x01;
                    repEff[9] = led;
                    repEff[10] = 0x04; // SPECTRUM
                    SendReport(handle, repEff);
                }

                return true;
            }
        }

        public bool SetLightingOff()
        {
            if (!Connect()) return false;

            using (var handle = CreateFile(DevicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid) return false;

                // 1. Set brightness to 0 on Scroll (0x01), Logo (0x04), and Backlight (0x05)
                foreach (byte led in new byte[] { 0x01, 0x04, 0x05 })
                {
                    byte[] repBri3F = new byte[90];
                    repBri3F[1] = 0x3F;
                    repBri3F[5] = 0x03;
                    repBri3F[6] = 0x03;
                    repBri3F[7] = 0x03; // set brightness
                    repBri3F[8] = 0x01; // VARSTORE
                    repBri3F[9] = led;
                    repBri3F[10] = 0x00; // 0 brightness
                    SendReport(handle, repBri3F);

                    byte[] repBriFF = new byte[90];
                    repBriFF[1] = 0xFF;
                    repBriFF[5] = 0x03;
                    repBriFF[6] = 0x03;
                    repBriFF[7] = 0x03;
                    repBriFF[8] = 0x01;
                    repBriFF[9] = led;
                    repBriFF[10] = 0x00;
                    SendReport(handle, repBriFF);
                }

                // 2. Hardware Matrix Effect OFF (0x00): Native engine turns off all LEDs!
                byte[] repOff3F = new byte[90];
                repOff3F[1] = 0x3F;
                repOff3F[5] = 0x01;
                repOff3F[6] = 0x03;
                repOff3F[7] = 0x0A;
                repOff3F[8] = 0x00; // NONE / OFF
                SendReport(handle, repOff3F);

                byte[] repOffFF = new byte[90];
                repOffFF[1] = 0xFF;
                repOffFF[5] = 0x01;
                repOffFF[6] = 0x03;
                repOffFF[7] = 0x0A;
                repOffFF[8] = 0x00; // NONE / OFF
                SendReport(handle, repOffFF);

                return true;
            }
        }
    }
}
