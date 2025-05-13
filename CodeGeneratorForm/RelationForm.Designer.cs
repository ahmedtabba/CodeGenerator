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
            txtRelEnt = new TextBox();
            cmboRel = new ComboBox();
            lblRelation = new Label();
            lblRelEnt = new Label();
            btnAddRelation = new Button();
            SuspendLayout();
            // 
            // txtRelEnt
            // 
            txtRelEnt.Location = new Point(106, 75);
            txtRelEnt.Name = "txtRelEnt";
            txtRelEnt.Size = new Size(151, 23);
            txtRelEnt.TabIndex = 49;
            // 
            // cmboRel
            // 
            cmboRel.FormattingEnabled = true;
            cmboRel.Items.AddRange(new object[] { "OneToOneSelfJoin", "OneToOne (entity is child)", "OneToOneNullable (entity is child)", "OneToMany (entity is parent)", "OneToManyNullable (entity is parent)", "ManyToOne (entity is child)", "ManyToOneNullable (entity is child)", "ManyToMany" });
            cmboRel.Location = new Point(106, 33);
            cmboRel.Name = "cmboRel";
            cmboRel.Size = new Size(267, 23);
            cmboRel.TabIndex = 48;
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
            btnAddRelation.Location = new Point(15, 119);
            btnAddRelation.Name = "btnAddRelation";
            btnAddRelation.Size = new Size(84, 49);
            btnAddRelation.TabIndex = 51;
            btnAddRelation.Text = "Add";
            btnAddRelation.UseVisualStyleBackColor = true;
            btnAddRelation.Click += btnAddRelation_Click;
            // 
            // RelationForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(438, 275);
            Controls.Add(btnAddRelation);
            Controls.Add(lblRelEnt);
            Controls.Add(txtRelEnt);
            Controls.Add(cmboRel);
            Controls.Add(lblRelation);
            Name = "RelationForm";
            Text = "RelationForm";
            Load += RelationForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtRelEnt;
        private ComboBox cmboRel;
        private Label lblRelation;
        private Label lblRelEnt;
        private Button btnAddRelation;
    }
}