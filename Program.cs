using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Laba6
{
    class Program
    {
        private static Semaphore semaphore = new Semaphore(1, 1);//счетчик, ограничивающий число потоков, которые могут одновременно обращаться к ресурсу или пулу ресурсов.
        private static Random rnd = new Random();
        private static CHAR_INFO[] ASCII_TABLE;

        private static short WIDTH = 80;
        private static short HEIGHT = 10;
        static void InitTable()//чтение с клавы
        {
            ASCII_TABLE = new CHAR_INFO[93];
            for (int x = 0; x < 93; x++)
            {
                CHAR_INFO info = new CHAR_INFO();
                info.AsciiChar = (char)(x + 33);
                info.Attributes = 10;
                ASCII_TABLE[x] = info;
            }
        }

        static void SetSize(short width, short height)//размер консоли
        {
            IntPtr currentStdout = GetStdHandle(StdOutputHandle);//API

            CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
            GetConsoleScreenBufferInfo(currentStdout, out bufferInfo);
            SMALL_RECT winInfo = bufferInfo.srWindow;
            COORD windowSize;
            windowSize.X = (short)(winInfo.Right - winInfo.Left + 1);
            windowSize.Y = (short)(winInfo.Bottom - winInfo.Top + 1);
            if (windowSize.X > width || windowSize.Y > height)
            {
                SMALL_RECT info;
                info.Left = 0;
                info.Top = 0;
                info.Right = (short)(width < windowSize.X ? width - 1 : windowSize.X - 1);
                info.Bottom = (short)(height < windowSize.Y ? height - 1 : windowSize.Y - 1);

                SetConsoleWindowInfo(currentStdout, true, ref info);
            }

            COORD size;//координаты
            size.X = width;
            size.Y = height;

            SetConsoleScreenBufferSize(currentStdout, size);

            SMALL_RECT Rect;//углы прямоугольника
            Rect.Left = 0;
            Rect.Top = 0;
            Rect.Right = (short)(width - 1);
            Rect.Bottom = (short)(height - 1);
        }

        public static void threadFunction()//считывание экрана
        {
            IntPtr currentStdout = GetStdHandle(StdOutputHandle);
            SMALL_RECT matrixRectangle;
            matrixRectangle.Left = 0;
            matrixRectangle.Top = 2;
            matrixRectangle.Right = (short)(WIDTH - 1);
            matrixRectangle.Bottom = (short)(HEIGHT - 1);

            CHAR_INFO[] buffer = new CHAR_INFO[640];
            while (true)
            {
                semaphore.WaitOne();

                ReadConsoleOutput(currentStdout, buffer, new COORD(WIDTH, 8), new COORD(0, 0), ref matrixRectangle);

                for (int x = 0; x < WIDTH; x++)
                {
                    if (rnd.NextDouble() < 0.33)
                    {
                        continue;
                    }

                    for (int y = 7; y > 0; y--)
                    {
                        buffer[y * WIDTH + x] = buffer[(y - 1) * 80 + x];
                    }

                    if (rnd.NextDouble() < 0.3)
                    {
                        CHAR_INFO newChar = new CHAR_INFO();
                        newChar.AsciiChar = ' ';
                        newChar.Attributes = 10;

                        buffer[x] = newChar;
                    }
                    else
                    {
                        buffer[x] = ASCII_TABLE[rnd.Next(ASCII_TABLE.Length)];
                    }
                }
                WriteConsoleOutput(currentStdout, buffer, new COORD(WIDTH, 8), new COORD(0, 0), ref matrixRectangle);

                String currentDateTime = DateTime.Now.ToString("yyyy-MM-dd H:mm:ss");

                uint written;
                WriteConsoleOutputCharacter(currentStdout, currentDateTime, (uint)currentDateTime.Length, new COORD((short)(WIDTH - currentDateTime.Length), 0), out written);

                semaphore.Release();

                Thread.Sleep(50);
            }
        }

        static void Main(string[] args)
        {
            AllocConsole();
            InitTable();

            IntPtr currentStdout = GetStdHandle(StdOutputHandle);
            IntPtr currentStdin = GetStdHandle(StdInputHandle);

            SetConsoleCtrlHandler(new ConsoleCtrlDelegate(ConsoleCtrlCheck), true);

            SetConsoleCP(1251);//установка кодовых страниц
            SetConsoleOutputCP(1251);

            SetConsoleMode(currentStdin, 0x0080);//захват мыши
            SetConsoleMode(currentStdin, 0x0008);

            SetSize(WIDTH, HEIGHT);

            Thread thread = new Thread(threadFunction);
            thread.IsBackground = true;
            thread.Start();

            KeyCheck(currentStdin, currentStdout);
        }

        public static void KeyCheck(IntPtr currentStdin, IntPtr currentStdout)
        {

            INPUT_RECORD[] buffer = new INPUT_RECORD[1];
            uint events;

            while (true)
            {

                ReadConsoleInput(currentStdin, buffer, 1, out events);
                INPUT_RECORD record = buffer[0];
                if (record.EventType == 0x0001)
                {
                    bool keyDown = record.KeyEvent.bKeyDown;
                    ushort repeatCount = record.KeyEvent.wRepeatCount;
                    uint controlKeyState = record.KeyEvent.dwControlKeyState;
                    ushort virtualKeyCode = record.KeyEvent.wVirtualKeyCode;
                    ushort virtualScanCode = record.KeyEvent.wVirtualScanCode;
                    char unicodeChar = record.KeyEvent.UnicodeChar;


                    if (virtualKeyCode == 27)
                    {
                        break;
                    }

                    semaphore.WaitOne();

                    String line = String.Format("KeyDown: {0} RepeatCount: {1} ControlKeyState: {2}  ", keyDown, repeatCount, controlKeyState);
                    uint written;
                    WriteConsoleOutputCharacter(currentStdout, line, (uint)line.Length, new COORD(0, 0), out written);

                    line = String.Format("VirtualKeyCode: {0} VirtualScanCode: {1} AsciiChar: {2}  ", virtualKeyCode, virtualScanCode, unicodeChar);
                    WriteConsoleOutputCharacter(currentStdout, line, (uint)line.Length, new COORD(0, 1), out written);

                    semaphore.Release();
                }
            }
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            return false;
        }

        // Windows API
        private const UInt32 StdInputHandle = 0xfffffff6;
        private const UInt32 StdOutputHandle = 0xFFFFFFF5;
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(UInt32 nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern void SetStdHandle(UInt32 nStdHandle, IntPtr handle);
        [DllImport("kernel32")]
        static extern bool AllocConsole();
        [DllImport("kernel32")]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        static extern bool WriteConsoleOutputCharacter(IntPtr hConsoleOutput,
                string lpCharacter, uint nLength, COORD dwWriteCoord,
                out uint lpNumberOfCharsWritten);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCP(
            uint wCodePageID
        );
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleOutputCP(
                uint wCodePageID
            );
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleCtrlHandler(
            ConsoleCtrlDelegate HandlerRoutine,
            bool Add
            );
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleMode(
            IntPtr hConsoleHandle,
             uint dwMode
        );
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleScreenBufferSize(
            IntPtr hConsoleOutput,
            COORD dwSize
        );
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleScreenBufferInfo(
            IntPtr hConsoleOutput,
            out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo
            );
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleWindowInfo(
            IntPtr hConsoleOutput,
            bool bAbsolute,
            [In] ref SMALL_RECT lpConsoleWindow
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadConsoleOutput(
        IntPtr hConsoleOutput,
        [Out] CHAR_INFO[] lpBuffer,
        COORD dwBufferSize,
        COORD dwBufferCoord,
        ref SMALL_RECT lpReadRegion
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteConsoleOutput(
        IntPtr hConsoleOutput,
        CHAR_INFO[] lpBuffer,
        COORD dwBufferSize,
        COORD dwBufferCoord,
        ref SMALL_RECT lpWriteRegion
        );

        [DllImport("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)]
        public static extern bool ReadConsoleInput(
        IntPtr hConsoleInput,
        [Out] INPUT_RECORD[] lpBuffer,
        uint nLength,
        out uint lpNumberOfEventsRead
        );

        delegate bool ConsoleCtrlDelegate(CtrlTypes CtrlType);
        enum CtrlTypes : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {

            public short X;
            public short Y;

            public COORD(short x, short y)
            {
                X = x;
                Y = y;
            }

        }

        public struct SMALL_RECT
        {

            public short Left;
            public short Top;
            public short Right;
            public short Bottom;

        }

        public struct CONSOLE_SCREEN_BUFFER_INFO
        {

            public COORD dwSize;
            public COORD dwCursorPosition;
            public short wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;

        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CHAR_INFO
        {
            [FieldOffset(0)]
            public char UnicodeChar;
            [FieldOffset(0)]
            public char AsciiChar;
            [FieldOffset(2)] //2 bytes seems to work properly
            public UInt16 Attributes;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT_RECORD
        {
            [FieldOffset(0)]
            public ushort EventType;
            [FieldOffset(4)]
            public KEY_EVENT_RECORD KeyEvent;
            [FieldOffset(4)]
            public MOUSE_EVENT_RECORD MouseEvent;
            [FieldOffset(4)]
            public WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;
            [FieldOffset(4)]
            public MENU_EVENT_RECORD MenuEvent;
            [FieldOffset(4)]
            public FOCUS_EVENT_RECORD FocusEvent;
        };

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]//положение обьектов
        public struct KEY_EVENT_RECORD
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.Bool)]
            public bool bKeyDown;
            [FieldOffset(4), MarshalAs(UnmanagedType.U2)]
            public ushort wRepeatCount;
            [FieldOffset(6), MarshalAs(UnmanagedType.U2)]
            //public VirtualKeys wVirtualKeyCode;
            public ushort wVirtualKeyCode;
            [FieldOffset(8), MarshalAs(UnmanagedType.U2)]
            public ushort wVirtualScanCode;
            [FieldOffset(10)]
            public char UnicodeChar;
            [FieldOffset(12), MarshalAs(UnmanagedType.U4)]
            //public ControlKeyState dwControlKeyState;
            public uint dwControlKeyState;
        }

        [StructLayout(LayoutKind.Sequential)]//последовательность обьектов
        public struct MOUSE_EVENT_RECORD
        {
            public COORD dwMousePosition;
            public uint dwButtonState;
            public uint dwControlKeyState;
            public uint dwEventFlags;
        }

        public struct WINDOW_BUFFER_SIZE_RECORD
        {
            public COORD dwSize;

            public WINDOW_BUFFER_SIZE_RECORD(short x, short y)
            {
                dwSize = new COORD();
                dwSize.X = x;
                dwSize.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MENU_EVENT_RECORD
        {
            public uint dwCommandId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FOCUS_EVENT_RECORD
        {
            public uint bSetFocus;
        }
    }
}