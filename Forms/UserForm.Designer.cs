namespace SiapControl.Forms
{
    partial class UserForm
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
            this.m_btn_save = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.m_txt_user = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.m_text_path = new System.Windows.Forms.TextBox();
            this.m_btn_findPath = new System.Windows.Forms.Button();
            this.m_btn_cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // m_btn_save
            // 
            this.m_btn_save.Location = new System.Drawing.Point(203, 65);
            this.m_btn_save.Name = "m_btn_save";
            this.m_btn_save.Size = new System.Drawing.Size(75, 23);
            this.m_btn_save.TabIndex = 4;
            this.m_btn_save.Text = "Guardar";
            this.m_btn_save.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(37, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Usuario";
            // 
            // m_txt_user
            // 
            this.m_txt_user.Location = new System.Drawing.Point(86, 13);
            this.m_txt_user.Name = "m_txt_user";
            this.m_txt_user.Size = new System.Drawing.Size(273, 20);
            this.m_txt_user.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(50, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(30, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Ruta";
            // 
            // m_text_path
            // 
            this.m_text_path.Location = new System.Drawing.Point(86, 39);
            this.m_text_path.Name = "m_text_path";
            this.m_text_path.Size = new System.Drawing.Size(244, 20);
            this.m_text_path.TabIndex = 2;
            // 
            // m_btn_findPath
            // 
            this.m_btn_findPath.Location = new System.Drawing.Point(336, 39);
            this.m_btn_findPath.Name = "m_btn_findPath";
            this.m_btn_findPath.Size = new System.Drawing.Size(23, 20);
            this.m_btn_findPath.TabIndex = 3;
            this.m_btn_findPath.Text = "...";
            this.m_btn_findPath.UseVisualStyleBackColor = true;
            // 
            // m_btn_cancel
            // 
            this.m_btn_cancel.Location = new System.Drawing.Point(284, 65);
            this.m_btn_cancel.Name = "m_btn_cancel";
            this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
            this.m_btn_cancel.TabIndex = 5;
            this.m_btn_cancel.Text = "Cancelar";
            this.m_btn_cancel.UseVisualStyleBackColor = true;
            // 
            // UserForm
            // 
            this.AcceptButton = this.m_btn_save;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.m_btn_cancel;
            this.ClientSize = new System.Drawing.Size(371, 96);
            this.ControlBox = false;
            this.Controls.Add(this.m_btn_findPath);
            this.Controls.Add(this.m_text_path);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.m_txt_user);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.m_btn_cancel);
            this.Controls.Add(this.m_btn_save);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UserForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Formulario usuario";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button m_btn_save;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox m_txt_user;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox m_text_path;
        private System.Windows.Forms.Button m_btn_findPath;
        private System.Windows.Forms.Button m_btn_cancel;
    }
}