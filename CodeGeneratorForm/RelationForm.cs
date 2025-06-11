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

        private string _projectPath;

        public RelationForm(string projectPath)
        {
            InitializeComponent();
            _projectPath = projectPath;
        }

        private void LoadExistingEntities()
        {
            try
            {
                var metadata = MetadataManager.LoadMetadata(_projectPath);
                if (metadata != null && metadata.Entities != null)
                {
                    cmboRelEnt.Items.Clear();
                    foreach (var entity in metadata.Entities)
                    {
                        cmboRelEnt.Items.Add(entity.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading entities: {ex.Message}");
            }
        }

        private void btnAddRelation_Click(object sender, EventArgs e)
        {
            if (cmboRel.SelectedItem != null && cmboRelEnt.SelectedItem != null)
            {
                Relation.Type = (RelationType)(cmboRel.SelectedIndex);
                Relation.RelatedEntity = cmboRelEnt.SelectedItem.ToString();
                Relation.DisplayedProperty = cmboProp.Text;
                Relation.HiddenInTable = chkHideRelInTable.Checked;
                IsSaved = true;
                this.Close();
            }
            else
                MessageBox.Show("Please select both relation type and entity");

            //Relation.Type = (RelationType)(cmboRel.SelectedIndex);
            //Relation.RelatedEntity = textBox1.Text;
            //IsSaved = true;
            //this.Close();
        }

        private void RelationForm_Load(object sender, EventArgs e)
        {
            LoadExistingEntities();

            if (Relation != null)
            {
                if (Relation.RelatedEntity != null)
                    cmboRelEnt.SelectedItem = Relation.RelatedEntity;
                switch (Relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        cmboRel.SelectedIndex = 0;
                        break;
                    case RelationType.OneToOne:
                        cmboRel.SelectedIndex = 1;
                        break;
                    case RelationType.OneToOneNullable:
                        cmboRel.SelectedIndex = 2;
                        break;
                    case RelationType.OneToMany:
                        cmboRel.SelectedIndex = 3;
                        break;
                    case RelationType.OneToManyNullable:
                        cmboRel.SelectedIndex = 4;
                        break;
                    case RelationType.ManyToOne:
                        cmboRel.SelectedIndex = 5;
                        break;
                    case RelationType.ManyToOneNullable:
                        cmboRel.SelectedIndex = 6;
                        break;
                    case RelationType.ManyToMany:
                        cmboRel.SelectedIndex = 7;
                        break;
                    default:
                        break;
                }
            }
        }

        private void cmboRelEnt_SelectedIndexChanged(object sender, EventArgs e)
        {
            chkHideRelInTable.Checked = false;
            var metadata = MetadataManager.LoadMetadata(_projectPath);
            cmboProp.Items.Clear();
            cmboProp.Text = string.Empty;
            if (metadata != null && metadata.Entities != null)
            {
                var x = metadata.Entities.First(e => e.Name == cmboRelEnt.Text);
                cmboProp.Items.AddRange(x.Properties.Select(p => p.Name).ToArray());
            }
            else
                cmboProp.Items.Clear();
        }

        private void cmboRel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmboRel.SelectedIndex == 0)
            {
                lblSelfProp.Visible = true;
                cmboSelfProps.Visible = true;
                cmboSelfProps.Items.Clear();
            }
            else
            {
                lblSelfProp.Visible = false;
                cmboSelfProps.Visible = false;
                cmboSelfProps.Items.Clear();
            }
        }
    }
}
