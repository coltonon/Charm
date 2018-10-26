using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.IO;

public class Charm
{
    public enum CharmResult
    {
        CHARM_SUCCESS,      // no errors
        CHARM_PROCESS_NONE, // targetted process not found
        CHARM_PROCESS_MANY, // multiple instances of targetted process found
        CHARM_NATIVE_NONE,  // the native dll isn't found
        CHARM_WINDOW_NONE   // the main window of the targetted process doesn't exist 
    }

    [Flags]
    public enum CharmSettings
    {
        CHARM_REQUIRE_FOREGROUND = (1 << 0),
        CHARM_DRAW_FPS = (1 << 1),
        CHARM_VSYNC = (1 << 2),
        CHARM_FONT_CALIBRI = (1 << 3),
        CHARM_FONT_ARIAL = (1 << 4),
        CHARM_FONT_COURIER = (1 << 5),
        CHARM_FONT_GABRIOLA = (1 << 6),
        CHARM_FONT_IMPACT = (1 << 7),
        WPM_WRITE_DIRTY = (1 << 8)
    }

    // name of the native dll to import
    public const string UnmanagedFileName = "Charm_native.dll";

    // the callback function from the native dll
    private delegate void CallbackUnmanaged(int width, int height);

    // the setup function from the native dll
    [DllImport(UnmanagedFileName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void DirectOverlaySetup([MarshalAs(UnmanagedType.FunctionPtr)]CallbackUnmanaged callBackUnmanaged, IntPtr hWnd);

    // import function for enabling window settings
    [DllImport(UnmanagedFileName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void DirectOverlaySetOption(CharmSettings option);

    // callback function for **THIS** class
    public delegate void CallbackManaged(RPM rpm, Renderer renderer, int width, int height);

    // RPM instance
    private RPM rpm;

    // use NtWriteVirtualMemory instead of WriteProcessMemory to skip querying 
    private bool fastWrite = false;

    // renderer instance
    private Renderer renderer;

    // callback
    private CallbackManaged callbackManaged;

    // set some backups to keep the garbage man from throwing stuff out early
    private CallbackUnmanaged callbackUnmanaged;

    static CallbackUnmanaged StaticCallbackUnmanaged;
    static CallbackManaged StaticCallbackManaged;

    // Need a ctor before calling any of the pinvoked functions, as we have to make sure the embedded dll exists on disk first
    public Charm()
    {
        // check if the native dll has already been extracted
        if (!File.Exists(Directory.GetCurrentDirectory() + UnmanagedFileName))
        {
            // else do that now
            File.WriteAllBytes(UnmanagedFileName, Charms.Properties.Resources.Charm_native);
        }
    }

    // function for setting overlay options
    public void CharmSetOptions(CharmSettings settings)
    {
        // first check the RPM options that WON'T be sent to the D2DOverlay
        if ((settings & CharmSettings.WPM_WRITE_DIRTY) == CharmSettings.WPM_WRITE_DIRTY)
        {
            this.fastWrite = true;
        }
        else
        {
            // otherwise call the pinvoked function to set the window options
            DirectOverlaySetOption(settings);
        }
    }

    // method to call for setting up the renderer 
    public CharmResult CharmInit(CallbackManaged m_callbackManaged, string processName)
    {
        // Get all processes by the inputted name, then check that only one exists
        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length < 1) return CharmResult.CHARM_NATIVE_NONE;
        if (processes.Length > 1) return CharmResult.CHARM_PROCESS_MANY;
        Process process = processes[0];

        // Get the main window of the module, then verify it's valid
        IntPtr targetWindow = process.MainWindowHandle;
        if (targetWindow == IntPtr.Zero) return CharmResult.CHARM_WINDOW_NONE;

        // set the current instances
        this.callbackManaged = Charm.StaticCallbackManaged = m_callbackManaged;
        // set up RPM
        this.rpm = new RPM(process.Id, this.fastWrite);

        // set up renderer
        this.renderer = new Renderer();

        // create the callback
        this.callbackUnmanaged = Charm.StaticCallbackUnmanaged = new CallbackUnmanaged(CharmCallback);

        // call the native function
        DirectOverlaySetup(callbackUnmanaged, targetWindow);

        // return successful
        return CharmResult.CHARM_SUCCESS;
    }

    // This is what the native dll will call once per frame
    private void CharmCallback(int width, int height)
    {
        // set up WorldToScreen window width/height

        this.renderer.SetWorldToScreenSize(width, height);

        // we essentially forward the callback the the managed user-defined callback function
        this.callbackManaged(this.rpm, this.renderer, width, height);
    }

    public class RPM
    {
        // for querying memory
        private struct MEMORY_BASIC_INFORMATION
        {
            public ulong BaseAddress;
            public ulong AllocationBase;
            public int AllocationProtect;
            public ulong RegionSize;
            public int State;
            public ulong Protect;
            public ulong Type;
        }

        private bool writeFastMode = false; // Use NtWriteVirtualMemory and skip quering the memory

        private IntPtr hProc = IntPtr.Zero;

        // Open a handle to the process
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(UInt32 dwAccess, bool inherit, int pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UInt32 dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, Int64 lpBaseAddress, [In, Out] byte[] lpBuffer, UInt64 dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, Int64 lpBaseAddress, [In, Out] byte[] lpBuffer, UInt64 dwSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("ntdll.dll")]
        private static extern bool NtWriteVirtualMemory(IntPtr hProcess, Int64 lpBaseAddress, [In, Out] byte[] lpBuffer, UInt64 dwSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        // ctor which sets up fastmode and attaches to a process
        public RPM(int pId, bool fastmode)
        {
            this.writeFastMode = fastmode;
            this.hProc = RPM.OpenProcess(0x0010 | 0x0020 | 0x0008, false, pId);
        }

        // Read entire stuct at once, rather than each member individually
        public T ReadStruct<T>(long addr)
        {
            byte[] Buffer = new byte[Marshal.SizeOf(typeof(T))];
            IntPtr ByteRead;
            RPM.ReadProcessMemory(this.hProc, addr, Buffer, (ulong)Marshal.SizeOf(typeof(T)), out ByteRead);
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
            Marshal.Copy(Buffer, 0, ptr, Marshal.SizeOf(typeof(T)));
            T t = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);
            return t;
        }

        public long ReadInt64(long addr)
        {
            byte[] Buffer = new byte[8];
            IntPtr ByteRead;
            RPM.ReadProcessMemory(this.hProc, addr, Buffer, 8, out ByteRead);
            return BitConverter.ToInt64(Buffer, 0);
        }

        public Int32 ReadInt32(long addr)
        {
            byte[] Buffer = new byte[4];
            IntPtr ByteRead;
            RPM.ReadProcessMemory(this.hProc, addr, Buffer, 4, out ByteRead);
            return BitConverter.ToInt32(Buffer, 0);
        }

        public float ReadFloat(long addr)
        {
            byte[] Buffer = new byte[sizeof(float)];
            IntPtr ByteRead;
            RPM.ReadProcessMemory(this.hProc, addr, Buffer, sizeof(float), out ByteRead);
            return BitConverter.ToSingle(Buffer, 0);
        }

        public bool WriteMemory(long addr, byte[] Buffer)
        {
            IntPtr ptrBytesWritten;
            if (!this.writeFastMode)
            {
                RPM.WriteProcessMemory(this.hProc, addr, Buffer, (uint)Buffer.Length, out ptrBytesWritten);
            }
            else
            {
                RPM.NtWriteVirtualMemory(this.hProc, addr, Buffer, (uint)Buffer.Length, out ptrBytesWritten);
            }
            return ((IntPtr)Buffer.Length == ptrBytesWritten);
        }

        public bool WriteFloat(long addr, float _Value)
        {
            byte[] Buffer = BitConverter.GetBytes(_Value);
            return WriteMemory(addr, Buffer);
        }

        public bool WriteInt32(long addr, int _Value)
        {
            byte[] Buffer = BitConverter.GetBytes(_Value);
            return WriteMemory(addr, Buffer);
        }
        public bool WriteInt64(long addr, long _Value)
        {
            byte[] bytes = BitConverter.GetBytes(_Value);
            return WriteMemory(addr, bytes);
        }
        public bool WriteString(long addr, string _Value)
        {
            byte[] Buffer = Encoding.ASCII.GetBytes(_Value);
            return WriteMemory(addr, Buffer);
        }
        public bool WriteByte(long addr, byte _Value)
        {
            byte[] Buffer = BitConverter.GetBytes(_Value);
            return WriteMemory(addr, Buffer);
        }

        public byte ReadByte(long addr)
        {
            byte[] Buffer = new byte[sizeof(byte)];
            IntPtr ByteRead;
            RPM.ReadProcessMemory(this.hProc, addr, Buffer, sizeof(byte), out ByteRead);
            return Buffer[0];
        }
        public string ReadString(long addr)
        {
            byte[] buffer = new byte[512];
            IntPtr BytesRead;
            RPM.ReadProcessMemory(this.hProc, addr, buffer, 512, out BytesRead);
            // split it at the first null character (that's how char*'s work)
            return Encoding.ASCII.GetString(buffer).Split('\0')[0];
        }

        public System.Numerics.Vector3 ReadVector3(long addr)
        {
            System.Numerics.Vector3 tmp = new System.Numerics.Vector3();

            byte[] Buffer = new byte[12];
            IntPtr ByteRead;

            RPM.ReadProcessMemory(this.hProc, addr, Buffer, 12, out ByteRead);
            tmp.X = BitConverter.ToSingle(Buffer, (0 * 4));
            tmp.Y = BitConverter.ToSingle(Buffer, (1 * 4));
            tmp.Z = BitConverter.ToSingle(Buffer, (2 * 4));
            return tmp;
        }

        public System.Numerics.Matrix4x4 ReadMatrix(long addr)
        {
            System.Numerics.Matrix4x4 tmp = new System.Numerics.Matrix4x4();

            byte[] Buffer = new byte[64];
            IntPtr ByteRead;

            RPM.ReadProcessMemory(this.hProc, addr, Buffer, 64, out ByteRead);

            tmp.M11 = BitConverter.ToSingle(Buffer, (0 * 4));
            tmp.M12 = BitConverter.ToSingle(Buffer, (1 * 4));
            tmp.M13 = BitConverter.ToSingle(Buffer, (2 * 4));
            tmp.M14 = BitConverter.ToSingle(Buffer, (3 * 4));

            tmp.M21 = BitConverter.ToSingle(Buffer, (4 * 4));
            tmp.M22 = BitConverter.ToSingle(Buffer, (5 * 4));
            tmp.M23 = BitConverter.ToSingle(Buffer, (6 * 4));
            tmp.M24 = BitConverter.ToSingle(Buffer, (7 * 4));

            tmp.M31 = BitConverter.ToSingle(Buffer, (8 * 4));
            tmp.M32 = BitConverter.ToSingle(Buffer, (9 * 4));
            tmp.M33 = BitConverter.ToSingle(Buffer, (10 * 4));
            tmp.M34 = BitConverter.ToSingle(Buffer, (11 * 4));

            tmp.M41 = BitConverter.ToSingle(Buffer, (12 * 4));
            tmp.M42 = BitConverter.ToSingle(Buffer, (13 * 4));
            tmp.M43 = BitConverter.ToSingle(Buffer, (14 * 4));
            tmp.M44 = BitConverter.ToSingle(Buffer, (15 * 4));
            return tmp;
        }

        public bool IsValid(long addr)
        {
            MEMORY_BASIC_INFORMATION minfo;
            VirtualQueryEx(this.hProc, (IntPtr)addr, out minfo, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
            return (minfo.State == 0x1000);
        }

    }

    public class Renderer
    {
        // width of the window
        private int width = 0;

        // height of the window
        private int height = 0;

        // view projection inside the game
        private System.Numerics.Matrix4x4 pViewProj;

        // import DrawLine
        [DllImport(UnmanagedFileName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void DrawLine(float x1, float y1, float x2, float y2, float thickness, float r, float g, float b, float a = 1);

        // import DrawBox
        [DllImport(UnmanagedFileName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void DrawBox(float x, float y, float width, float height, float thickness, float r, float g, float b, float a, bool filled);

        // import DrawCircle
        [DllImport(UnmanagedFileName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void DrawCircle(float x, float y, float radius, float thickness, float r, float g, float b, float a, bool filled);

        // import DrawEllipse
        [DllImport(UnmanagedFileName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void DrawEllipse(float x, float y, float width, float height, float thickness, float r, float g, float b, float a, bool filled);

        // import DrawString and marshal
        [DllImport(UnmanagedFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern void DrawString(string str, float fontSize, float x, float y, float r, float g, float b, float a = 1);

        // for converting 0-255 RGB color values from System.Drawing.Color to 0-1f for D2D
        private float ByteToFloat(byte b)
        {
            return (float)b / 255;
        }

        // function to set up the view projection
        public void SetViewProjection(System.Numerics.Matrix4x4 m_ViewProj)
        {
            this.pViewProj = m_ViewProj;
        }

        // need to access width and height from the WorldToScreen function.  Don't call this from your renderloop!
        public void SetWorldToScreenSize(int m_width, int m_height)
        {
            this.width = m_width;
            this.height = m_height;
        }

        public void DrawLine(float x1, float y1, float x2, float y2, float thickness, System.Drawing.Color color)
        {
            Renderer.DrawLine(x1, y1, x2, y2, thickness, ByteToFloat(color.R), ByteToFloat(color.G), ByteToFloat(color.B));
        }

        public void DrawBox(float x, float y, float width, float height, float thickness, System.Drawing.Color color, bool filled)
        {
            Renderer.DrawBox(x, y, width, height, thickness, ByteToFloat(color.R), ByteToFloat(color.G), ByteToFloat(color.B), ByteToFloat(color.A), filled);
        }

        public void DrawCircle(float x, float y, float radius, float thickness, System.Drawing.Color color, bool filled)
        {
            Renderer.DrawCircle(x, y, radius, thickness, ByteToFloat(color.R), ByteToFloat(color.G), ByteToFloat(color.B), ByteToFloat(color.A), filled);
        }

        public void DrawEllipse(float x, float y, float width, float height, float thickness, System.Drawing.Color color, bool filled)
        {
            Renderer.DrawEllipse(x, y, width, height, thickness, ByteToFloat(color.R), ByteToFloat(color.G), ByteToFloat(color.B), ByteToFloat(color.A), filled);
        }

        public void DrawString(float x, float y, string text, System.Drawing.Color color, float fontsize = 24)
        {
            Renderer.DrawString(text, fontsize, x, y, ByteToFloat(color.R), ByteToFloat(color.G), ByteToFloat(color.B));
        }

        // convert world-space coordinates to screen-space coordinates
        public bool WorldToScreen(System.Numerics.Vector3 m_World, out System.Numerics.Vector3 m_Screen)
        {
            m_Screen = new System.Numerics.Vector3(0, 0, 0);
            float ScreenW = (this.pViewProj.M14 * m_World.X) + (this.pViewProj.M24 * m_World.Y) + (this.pViewProj.M34 * m_World.Z + this.pViewProj.M44);

            if (ScreenW < 0.0001f)
                return false;

            float ScreenX = (this.pViewProj.M11 * m_World.X) + (this.pViewProj.M21 * m_World.Y) + (this.pViewProj.M31 * m_World.Z + this.pViewProj.M41);
            float ScreenY = (this.pViewProj.M12 * m_World.X) + (this.pViewProj.M22 * m_World.Y) + (this.pViewProj.M32 * m_World.Z + this.pViewProj.M42);

            m_Screen.X = (this.width / 2) + (this.width / 2) * ScreenX / ScreenW;
            m_Screen.Y = (this.height / 2) - (this.height / 2) * ScreenY / ScreenW;
            m_Screen.Z = ScreenW;
            return true;
        }

    }
}