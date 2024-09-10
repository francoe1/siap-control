namespace SiapControl.Forms
{
    partial class ControlForm
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.dt1 = new System.Windows.Forms.DataGridView();
            this.dt1_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dt1_user_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dt1_siap_path = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.m_btn_addUser = new System.Windows.Forms.Button();
            this.m_btn_update = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.m_btn_modules = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.dt1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // dt1
            // 
            this.dt1.AllowUserToAddRows = false;
            this.dt1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dt1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dt1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dt1_id,
            this.dt1_user_name,
            this.dt1_siap_path});
            this.dt1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dt1.Location = new System.Drawing.Point(3, 16);
            this.dt1.Name = "dt1";
            this.dt1.ReadOnly = true;
            this.dt1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dt1.Size = new System.Drawing.Size(770, 350);
            this.dt1.TabIndex = 0;
            // 
            // dt1_id
            // 
            this.dt1_id.HeaderText = "ID";
            this.dt1_id.Name = "dt1_id";
            this.dt1_id.ReadOnly = true;
            this.dt1_id.Visible = false;
            // 
            // dt1_user_name
            // 
            this.dt1_user_name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dt1_user_name.HeaderText = "Usuario";
            this.dt1_user_name.Name = "dt1_user_name";
            this.dt1_user_name.ReadOnly = true;
            this.dt1_user_name.Width = 68;
            // 
            // dt1_siap_path
            // 
            this.dt1_siap_path.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dt1_siap_path.HeaderText = "Ruta";
            this.dt1_siap_path.Name = "dt1_siap_path";
            this.dt1_siap_path.ReadOnly = true;
            // 
            // m_btn_addUser
            // 
            this.m_btn_addUser.Location = new System.Drawing.Point(6, 19);
            this.m_btn_addUser.Name = "m_btn_addUser";
            this.m_btn_addUser.Size = new System.Drawing.Size(75, 23);
            this.m_btn_addUser.TabIndex = 2;
            this.m_btn_addUser.Text = "Nuevo";
            this.m_btn_addUser.UseVisualStyleBackColor = true;
            this.m_btn_addUser.Click += new System.EventHandler(this.m_btn_addUser_Click);
            // 
            // m_btn_update
            // 
            this.m_btn_update.Location = new System.Drawing.Point(87, 19);
            this.m_btn_update.Name = "m_btn_update";
            this.m_btn_update.Size = new System.Drawing.Size(75, 23);
            this.m_btn_update.TabIndex = 3;
            this.m_btn_update.Text = "Instalar";
            this.m_btn_update.UseVisualStyleBackColor = true;
            this.m_btn_update.Click += new System.EventHandler(this.m_btn_update_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.m_btn_modules);
            this.groupBox1.Controls.Add(this.m_btn_addUser);
            this.groupBox1.Controls.Add(this.m_btn_update);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(776, 51);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Herramientas";
            // 
            // m_btn_modules
            // 
            this.m_btn_modules.Enabled = false;
            this.m_btn_modules.Location = new System.Drawing.Point(168, 19);
            this.m_btn_modules.Name = "m_btn_modules";
            this.m_btn_modules.Size = new System.Drawing.Size(75, 23);
            this.m_btn_modules.TabIndex = 5;
            this.m_btn_modules.Text = "Módulos";
            this.m_btn_modules.UseVisualStyleBackColor = true;
            this.m_btn_modules.Click += new System.EventHandler(this.m_btn_modules_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.dt1);
            this.groupBox2.Location = new System.Drawing.Point(12, 69);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(776, 369);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Instalaciones";
            // 
            // ControlForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ControlForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SIAPControl - Administrador de SIAP";
            ((System.ComponentModel.ISupportInitialize)(this.dt1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.DataGridView dt1;
        private System.Windows.Forms.Button m_btn_addUser;
        private System.Windows.Forms.Button m_btn_update;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button m_btn_modules;
        private System.Windows.Forms.DataGridViewTextBoxColumn dt1_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn dt1_user_name;
        private System.Windows.Forms.DataGridViewTextBoxColumn dt1_siap_path;
    }
}

