namespace WindowMedia
{
    partial class MainWindow
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.panelBg = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.WmPlayer = new AxWMPLib.AxWindowsMediaPlayer();
            this.panelBg.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.WmPlayer)).BeginInit();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // timer2
            // 
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("华康少女文字W5", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ForeColor = System.Drawing.Color.MediumTurquoise;
            this.label1.Location = new System.Drawing.Point(161, 322);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(399, 41);
            this.label1.TabIndex = 1;
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label1.UseWaitCursor = true;
            this.label1.DoubleClick += new System.EventHandler(this.label1_DoubleClick);
            // 
            // panelBg
            // 
            this.panelBg.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panelBg.Controls.Add(this.label2);
            this.panelBg.Location = new System.Drawing.Point(27, 21);
            this.panelBg.Name = "panelBg";
            this.panelBg.Size = new System.Drawing.Size(649, 266);
            this.panelBg.TabIndex = 0;
            this.panelBg.UseWaitCursor = true;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("华康少女文字W5", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.ForeColor = System.Drawing.Color.Turquoise;
            this.label2.Location = new System.Drawing.Point(258, 86);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(169, 40);
            this.label2.TabIndex = 2;
            this.label2.Text = "Happy New Year";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label2.UseWaitCursor = true;
            this.label2.MouseEnter += new System.EventHandler(this.label2_MouseEnter);
            this.label2.MouseLeave += new System.EventHandler(this.label2_MouseLeave);
            // 
            // axWindowsMediaPlayer1
            // 
            this.WmPlayer.Enabled = true;
            this.WmPlayer.Visible = false;
            this.WmPlayer.Location = new System.Drawing.Point(66, 313);
            this.WmPlayer.Name = "axWindowsMediaPlayer1";
            this.WmPlayer.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axWindowsMediaPlayer1.OcxState")));
            this.WmPlayer.Size = new System.Drawing.Size(10, 10);
            this.WmPlayer.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(710, 388);
            this.Controls.Add(this.WmPlayer);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panelBg);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Happy New Year";
            this.UseWaitCursor = true;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.LocationChanged += new System.EventHandler(this.Form1_LocationChanged);
            this.panelBg.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.WmPlayer)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelBg;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private AxWMPLib.AxWindowsMediaPlayer WmPlayer;
    }
}