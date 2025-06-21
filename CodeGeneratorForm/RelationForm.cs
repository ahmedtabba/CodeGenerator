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
        private readonly SharedClasses.Properties properties = new SharedClasses.Properties();
        private readonly string EntityName;
        public bool IsSaved { get; set; } = false;

        private string _projectPath;
        private bool isLoading = false;
        private string? currentEntityName;
        private string? currentDisplayedProp;
        public RelationForm(string projectPath, SharedClasses.Properties _properties, string entityName)
        {
            InitializeComponent();
            _projectPath = projectPath;
            properties = _properties;
            EntityName = entityName;
        }

        private void LoadExistingEntities(string? entityName = null,string? displayedProp = null)
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
                    if (entityName != null)
                        cmboRelEnt.SelectedItem = entityName;
                    if (displayedProp != null)
                        cmboProp.SelectedItem = displayedProp;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading entities: {ex.Message}");
            }
        }

        private void btnAddRelation_Click(object sender, EventArgs e)
        {
            if (cmboRel.SelectedItem != null)
            {
                if (cmboRel.SelectedIndex == 0)
                {
                    Relation.Type = (RelationType)(cmboRel.SelectedIndex);
                    Relation.RelatedEntity = EntityName;
                    Relation.DisplayedProperty = cmboSelfProps.Text;
                    Relation.IsGeneratedInTable = chkGenerateRelInTable.Checked;
                    Relation.HiddenInTable = chkHideRelInTable.Checked;
                    IsSaved = true;
                    this.Close();
                }
                else if (cmboRelEnt.SelectedItem != null)
                {
                    Relation.Type = (RelationType)(cmboRel.SelectedIndex);
                    Relation.RelatedEntity = cmboRelEnt.SelectedItem.ToString();
                    Relation.DisplayedProperty = cmboProp.Text;
                    Relation.IsGeneratedInTable = chkGenerateRelInTable.Checked;
                    Relation.HiddenInTable = chkHideRelInTable.Checked;
                    IsSaved = true;
                    this.Close();
                }

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
            isLoading = true;

            LoadExistingEntities();

            if (Relation != null)
            {
                if (Relation.RelatedEntity != null)
                {
                    if (Relation.Type == RelationType.OneToOneSelfJoin)
                    {
                        cmboSelfProps.Text = Relation.DisplayedProperty;
                    }
                    else
                    {
                        cmboRelEnt.SelectedItem = Relation.RelatedEntity;
                        currentEntityName = Relation.RelatedEntity;
                        cmboProp.SelectedItem = Relation.DisplayedProperty;
                        currentDisplayedProp = Relation.DisplayedProperty;

                    }

                }
                this.chkGenerateRelInTable.Checked = Relation.IsGeneratedInTable;
                this.chkHideRelInTable.Checked = Relation.HiddenInTable;
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
                isLoading = false;
            }
        }

        private void cmboRelEnt_SelectedIndexChanged(object sender, EventArgs e)
        {
            //chkHideRelInTable.Checked = false;
            var metadata = MetadataManager.LoadMetadata(_projectPath);
            cmboProp.Items.Clear();
            cmboProp.Text = string.Empty;
            if (metadata != null && metadata.Entities != null)
            {
                var entityMetadata = metadata.Entities.First(e => e.Name == cmboRelEnt.Text);
                cmboProp.Items.AddRange(entityMetadata.Properties.Where(p => p.Type == "string").Select(p => p.Name).ToArray());
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
                cmboSelfProps.Items.AddRange(properties.PropertiesList.Where(p => p.Type == "string").Select(p => p.Name).ToArray());
                lblRelEnt.Visible = false;
                lblPropDisplay.Visible = false;
                cmboRelEnt.Visible = false;
                cmboProp.Visible = false;
                cmboRelEnt.Items.Clear();
                cmboProp.Items.Clear();
                cmboRelEnt.Text = string.Empty;
                cmboProp.Text = string.Empty;
                if (!isLoading)
                {
                    chkGenerateRelInTable.Checked = true;
                    chkHideRelInTable.Checked = false;
                }
                    
            }
            else
            {
                lblSelfProp.Visible = false;
                cmboSelfProps.Visible = false;
                cmboSelfProps.Items.Clear();
                cmboSelfProps.Text = string.Empty;
                lblRelEnt.Visible = true;
                lblPropDisplay.Visible = true;
                cmboRelEnt.Visible = true;
                cmboProp.Visible = true;
                if (!isLoading)
                {
                    chkGenerateRelInTable.Checked = true;
                    chkHideRelInTable.Checked = false;
                }
                LoadExistingEntities(currentEntityName,currentDisplayedProp);
                currentEntityName = null;
                currentDisplayedProp = null;
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
    }
}
