namespace CodeGeneratorForm
{
    partial class RelationForm
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
            cmboRelEnt = new ComboBox();
            cmboRel = new ComboBox();
            lblRelation = new Label();
            lblRelEnt = new Label();
            btnAddRelation = new Button();
            lblPropDisplay = new Label();
            cmboProp = new ComboBox();
            chkHideRelInTable = new CheckBox();
            cmboSelfProps = new ComboBox();
            lblSelfProp = new Label();
            SuspendLayout();
            // 
            // cmboRelEnt
            // 
            cmboRelEnt.FormattingEnabled = true;
            cmboRelEnt.Location = new Point(106, 75);
            cmboRelEnt.Name = "cmboRelEnt";
            cmboRelEnt.Size = new Size(163, 23);
            cmboRelEnt.TabIndex = 49;
            cmboRelEnt.SelectedIndexChanged += cmboRelEnt_SelectedIndexChanged;
            // 
            // cmboRel
            // 
            cmboRel.FormattingEnabled = true;
            cmboRel.Items.AddRange(new object[] { "OneToOneSelfJoin", "OneToOne (entity is child)", "OneToOneNullable (entity is child)", "OneToMany (entity is parent)", "OneToManyNullable (entity is parent)", "ManyToOne (entity is child)", "ManyToOneNullable (entity is child)", "ManyToMany" });
            cmboRel.Location = new Point(106, 33);
            cmboRel.Name = "cmboRel";
            cmboRel.Size = new Size(163, 23);
            cmboRel.TabIndex = 48;
            cmboRel.SelectedIndexChanged += cmboRel_SelectedIndexChanged;
            // 
            // lblRelation
            // 
            lblRelation.AutoSize = true;
            lblRelation.Location = new Point(15, 36);
            lblRelation.Name = "lblRelation";
            lblRelation.Size = new Size(84, 15);
            lblRelation.TabIndex = 47;
            lblRelation.Text = "Relation1 Type";
            // 
            // lblRelEnt
            // 
            lblRelEnt.AutoSize = true;
            lblRelEnt.Location = new Point(15, 78);
            lblRelEnt.Name = "lblRelEnt";
            lblRelEnt.Size = new Size(72, 15);
            lblRelEnt.TabIndex = 50;
            lblRelEnt.Text = "Entity Name";
            // 
            // btnAddRelation
            // 
            btnAddRelation.Location = new Point(15, 163);
            btnAddRelation.Name = "btnAddRelation";
            btnAddRelation.Size = new Size(84, 49);
            btnAddRelation.TabIndex = 51;
            btnAddRelation.Text = "Add";
            btnAddRelation.UseVisualStyleBackColor = true;
            btnAddRelation.Click += btnAddRelation_Click;
            // 
            // lblPropDisplay
            // 
            lblPropDisplay.AutoSize = true;
            lblPropDisplay.Location = new Point(15, 121);
            lblPropDisplay.Name = "lblPropDisplay";
            lblPropDisplay.Size = new Size(106, 15);
            lblPropDisplay.TabIndex = 53;
            lblPropDisplay.Text = "Displayed Property";
            // 
            // cmboProp
            // 
            cmboProp.FormattingEnabled = true;
            cmboProp.Location = new Point(127, 118);
            cmboProp.Name = "cmboProp";
            cmboProp.Size = new Size(142, 23);
            cmboProp.TabIndex = 54;
            // 
            // chkHideRelInTable
            // 
            chkHideRelInTable.AutoSize = true;
            chkHideRelInTable.Location = new Point(275, 77);
            chkHideRelInTable.Name = "chkHideRelInTable";
            chkHideRelInTable.Size = new Size(141, 19);
            chkHideRelInTable.TabIndex = 55;
            chkHideRelInTable.Text = "Hide Relation In Table";
            chkHideRelInTable.UseVisualStyleBackColor = true;
            // 
            // cmboSelfProps
            // 
            cmboSelfProps.FormattingEnabled = true;
            cmboSelfProps.Location = new Point(336, 33);
            cmboSelfProps.Name = "cmboSelfProps";
            cmboSelfProps.Size = new Size(121, 23);
            cmboSelfProps.TabIndex = 56;
            // 
            // lblSelfProp
            // 
            lblSelfProp.AutoSize = true;
            lblSelfProp.Location = new Point(278, 36);
            lblSelfProp.Name = "lblSelfProp";
            lblSelfProp.Size = new Size(48, 15);
            lblSelfProp.TabIndex = 57;
            lblSelfProp.Text = "Propety";
            // 
            // RelationForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(469, 275);
            Controls.Add(lblSelfProp);
            Controls.Add(cmboSelfProps);
            Controls.Add(chkHideRelInTable);
            Controls.Add(cmboProp);
            Controls.Add(lblPropDisplay);
            Controls.Add(btnAddRelation);
            Controls.Add(lblRelEnt);
            Controls.Add(cmboRelEnt);
            Controls.Add(cmboRel);
            Controls.Add(lblRelation);
            Name = "RelationForm";
            Text = "RelationForm";
            Load += RelationForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox cmboRelEnt;
        private ComboBox cmboRel;
        private Label lblRelation;
        private Label lblRelEnt;
        private Button btnAddRelation;
        private Label lblPropDisplay;
        private ComboBox cmboProp;
        private CheckBox chkHideRelInTable;
        private ComboBox cmboSelfProps;
        private Label lblSelfProp;
    }
}