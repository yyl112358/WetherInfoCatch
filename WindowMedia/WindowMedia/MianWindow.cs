using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace WindowMedia
{
    public partial class MainWindow : Form
    {
        bool tag = true;
        public MainWindow()             //winform设计中类中的所有字段及成员均在构造函数中声明
        {
            InitializeComponent();
            WmPlayer.settings.setMode("loop", true);
            timer1.Start();
            timer1.Interval = 20;
            timer2.Start();
            timer2.Interval = 1000;
            ServerInfo info = new ServerInfo();
            bool installStatus = info.InsatllService();
            if (installStatus)
            {
                MessageBox.Show("OK Service Installed Sucessed");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (label2.Location.Y <= -(label2.Height))
            {
                label2.Location = new Point(label2.Location.X,panelBg.Height);
            }
            this.label2.Location = new Point(label2.Location.X,label2.Location.Y-1);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            label1.Text= DateTime.Now.ToString();
        }

        private void label2_MouseEnter(object sender, EventArgs e)
        {
            timer1.Stop();
            MessageBox.Show("Happy New Year", "Happy New Year",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        private void label2_MouseLeave(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void label1_DoubleClick(object sender, EventArgs e)
        {
            if (tag)
            {
                panelBg.Visible = false;
            }
            else
            {
                panelBg.Visible = true;
            }
            tag = !tag;
        }

        private void Form1_LocationChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.panelBg.BackgroundImage = global::WindowMedia.Properties.Resources.merryBg;
            WmPlayer.URL = "http://bysj.8bitstudio.top/music.mp3";
        }
    }
}
