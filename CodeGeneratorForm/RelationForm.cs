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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CodeGeneratorForm
{
    public partial class RelationForm : Form
    {
        public Relation Relation { get; set; } = new Relation();
        public bool IsSaved { get; set; } = false;
        public RelationForm()
        {
            InitializeComponent();
        }

        private void btnAddRelation_Click(object sender, EventArgs e)
        {
            if(cmboRel.SelectedItem != null && !string.IsNullOrWhiteSpace(txtRelEnt.Text)) 
            {
                Relation.Type = (RelationType)(cmboRel.SelectedIndex);
                Relation.RelatedEntity = txtRelEnt.Text;
                IsSaved = true;
                this.Close();
            }
            else
                MessageBox.Show("You do not add relation yet");
        }
    }
}
