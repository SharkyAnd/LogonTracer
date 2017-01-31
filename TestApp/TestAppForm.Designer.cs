namespace TestApp
{
    partial class TestAppForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonWorkingHours = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonWorkingHours
            // 
            this.buttonWorkingHours.Location = new System.Drawing.Point(12, 22);
            this.buttonWorkingHours.Name = "buttonWorkingHours";
            this.buttonWorkingHours.Size = new System.Drawing.Size(145, 23);
            this.buttonWorkingHours.TabIndex = 0;
            this.buttonWorkingHours.Text = "Working Hours";
            this.buttonWorkingHours.UseVisualStyleBackColor = true;
            this.buttonWorkingHours.Click += new System.EventHandler(this.buttonWorkingHours_Click);
            // 
            // TestAppForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.buttonWorkingHours);
            this.Name = "TestAppForm";
            this.Text = "TestApp Form";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonWorkingHours;
    }
}

