namespace CodeGeneratorForm
{
    partial class UserRelationForm
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
            txtRelProp = new TextBox();
            chkGenerateRelInTable = new CheckBox();
            chkHideRelInTable = new CheckBox();
            btnSave = new Button();
            cmboRelType = new ComboBox();
            lblRelType = new Label();
            lblPropName = new Label();
            lblHint = new Label();
            SuspendLayout();
            // 
            // txtRelProp
            // 
            txtRelProp.Location = new Point(133, 63);
            txtRelProp.Name = "txtRelProp";
            txtRelProp.PlaceholderText = "enter property name";
            txtRelProp.Size = new Size(157, 23);
            txtRelProp.TabIndex = 2;
            // 
            // chkGenerateRelInTable
            // 
            chkGenerateRelInTable.AutoSize = true;
            chkGenerateRelInTable.Checked = true;
            chkGenerateRelInTable.CheckState = CheckState.Checked;
            chkGenerateRelInTable.Location = new Point(338, 38);
            chkGenerateRelInTable.Name = "chkGenerateRelInTable";
            chkGenerateRelInTable.Size = new Size(163, 19);
            chkGenerateRelInTable.TabIndex = 60;
            chkGenerateRelInTable.Text = "Generate Relation In Table";
            chkGenerateRelInTable.UseVisualStyleBackColor = true;
            chkGenerateRelInTable.CheckedChanged += chkGenerateRelInTable_CheckedChanged;
            // 
            // chkHideRelInTable
            // 
            chkHideRelInTable.AutoSize = true;
            chkHideRelInTable.Location = new Point(338, 62);
            chkHideRelInTable.Name = "chkHideRelInTable";
            chkHideRelInTable.Size = new Size(141, 19);
            chkHideRelInTable.TabIndex = 59;
            chkHideRelInTable.Text = "Hide Relation In Table";
            chkHideRelInTable.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSave.Location = new Point(39, 134);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(87, 44);
            btnSave.TabIndex = 63;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // cmboRelType
            // 
            cmboRelType.FormattingEnabled = true;
            cmboRelType.Items.AddRange(new object[] { "Single Relation", "Single Relation (Nullable)", "Many Relation" });
            cmboRelType.Location = new Point(133, 34);
            cmboRelType.Name = "cmboRelType";
            cmboRelType.Size = new Size(157, 23);
            cmboRelType.TabIndex = 64;
            cmboRelType.SelectedIndexChanged += cmboRelType_SelectedIndexChanged;
            // 
            // lblRelType
            // 
            lblRelType.AutoSize = true;
            lblRelType.Location = new Point(39, 37);
            lblRelType.Name = "lblRelType";
            lblRelType.Size = new Size(66, 15);
            lblRelType.TabIndex = 65;
            lblRelType.Text = "Select Type";
            // 
            // lblPropName
            // 
            lblPropName.AutoSize = true;
            lblPropName.Location = new Point(39, 66);
            lblPropName.Name = "lblPropName";
            lblPropName.Size = new Size(87, 15);
            lblPropName.TabIndex = 66;
            lblPropName.Text = "Property Name";
            // 
            // lblHint
            // 
            lblHint.AutoSize = true;
            lblHint.Location = new Point(133, 98);
            lblHint.Name = "lblHint";
            lblHint.Size = new Size(394, 15);
            lblHint.TabIndex = 67;
            lblHint.Text = "enter property name in singular case. ex: EmployeeUser / Employee / User";
            lblHint.Visible = false;
            // 
            // UserRelationForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(536, 205);
            Controls.Add(lblHint);
            Controls.Add(lblPropName);
            Controls.Add(lblRelType);
            Controls.Add(cmboRelType);
            Controls.Add(btnSave);
            Controls.Add(chkGenerateRelInTable);
            Controls.Add(chkHideRelInTable);
            Controls.Add(txtRelProp);
            Name = "UserRelationForm";
            Text = "UserRelationForm";
            Load += UserRelationForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox txtRelProp;
        private CheckBox chkGenerateRelInTable;
        private CheckBox chkHideRelInTable;
        private Button btnSave;
        private ComboBox cmboRelType;
        private Label lblRelType;
        private Label lblPropName;
        private Label lblHint;
    }
}