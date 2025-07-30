namespace CodeGeneratorForm
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
            checkBoxLocalization = new CheckBox();
            label1 = new Label();
            txtDir = new TextBox();
            label2 = new Label();
            txtEntityName = new TextBox();
            groupBoxOption = new GroupBox();
            checkBoxBulk = new CheckBox();
            lblBulk = new Label();
            checkBoxNotifications = new CheckBox();
            checkBoxVersioning = new CheckBox();
            checkBoxUserActions = new CheckBox();
            lblNotifications = new Label();
            lblVersioning = new Label();
            lblUserActions = new Label();
            checkBoxPermissions = new CheckBox();
            lblPermissions = new Label();
            lblLocalization = new Label();
            btnProperty = new Button();
            label8 = new Label();
            btnSave = new Button();
            btnNewRelation = new Button();
            label3 = new Label();
            label4 = new Label();
            pnlScrollable = new Panel();
            label5 = new Label();
            pnlRelations = new Panel();
            btnRelUsers = new Button();
            panel1 = new Panel();
            btnClearPartial = new Button();
            rdioChild = new RadioButton();
            rdioParent = new RadioButton();
            cmboParent = new ComboBox();
            lblParent = new Label();
            groupBoxOption.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // checkBoxLocalization
            // 
            checkBoxLocalization.AutoSize = true;
            checkBoxLocalization.ForeColor = SystemColors.ControlText;
            checkBoxLocalization.Location = new Point(84, 24);
            checkBoxLocalization.Name = "checkBoxLocalization";
            checkBoxLocalization.Size = new Size(15, 14);
            checkBoxLocalization.TabIndex = 0;
            checkBoxLocalization.UseVisualStyleBackColor = true;
            checkBoxLocalization.CheckedChanged += checkBoxLocalization_CheckedChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = SystemColors.ActiveCaption;
            label1.Location = new Point(9, 20);
            label1.Name = "label1";
            label1.Size = new Size(100, 15);
            label1.TabIndex = 1;
            label1.Text = "Enter Solusion Dir";
            label1.Click += label1_Click;
            // 
            // txtDir
            // 
            txtDir.Location = new Point(9, 38);
            txtDir.Name = "txtDir";
            txtDir.Size = new Size(524, 23);
            txtDir.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = SystemColors.ActiveCaption;
            label2.Location = new Point(12, 64);
            label2.Name = "label2";
            label2.Size = new Size(102, 15);
            label2.TabIndex = 3;
            label2.Text = "Enter Entity Name";
            // 
            // txtEntityName
            // 
            txtEntityName.Location = new Point(9, 89);
            txtEntityName.Name = "txtEntityName";
            txtEntityName.PlaceholderText = "entity name in singular";
            txtEntityName.Size = new Size(136, 23);
            txtEntityName.TabIndex = 4;
            // 
            // groupBoxOption
            // 
            groupBoxOption.BackColor = SystemColors.ButtonHighlight;
            groupBoxOption.Controls.Add(checkBoxBulk);
            groupBoxOption.Controls.Add(lblBulk);
            groupBoxOption.Controls.Add(checkBoxNotifications);
            groupBoxOption.Controls.Add(checkBoxVersioning);
            groupBoxOption.Controls.Add(checkBoxUserActions);
            groupBoxOption.Controls.Add(lblNotifications);
            groupBoxOption.Controls.Add(lblVersioning);
            groupBoxOption.Controls.Add(lblUserActions);
            groupBoxOption.Controls.Add(checkBoxPermissions);
            groupBoxOption.Controls.Add(lblPermissions);
            groupBoxOption.Controls.Add(lblLocalization);
            groupBoxOption.Controls.Add(checkBoxLocalization);
            groupBoxOption.Location = new Point(151, 67);
            groupBoxOption.Name = "groupBoxOption";
            groupBoxOption.Size = new Size(609, 45);
            groupBoxOption.TabIndex = 5;
            groupBoxOption.TabStop = false;
            groupBoxOption.Text = "Options";
            // 
            // checkBoxBulk
            // 
            checkBoxBulk.AutoSize = true;
            checkBoxBulk.Location = new Point(553, 24);
            checkBoxBulk.Name = "checkBoxBulk";
            checkBoxBulk.Size = new Size(15, 14);
            checkBoxBulk.TabIndex = 11;
            checkBoxBulk.UseVisualStyleBackColor = true;
            checkBoxBulk.CheckedChanged += checkBoxBulk_CheckedChanged;
            // 
            // lblBulk
            // 
            lblBulk.AutoSize = true;
            lblBulk.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblBulk.Location = new Point(515, 24);
            lblBulk.Name = "lblBulk";
            lblBulk.Size = new Size(32, 15);
            lblBulk.TabIndex = 10;
            lblBulk.Text = "Bulk";
            // 
            // checkBoxNotifications
            // 
            checkBoxNotifications.AutoSize = true;
            checkBoxNotifications.Location = new Point(494, 24);
            checkBoxNotifications.Name = "checkBoxNotifications";
            checkBoxNotifications.Size = new Size(15, 14);
            checkBoxNotifications.TabIndex = 9;
            checkBoxNotifications.UseVisualStyleBackColor = true;
            checkBoxNotifications.CheckedChanged += checkBoxNotifications_CheckedChanged;
            // 
            // checkBoxVersioning
            // 
            checkBoxVersioning.AutoSize = true;
            checkBoxVersioning.Location = new Point(380, 24);
            checkBoxVersioning.Name = "checkBoxVersioning";
            checkBoxVersioning.Size = new Size(15, 14);
            checkBoxVersioning.TabIndex = 8;
            checkBoxVersioning.UseVisualStyleBackColor = true;
            checkBoxVersioning.CheckedChanged += checkBoxVersioning_CheckedChanged;
            // 
            // checkBoxUserActions
            // 
            checkBoxUserActions.AutoSize = true;
            checkBoxUserActions.Location = new Point(284, 24);
            checkBoxUserActions.Name = "checkBoxUserActions";
            checkBoxUserActions.Size = new Size(15, 14);
            checkBoxUserActions.TabIndex = 7;
            checkBoxUserActions.UseVisualStyleBackColor = true;
            checkBoxUserActions.CheckedChanged += checkBoxUserActions_CheckedChanged;
            // 
            // lblNotifications
            // 
            lblNotifications.AutoSize = true;
            lblNotifications.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblNotifications.Location = new Point(410, 23);
            lblNotifications.Name = "lblNotifications";
            lblNotifications.Size = new Size(78, 15);
            lblNotifications.TabIndex = 6;
            lblNotifications.Text = "Notifications";
            // 
            // lblVersioning
            // 
            lblVersioning.AutoSize = true;
            lblVersioning.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblVersioning.Location = new Point(309, 22);
            lblVersioning.Name = "lblVersioning";
            lblVersioning.Size = new Size(65, 15);
            lblVersioning.TabIndex = 5;
            lblVersioning.Text = "Versioning";
            // 
            // lblUserActions
            // 
            lblUserActions.AutoSize = true;
            lblUserActions.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblUserActions.Location = new Point(204, 23);
            lblUserActions.Name = "lblUserActions";
            lblUserActions.Size = new Size(74, 15);
            lblUserActions.TabIndex = 4;
            lblUserActions.Text = "UserActions";
            lblUserActions.Click += label5_Click;
            // 
            // checkBoxPermissions
            // 
            checkBoxPermissions.AutoSize = true;
            checkBoxPermissions.Location = new Point(183, 24);
            checkBoxPermissions.Name = "checkBoxPermissions";
            checkBoxPermissions.Size = new Size(15, 14);
            checkBoxPermissions.TabIndex = 3;
            checkBoxPermissions.UseVisualStyleBackColor = true;
            checkBoxPermissions.CheckedChanged += checkBoxPermissions_CheckedChanged;
            // 
            // lblPermissions
            // 
            lblPermissions.AutoSize = true;
            lblPermissions.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblPermissions.Location = new Point(105, 22);
            lblPermissions.Name = "lblPermissions";
            lblPermissions.Size = new Size(72, 15);
            lblPermissions.TabIndex = 2;
            lblPermissions.Text = "Permissions";
            // 
            // lblLocalization
            // 
            lblLocalization.AutoSize = true;
            lblLocalization.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLocalization.Location = new Point(6, 22);
            lblLocalization.Name = "lblLocalization";
            lblLocalization.Size = new Size(72, 15);
            lblLocalization.TabIndex = 1;
            lblLocalization.Text = "Localization";
            // 
            // btnProperty
            // 
            btnProperty.Location = new Point(10, 201);
            btnProperty.Name = "btnProperty";
            btnProperty.Size = new Size(90, 25);
            btnProperty.TabIndex = 6;
            btnProperty.Text = "New Property";
            btnProperty.UseVisualStyleBackColor = true;
            btnProperty.Click += btnProperty_Click;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(9, 183);
            label8.Name = "label8";
            label8.Size = new Size(102, 15);
            label8.TabIndex = 7;
            label8.Text = "Add new property";
            // 
            // btnSave
            // 
            btnSave.Location = new Point(9, 618);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(99, 48);
            btnSave.TabIndex = 54;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnNewRelation
            // 
            btnNewRelation.Location = new Point(1, 445);
            btnNewRelation.Name = "btnNewRelation";
            btnNewRelation.Size = new Size(149, 25);
            btnNewRelation.TabIndex = 57;
            btnNewRelation.Text = "New Relation => Entities";
            btnNewRelation.UseVisualStyleBackColor = true;
            btnNewRelation.Click += btnNewRelation_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = SystemColors.InactiveCaption;
            label3.Location = new Point(151, 427);
            label3.Name = "label3";
            label3.Size = new Size(55, 15);
            label3.TabIndex = 58;
            label3.Text = "Relations";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(10, 427);
            label4.Name = "label4";
            label4.Size = new Size(97, 15);
            label4.TabIndex = 60;
            label4.Text = "Add new relation";
            // 
            // pnlScrollable
            // 
            pnlScrollable.AutoScroll = true;
            pnlScrollable.BorderStyle = BorderStyle.FixedSingle;
            pnlScrollable.Location = new Point(151, 201);
            pnlScrollable.Name = "pnlScrollable";
            pnlScrollable.Size = new Size(609, 210);
            pnlScrollable.TabIndex = 0;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.BackColor = SystemColors.InactiveCaption;
            label5.Location = new Point(151, 183);
            label5.Name = "label5";
            label5.Size = new Size(60, 15);
            label5.TabIndex = 61;
            label5.Text = "Properties";
            // 
            // pnlRelations
            // 
            pnlRelations.AutoScroll = true;
            pnlRelations.BorderStyle = BorderStyle.FixedSingle;
            pnlRelations.Location = new Point(151, 445);
            pnlRelations.Name = "pnlRelations";
            pnlRelations.Size = new Size(609, 221);
            pnlRelations.TabIndex = 62;
            // 
            // btnRelUsers
            // 
            btnRelUsers.Location = new Point(1, 487);
            btnRelUsers.Name = "btnRelUsers";
            btnRelUsers.Size = new Size(149, 25);
            btnRelUsers.TabIndex = 63;
            btnRelUsers.Text = "New Relation => Users";
            btnRelUsers.UseVisualStyleBackColor = true;
            btnRelUsers.Click += btnRelUsers_Click;
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.GradientActiveCaption;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(btnClearPartial);
            panel1.Controls.Add(rdioChild);
            panel1.Controls.Add(rdioParent);
            panel1.Location = new Point(9, 119);
            panel1.Name = "panel1";
            panel1.Size = new Size(136, 51);
            panel1.TabIndex = 64;
            // 
            // btnClearPartial
            // 
            btnClearPartial.Location = new Point(15, 23);
            btnClearPartial.Name = "btnClearPartial";
            btnClearPartial.Size = new Size(99, 23);
            btnClearPartial.TabIndex = 67;
            btnClearPartial.Text = "Clear selection";
            btnClearPartial.UseVisualStyleBackColor = true;
            btnClearPartial.Click += btnClearPartial_Click;
            // 
            // rdioChild
            // 
            rdioChild.AutoSize = true;
            rdioChild.Location = new Point(70, 3);
            rdioChild.Name = "rdioChild";
            rdioChild.Size = new Size(53, 19);
            rdioChild.TabIndex = 1;
            rdioChild.TabStop = true;
            rdioChild.Text = "Child";
            rdioChild.UseVisualStyleBackColor = true;
            rdioChild.CheckedChanged += rdioChild_CheckedChanged;
            // 
            // rdioParent
            // 
            rdioParent.AutoSize = true;
            rdioParent.Location = new Point(5, 3);
            rdioParent.Name = "rdioParent";
            rdioParent.Size = new Size(59, 19);
            rdioParent.TabIndex = 0;
            rdioParent.TabStop = true;
            rdioParent.Text = "Parent";
            rdioParent.UseVisualStyleBackColor = true;
            rdioParent.CheckedChanged += rdioParent_CheckedChanged;
            // 
            // cmboParent
            // 
            cmboParent.FormattingEnabled = true;
            cmboParent.Location = new Point(251, 122);
            cmboParent.Name = "cmboParent";
            cmboParent.Size = new Size(146, 23);
            cmboParent.TabIndex = 65;
            cmboParent.Visible = false;
            // 
            // lblParent
            // 
            lblParent.AutoSize = true;
            lblParent.Location = new Point(169, 126);
            lblParent.Name = "lblParent";
            lblParent.Size = new Size(75, 15);
            lblParent.TabIndex = 66;
            lblParent.Text = "Select Parent";
            lblParent.Visible = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            BackgroundImageLayout = ImageLayout.None;
            ClientSize = new Size(772, 701);
            Controls.Add(lblParent);
            Controls.Add(cmboParent);
            Controls.Add(panel1);
            Controls.Add(btnRelUsers);
            Controls.Add(pnlRelations);
            Controls.Add(label5);
            Controls.Add(pnlScrollable);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(btnNewRelation);
            Controls.Add(btnSave);
            Controls.Add(label8);
            Controls.Add(btnProperty);
            Controls.Add(groupBoxOption);
            Controls.Add(txtEntityName);
            Controls.Add(label2);
            Controls.Add(txtDir);
            Controls.Add(label1);
            DoubleBuffered = true;
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            groupBoxOption.ResumeLayout(false);
            groupBoxOption.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CheckBox checkBoxLocalization;
        private Label label1;
        private TextBox txtDir;
        private Label label2;
        private TextBox txtEntityName;
        private GroupBox groupBoxOption;
        private Label lblLocalization;
        private Label lblNotifications;
        private Label lblVersioning;
        private Label lblUserActions;
        private CheckBox checkBoxPermissions;
        private Label lblPermissions;
        private CheckBox checkBoxNotifications;
        private CheckBox checkBoxVersioning;
        private CheckBox checkBoxUserActions;
        private Button btnProperty;
        private Label label8;
        private Button btnSave;
        private Button btnNewRelation;
        private Label label3;
        private Label label4;
        private Panel pnlScrollable;
        private Label label5;
        private Panel pnlRelations;
        private CheckBox checkBoxBulk;
        private Label lblBulk;
        private Button btnRelUsers;
        private Panel panel1;
        private RadioButton rdioChild;
        private RadioButton rdioParent;
        private ComboBox cmboParent;
        private Label lblParent;
        private Button btnClearPartial;
    }
}
