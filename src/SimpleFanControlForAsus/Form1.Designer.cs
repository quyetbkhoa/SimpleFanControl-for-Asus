namespace AsusFanControlGUI
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                trayIcon?.Dispose();
                timer?.Dispose();
                controlTimer?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            SuspendLayout();
            Font = new System.Drawing.Font("Segoe UI", 9F);
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackColor = UiPalette.Page;
            ClientSize = new System.Drawing.Size(1000, 700);
            Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            MinimumSize = new System.Drawing.Size(900, 640);
            Name = "Form1";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "SimpleFanControl for Asus";
            ResumeLayout(false);
        }
    }
}
