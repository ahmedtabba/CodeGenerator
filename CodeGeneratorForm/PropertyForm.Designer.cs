namespace CodeGeneratorForm
{
    partial class PropertyForm
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
            label1 = new Label();
            txtName = new TextBox();
            label2 = new Label();
            cmboType = new ComboBox();
            cmboListType = new ComboBox();
            chkValidation = new CheckedListBox();
            txtMaxLen = new TextBox();
            txtMinLen = new TextBox();
            txtMaxRng = new TextBox();
            txtMinRng = new TextBox();
            lblMaxLen = new Label();
            lblMinLen = new Label();
            lblMaxRng = new Label();
            lblMinRng = new Label();
            chkLocalized = new CheckBox();
            lblEnum = new Label();
            txtEnums = new TextBox();
            btnSave = new Button();
            label3 = new Label();
            groupBox1 = new GroupBox();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(17, 9);
            label1.Name = "label1";
            label1.Size = new Size(39, 15);
            label1.TabIndex = 0;
            label1.Text = "Name";
            // 
            // txtName
            // 
            txtName.Location = new Point(62, 6);
            txtName.Name = "txtName";
            txtName.Size = new Size(128, 23);
            txtName.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(18, 44);
            label2.Name = "label2";
            label2.Size = new Size(32, 15);
            label2.TabIndex = 2;
            label2.Text = "Type";
            // 
            // cmboType
            // 
            cmboType.FormattingEnabled = true;
            cmboType.Items.AddRange(new object[] { "string", "int", "double", "decimal", "float", "bool", "enum", "List of", "Image (single file)", "List of images (multi files)", "Video", "DateTime", "DateOnly", "TimeOnly" });
            cmboType.Location = new Point(62, 41);
            cmboType.Name = "cmboType";
            cmboType.Size = new Size(128, 23);
            cmboType.TabIndex = 3;
            cmboType.SelectedIndexChanged += cmboType_SelectedIndexChanged;
            // 
            // cmboListType
            // 
            cmboListType.FormattingEnabled = true;
            cmboListType.Items.AddRange(new object[] { "string", "int", "double", "decimal", "float", "char", "bool" });
            cmboListType.Location = new Point(204, 41);
            cmboListType.Name = "cmboListType";
            cmboListType.Size = new Size(121, 23);
            cmboListType.TabIndex = 4;
            cmboListType.Visible = false;
            // 
            // chkValidation
            // 
            chkValidation.FormattingEnabled = true;
            chkValidation.Items.AddRange(new object[] { "Required", "Unique", "Has max length", "Has min length", "Has max range", "Has min range" });
            chkValidation.Location = new Point(17, 121);
            chkValidation.Name = "chkValidation";
            chkValidation.Size = new Size(119, 112);
            chkValidation.TabIndex = 5;
            chkValidation.SelectedIndexChanged += chkValidation_SelectedIndexChanged;
            // 
            // txtMaxLen
            // 
            txtMaxLen.Location = new Point(94, 17);
            txtMaxLen.Name = "txtMaxLen";
            txtMaxLen.Size = new Size(100, 23);
            txtMaxLen.TabIndex = 6;
            txtMaxLen.Visible = false;
            // 
            // txtMinLen
            // 
            txtMinLen.Location = new Point(94, 42);
            txtMinLen.Name = "txtMinLen";
            txtMinLen.Size = new Size(100, 23);
            txtMinLen.TabIndex = 7;
            txtMinLen.Visible = false;
            // 
            // txtMaxRng
            // 
            txtMaxRng.Location = new Point(94, 67);
            txtMaxRng.Name = "txtMaxRng";
            txtMaxRng.Size = new Size(100, 23);
            txtMaxRng.TabIndex = 8;
            txtMaxRng.Visible = false;
            // 
            // txtMinRng
            // 
            txtMinRng.Location = new Point(94, 93);
            txtMinRng.Name = "txtMinRng";
            txtMinRng.Size = new Size(100, 23);
            txtMinRng.TabIndex = 9;
            txtMinRng.Visible = false;
            // 
            // lblMaxLen
            // 
            lblMaxLen.AutoSize = true;
            lblMaxLen.Location = new Point(20, 20);
            lblMaxLen.Name = "lblMaxLen";
            lblMaxLen.Size = new Size(66, 15);
            lblMaxLen.TabIndex = 10;
            lblMaxLen.Text = "Max length";
            lblMaxLen.Visible = false;
            // 
            // lblMinLen
            // 
            lblMinLen.AutoSize = true;
            lblMinLen.Location = new Point(20, 45);
            lblMinLen.Name = "lblMinLen";
            lblMinLen.Size = new Size(65, 15);
            lblMinLen.TabIndex = 11;
            lblMinLen.Text = "Min length";
            lblMinLen.Visible = false;
            // 
            // lblMaxRng
            // 
            lblMaxRng.AutoSize = true;
            lblMaxRng.Location = new Point(20, 70);
            lblMaxRng.Name = "lblMaxRng";
            lblMaxRng.Size = new Size(62, 15);
            lblMaxRng.TabIndex = 12;
            lblMaxRng.Text = "Max range";
            lblMaxRng.Visible = false;
            // 
            // lblMinRng
            // 
            lblMinRng.AutoSize = true;
            lblMinRng.Location = new Point(19, 97);
            lblMinRng.Name = "lblMinRng";
            lblMinRng.Size = new Size(65, 15);
            lblMinRng.TabIndex = 13;
            lblMinRng.Text = "Min length";
            lblMinRng.Visible = false;
            // 
            // chkLocalized
            // 
            chkLocalized.AutoSize = true;
            chkLocalized.Location = new Point(216, 12);
            chkLocalized.Name = "chkLocalized";
            chkLocalized.Size = new Size(75, 19);
            chkLocalized.TabIndex = 14;
            chkLocalized.Text = "Localized";
            chkLocalized.UseVisualStyleBackColor = true;
            chkLocalized.Visible = this.HasLocalization;
            // 
            // lblEnum
            // 
            lblEnum.AutoSize = true;
            lblEnum.Location = new Point(62, 76);
            lblEnum.Name = "lblEnum";
            lblEnum.Size = new Size(74, 15);
            lblEnum.TabIndex = 15;
            lblEnum.Text = "enum values";
            lblEnum.Visible = false;
            // 
            // txtEnums
            // 
            txtEnums.Location = new Point(142, 73);
            txtEnums.Name = "txtEnums";
            txtEnums.PlaceholderText = "enum values separeted by comma ,";
            txtEnums.Size = new Size(636, 23);
            txtEnums.TabIndex = 16;
            txtEnums.Visible = false;
            // 
            // btnSave
            // 
            btnSave.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnSave.Location = new Point(17, 253);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(106, 44);
            btnSave.TabIndex = 17;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(17, 103);
            label3.Name = "label3";
            label3.Size = new Size(64, 15);
            label3.TabIndex = 36;
            label3.Text = "Validations";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(txtMaxLen);
            groupBox1.Controls.Add(txtMinLen);
            groupBox1.Controls.Add(txtMaxRng);
            groupBox1.Controls.Add(txtMinRng);
            groupBox1.Controls.Add(lblMaxLen);
            groupBox1.Controls.Add(lblMinLen);
            groupBox1.Controls.Add(lblMaxRng);
            groupBox1.Controls.Add(lblMinRng);
            groupBox1.Location = new Point(142, 113);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(212, 122);
            groupBox1.TabIndex = 37;
            groupBox1.TabStop = false;
            groupBox1.Text = "Length / Rang";
            // 
            // PropertyForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(793, 341);
            Controls.Add(groupBox1);
            Controls.Add(label3);
            Controls.Add(btnSave);
            Controls.Add(txtEnums);
            Controls.Add(lblEnum);
            Controls.Add(chkLocalized);
            Controls.Add(chkValidation);
            Controls.Add(cmboListType);
            Controls.Add(cmboType);
            Controls.Add(label2);
            Controls.Add(txtName);
            Controls.Add(label1);
            Name = "PropertyForm";
            Text = "PropertyForm";
            Load += PropertyForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtName;
        private Label label2;
        private ComboBox cmboType;
        private ComboBox cmboListType;
        private CheckedListBox chkValidation;
        private TextBox txtMaxLen;
        private TextBox txtMinLen;
        private TextBox txtMaxRng;
        private TextBox txtMinRng;
        private Label lblMaxLen;
        private Label lblMinLen;
        private Label lblMaxRng;
        private Label lblMinRng;
        private CheckBox chkLocalized;
        private Label lblEnum;
        private TextBox txtEnums;
        private Button btnSave;
        private Label label3;
        private GroupBox groupBox1;
    }
}