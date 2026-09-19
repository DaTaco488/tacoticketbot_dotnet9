namespace tacoticketbot_dotnet9
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            richTextBox1 = new RichTextBox();
            groupBox1 = new GroupBox();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.BackColor = Color.PaleGreen;
            button1.Font = new Font("NSimSun", 26.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.Location = new Point(7, 15);
            button1.Name = "button1";
            button1.Size = new Size(214, 138);
            button1.TabIndex = 1;
            button1.Text = "START";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.BackColor = Color.FromArgb(192, 192, 255);
            button2.Location = new Point(227, 15);
            button2.Name = "button2";
            button2.Size = new Size(210, 66);
            button2.TabIndex = 2;
            button2.Text = "PAUSE";
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.BackColor = Color.Red;
            button3.Location = new Point(227, 82);
            button3.Name = "button3";
            button3.Size = new Size(210, 71);
            button3.TabIndex = 3;
            button3.Text = "STOP";
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(12, 12);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(600, 267);
            richTextBox1.TabIndex = 4;
            richTextBox1.Text = "";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(button3);
            groupBox1.Controls.Add(button2);
            groupBox1.Controls.Add(button1);
            groupBox1.Location = new Point(5, 285);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(443, 159);
            groupBox1.TabIndex = 5;
            groupBox1.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(groupBox1);
            Controls.Add(richTextBox1);
            Name = "Form1";
            Text = "Form1";
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Button button1;
        private Button button2;
        private Button button3;
        private RichTextBox richTextBox1;
        private GroupBox groupBox1;
    }
}
