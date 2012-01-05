using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

namespace SwfThief
{
    class ProcessHandle
    {
        private static ProcessHandle _processHandle = null;

        private ProcessHandle()
        {

        }

        public static ProcessHandle getInstence()
        {
            if (_processHandle == null)
            {
                _processHandle = new ProcessHandle();
            }
            return _processHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public List<SwfClass> list = new List<SwfClass>();

        /** 通过 进程返回swf信息 **/
        public List<SwfClass> getSwfArrByProcess(Process process)
        {
            list.Clear();

            IntPtr processId = Helper.OpenMyProcess(process.Id);
            if (processId == IntPtr.Zero)
            {
                MessageBox.Show("打开进程失败");
                return list;
            }

            long MaxAddress = 0x7fffffff;
            long address = 0;

            do
            {
                MEMORY_BASIC_INFORMATION m;

                long result = Helper.VirtualQueryEx(processId, (IntPtr)address, out m, Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));

                int rsLen = (int)m.RegionSize;
                IntPtr baseAdd = m.BaseAddress;

                //bool getPro = Helper.VirtualProtectEx(processId, m.BaseAddress, (UIntPtr)rsLen, (uint)MEM_PAGE.PAGE_READWRITE, out m.Protect);

                int mStep = 4096;
                while (rsLen > 0)
                {
                    rsLen -= mStep;

                    var bytes = new byte[8];

                    bool readBool = Helper.ReadProcessMemory(processId, baseAdd, bytes, 8, IntPtr.Zero);

                    SwfClass swf = SearchbyText(bytes, "FWS", (UInt32)baseAdd, m);
                    if (swf != null)
                    {
                        swf.baseAddress = (uint)baseAdd;
                        swf.fileByte = new byte[swf.size];
                        Helper.ReadProcessMemory(processId, baseAdd, swf.fileByte, swf.size, IntPtr.Zero);
                        swf.url = Config.getDir(baseAdd.ToString());
                        list.Add(swf);

                        //保存到临时目录

                        FileStream file = new FileStream(swf.url, FileMode.Create);
                        file.Write(swf.fileByte, 0, swf.size);
                        file.Flush();
                        file.Close();
                    }

                    baseAdd += mStep;
                }
                if (address == (long)m.BaseAddress + (long)m.RegionSize)
                    break;
                address = (long)m.BaseAddress + (long)m.RegionSize;
            } while (address <= MaxAddress);

            Helper.CloseHandle(processId);

            return list;
        }

        //判断字符串并返回size
        private SwfClass SearchbyText(byte[] bytes, String SearchValue, UInt32 BaseAddress, MEMORY_BASIC_INFORMATION inf)
        {
            var TextBytes = Encoding.Default.GetBytes(SearchValue);

            if (SearchValue == Encoding.Default.GetString(bytes, 0, TextBytes.Length))
            {
                int version = bytes[3];
                if (7 < version && version <= 10)
                {
                    SwfClass swf = new SwfClass();
                    swf.size = BitConverter.ToInt32(bytes, 4);
                    swf.version = version;

                    return swf;
                }
            }
            return null;
        }

        //通过索引返回内容
        public SwfClass getSwfClassByIndex(int index)
        {
            if (index < list.Count)
            {
                return list[index];
            }
            return null;
        }
    }
}
