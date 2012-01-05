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
using zlib;

namespace SwfThief
{
    public partial class MyForm : Form
    {
        public Process[] porcessArr;//进程列表

        public MyForm()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            Config.winTempDir = Environment.GetEnvironmentVariable("TEMP");
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
            string selProcessName = listBox.SelectedItem as String;
            selProcessName = selProcessName.Substring(0, selProcessName.LastIndexOf('#')-1);
            groupBox.Text = "进程:" + selProcessName;

       //     startSearch();

            Cursor = Cursors.WaitCursor;

             var thread = new Thread(startSearch);
             thread.Start();
        }

        private void startSearch(){
            swfListBox.Items.Clear();

            Process porcess = porcessArr[listBox.SelectedIndex];

            List<SwfClass> list = ProcessHandle.getInstence().getSwfArrByProcess(porcess);

            if (list.Count == 0)
            {
                swfListBox.Items.Add("获取不到swf信息");
            }
            else
            {
                string str = "";
                foreach (SwfClass swf in list)
                {
                    str = string.Format("地址0x{0}-大小{1}Kb-版本{2}", swf.baseAddress.ToString("X"), swf.size / 1024,swf.version);
                    swfListBox.Items.Add(str);
                }
            }

            Cursor = Cursors.Default;
        }

        //导出
        private void exportBtn_Click(object sender, EventArgs e)
        {
            int index = swfListBox.SelectedIndex;
            SwfClass swf = ProcessHandle.getInstence().getSwfClassByIndex(index);

            if (saveFileDialog.ShowDialog() == DialogResult.OK){
                string localFilePath = saveFileDialog.FileName;

                if(checkBox.Checked == false){
                    byte[] fileByte = swf.fileByte;
                    FileStream file = new FileStream(localFilePath, FileMode.Create);
                    file.Write(fileByte, 0, swf.size);
                    file.Flush();
                    file.Close();
                }
                else
                {
                    MemoryStream mStream = new MemoryStream(swf.fileByte.Length);
                    mStream.WriteByte(0x43);
                    mStream.Write(swf.fileByte, 1, 7);

                    MemoryStream getStream = new MemoryStream(swf.fileByte.Length);

                    //ZipOutputStream outPut = new ZipOutputStream(getStream);
                    //ZipEntry entry = new ZipEntry("");
                    //entry.CompressionMethod = CompressionMethod.WinZipAES
                    //outPut.PutNextEntry(entry);
                    //outPut.Write(swf.fileByte, 8, swf.fileByte.Length - 8);
                    //outPut.Finish();

                    ZOutputStream outPut = new ZOutputStream(getStream,zlib.zlibConst.Z_DEFAULT_COMPRESSION);
                    outPut.Write(swf.fileByte, 8, swf.fileByte.Length - 8);
                    outPut.finish();


                    getStream.WriteTo(mStream);

                    outPut.Close();
         
                    FileStream file = new FileStream(localFilePath, FileMode.Create);
                    file.Write(mStream.ToArray(), 0,(int)mStream.Length);
                    file.Flush();
                    file.Close();

                    MessageBox.Show("导出swf成功,路径:" + localFilePath,"成功");
                }



            }

        }

        //选中其中一个项播放
        private void swfListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = swfListBox.SelectedIndex;
            SwfClass swf = ProcessHandle.getInstence().getSwfClassByIndex(index);
            axShockwaveFlash.Movie = swf.url;
            axShockwaveFlash.Play();
        }  
       }
}
