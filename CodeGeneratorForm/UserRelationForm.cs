using SharedClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodeGeneratorForm
{
    public partial class UserRelationForm : Form
    {
        public Relation Relation { get; set; } = new Relation();
        public bool IsSaved { get; set; } = false;


        public UserRelationForm()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (cmboRelType.SelectedItem != null)
            {
                Relation.Type = cmboRelType.SelectedIndex == 0 ? RelationType.UserSingle : cmboRelType.SelectedIndex == 1 ? RelationType.UserSingleNullable : RelationType.UserMany;
                Relation.RelatedEntity = "User";
                Relation.DisplayedProperty = txtRelProp.Text;
                Relation.IsGeneratedInTable = chkGenerateRelInTable.Checked;
                Relation.HiddenInTable = chkHideRelInTable.Checked;
                IsSaved = true;
                this.Close();
            }
            else
                MessageBox.Show("Please select relation type and property");
        }

        private void UserRelationForm_Load(object sender, EventArgs e)
        {
            if (Relation.RelatedEntity == "User")
            {
                this.chkGenerateRelInTable.Checked = Relation.IsGeneratedInTable;
                this.chkHideRelInTable.Checked = Relation.HiddenInTable;
                if (Relation.Type == RelationType.UserSingle)
                    cmboRelType.SelectedIndex = 0;
                else if (Relation.Type == RelationType.UserSingleNullable)
                    cmboRelType.SelectedIndex = 1;
                else
                    cmboRelType.SelectedIndex = 2;

                txtRelProp.Text = Relation.DisplayedProperty;
            }
        }



        private void chkGenerateRelInTable_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkGenerateRelInTable.Checked)
            {
                chkHideRelInTable.Checked = false;
                chkHideRelInTable.Visible = false;
            }
            else
            {
                chkHideRelInTable.Checked = false;
                chkHideRelInTable.Visible = true;
            }
        }

        private void cmboRelType_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtRelProp.Clear();
        }
    }
}
