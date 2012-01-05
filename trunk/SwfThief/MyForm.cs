using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProcessesList
{
    public partial class MyForm : Form
    {
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

        public enum MEM_PAGE
        {
            PAGE_NOACCESS = 0x1,
            PAGE_READONLY = 0x2,
            PAGE_READWRITE = 0x4,
            PAGE_WRITECOPY = 0x8,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_READWRITECOPY = 0x50,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400,
        }



        public enum MEM_COMMIT
        {
            MEM_COMMIT = 0x1000,
            MEM_RESERVE = 0x2000,
            MEM_DECOMMIT = 0x4000,
            MEM_RELEASE = 0x8000,
            MEM_FREE = 0x10000,
            MEM_PRIVATE = 0x20000,
            MEM_MAPPED = 0x40000,
            MEM_RESET = 0x80000,
            MEM_TOP_DOWN = 0x100000,
            MEM_WRITE_WATCH = 0x200000,
            MEM_PHYSICAL = 0x400000,
            MEM_IMAGE = 0x1000000
        }

        public struct SwfFile
        {
            public byte[] fileByte;
            public uint BaseAddress;
            public int size;
            public int version;
            public string url;
        }

        public Process[] porcessArr;//进程列表

        public string selProcessName;//选中的进程名

        public List <SwfFile> swfFile;//得到的swf信息列表

        public string winTempDir;

        public MyForm()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            winTempDir = Environment.GetEnvironmentVariable("TEMP");
        }


        //程序加载完成获取一次进程列表
        private void MyForm_Load(object sender, EventArgs e)
        {
            timer_Tick(null, null);
        }

        //刷新一次进程
        private void refreshBtn_Click(object sender, EventArgs e)
        {
            timer_Tick(null, null);
        }

        //timer根据进程长度不同刷新列表
        private void timer_Tick(object sender, EventArgs e)
        {
            porcessArr = Process.GetProcesses();
            if (porcessArr.Length == listBox.Items.Count)
            {
                return;
            }

            listBox.Items.Clear();
            foreach (Process pre in porcessArr)
            {
                listBox.Items.Add(pre.ProcessName+" #"+pre.Id);
            }

            label1.Text = "数量:" + porcessArr.Length;
        }

        //选择进程读取内存信息
        private void listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            selProcessName = listBox.SelectedItem as String;
            selProcessName = selProcessName.Substring(0, selProcessName.LastIndexOf('#')-1);
            groupBox.Text = "进程:" + selProcessName;

       //     startSearch();

            Cursor = Cursors.WaitCursor;

             var thread = new Thread(startSearch);
             thread.Start();
        }

        private void startSearch(){
            swfListBox.Items.Clear();

            swfFile = new List<SwfFile>();

            Process porcess = porcessArr[listBox.SelectedIndex];

            IntPtr processId = Helper.OpenMyProcess(porcess.Id);
            if (processId == IntPtr.Zero)
            {
                MessageBox.Show("打开进程失败");
                return;
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
                while (rsLen >0)
                {
                    rsLen -=mStep;

                    if ((int)baseAdd == 0x9b86000)
                    {
                        int b;
                    }

                    var bytes = new byte[8];
                    
                    bool readBool = Helper.ReadProcessMemory(processId, baseAdd, bytes, 8, IntPtr.Zero);

                    int size = SearchbyText(bytes, "FWS", (UInt32)baseAdd, m);
                    if (size != 0){
                         SwfFile swf = new SwfFile();
                         swf.BaseAddress = (uint)baseAdd;
                         swf.size = size;
                         swf.fileByte = new byte[size];
                         Helper.ReadProcessMemory(processId, baseAdd, swf.fileByte, size, IntPtr.Zero);
                         swf.url = winTempDir + "\\" + baseAdd.ToString() + ".swf";
                         swfFile.Add(swf);

                        //保存到临时目录
       
                         FileStream file = new FileStream(swf.url, FileMode.Create);
                         file.Write(swf.fileByte, 0, swf.size);
                         file.Flush();
                         file.Close();
                     }

                    baseAdd += mStep;
                }
/*
                if ((int)m.BaseAddress < 0xCED5020 && 0xCED5020 < ((uint)m.BaseAddress + (uint)m.RegionSize - 1))
                {
                    
  
              
                }

                if ((m.State == (uint)MEM_COMMIT.MEM_COMMIT || m.State == (uint)MEM_COMMIT.MEM_FREE) && (m.Protect == (uint)MEM_PAGE.PAGE_READWRITE || m.Protect == (uint)MEM_PAGE.PAGE_READONLY))
                {
                    var bytes2 = new byte[(int)m.RegionSize];
                    Helper.ReadProcessMemory(processId, (IntPtr)m.BaseAddress, bytes2, (int)m.RegionSize, IntPtr.Zero);
                    SearchbyText(bytes2, "FWS", (UInt32)m.BaseAddress, m);
                }
 * */
                if( address == (long)m.BaseAddress + (long)m.RegionSize)
                    break;
                address = (long)m.BaseAddress + (long)m.RegionSize;
            } while (address <= MaxAddress);

            Helper.CloseHandle(processId);

            if (swfListBox.Items.Count == 0)
            {
                swfListBox.Items.Add("获取不到swf信息");
            }

            Cursor = Cursors.Default;
        }

        //判断字符串并返回size
        private int SearchbyText(byte[] bytes, String SearchValue, UInt32 BaseAddress, MEMORY_BASIC_INFORMATION inf)
        {
              var TextBytes = Encoding.Default.GetBytes(SearchValue);

                if (SearchValue == Encoding.Default.GetString(bytes, 0, TextBytes.Length))
               {
                   int version = bytes[3];
                   if(7 < version && version <=10){
                      int size = BitConverter.ToInt32(bytes,4);
                      string str;
                      str = string.Format("地址0x{0}-文件大小{1}Kb", BaseAddress.ToString("X"), size/1024);
                      swfListBox.Items.Add(str);

                      return size;
                   }
                }
                return 0;
          }

        //导出
        private void exportBtn_Click(object sender, EventArgs e)
        {
            int index = swfListBox.SelectedIndex;
            SwfFile swf = swfFile[index];

            if (saveFileDialog.ShowDialog() == DialogResult.OK){
                string localFilePath = saveFileDialog.FileName;

                FileStream file = new FileStream(localFilePath, FileMode.Create);
                file.Write(swf.fileByte, 0, swf.size);
                file.Flush();
                file.Close();
            }

        }

        //选中其中一个项播放
        private void swfListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = swfListBox.SelectedIndex;
            SwfFile swf = swfFile[index];
            axShockwaveFlash.Movie = swf.url;
            axShockwaveFlash.Play();
        }  
       }
}
