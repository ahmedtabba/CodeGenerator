using SharedClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace CodeGeneratorForm
{
    public partial class PropertyForm : Form
    {
        public PropertyInfo PropertyInfo { get; set; } = new PropertyInfo();
        public bool HasLocalization { get; set; } = false;
        public PropertyForm()
        {
            InitializeComponent();
        }
        public PropertyForm(bool hasLocalization)
        {
            this.HasLocalization = hasLocalization;
            InitializeComponent();
        }

        private void cmboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmboType_SelectedIndexChanged != null)
            {
                if (cmboType.SelectedIndex == 7)
                    cmboListType.Visible = true;
                else
                    cmboListType.Visible = false;
                if (cmboType.SelectedIndex == 6)
                {
                    lblEnum.Visible = true;
                    txtEnums.Visible = true;
                }
                else
                {
                    lblEnum.Visible = false;
                    txtEnums.Visible = false;
                }
            }
        }

        private void chkValidation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chkValidation_SelectedIndexChanged != null)
            {
                if (chkValidation.CheckedItems.Contains("Has max length"))
                {
                    lblMaxLen.Visible = true;
                    txtMaxLen.Visible = true;
                }
                else
                {
                    lblMaxLen.Visible = false;
                    txtMaxLen.Visible = false;
                }
                if (chkValidation.CheckedItems.Contains("Has min length"))
                {
                    lblMinLen.Visible = true;
                    txtMinLen.Visible = true;
                }
                else
                {
                    lblMinLen.Visible = false;
                    txtMinLen.Visible = false;
                }
                if (chkValidation.CheckedItems.Contains("Has max range"))
                {
                    lblMaxRng.Visible = true;
                    txtMaxRng.Visible = true;
                }
                else
                {
                    lblMaxRng.Visible = false;
                    txtMaxRng.Visible = false;
                }
                if (chkValidation.CheckedItems.Contains("Has min range"))
                {
                    lblMinRng.Visible = true;
                    txtMinRng.Visible = true;
                }
                else
                {
                    lblMinRng.Visible = false;
                    txtMinRng.Visible = false;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            //(List<(string Type, string Name, PropertyValidation Validation)> properties, List<string> localizedProp, List<(string prop, List<string> enumValues)> propEnums) result;
            (string Type, string Name, PropertyValidation Validation) property = new();
            //Fill Localized
            bool localizedProp = chkLocalized.Checked;

            (string prop, List<string> enumValues) enumProp = new();
            PropertyValidation propValidation = new PropertyValidation();
            int numOfValidation = 0;

            //Fill Validation
            if (chkValidation.CheckedItems.Contains("Required"))
            {
                propValidation.Required = true;
                numOfValidation++;
            }
            if (chkValidation.CheckedItems.Contains("Unique"))
            {
                propValidation.Unique = true;
                numOfValidation++;
            }
            if (chkValidation.CheckedItems.Contains("Has max length"))
            {
                propValidation.MaxLength = Int32.Parse(txtMaxLen.Text);
                numOfValidation++;
            }
            if (chkValidation.CheckedItems.Contains("Has min length"))
            {
                propValidation.MinLength = Int32.Parse(txtMinLen.Text);
                numOfValidation++;
            }
            if (chkValidation.CheckedItems.Contains("Has max range"))
            {
                propValidation.MaxRange = Int32.Parse(txtMaxRng.Text);
                numOfValidation++;
            }
            if (chkValidation.CheckedItems.Contains("Has min range"))
            {
                propValidation.MinRange = Int32.Parse(txtMinRng.Text); ;
                numOfValidation++;
            }
            
            //Fill Name
            property.Name = txtName.Text;

            // Fill Type
            switch (cmboType.SelectedItem)
            {
                case "Image (single file)":
                    property.Type = "GPG";
                    break;
                case "List of images (multi files)":
                    property.Type = "PNGs";
                    break;
                case "Video":
                    property.Type = "VD";
                    break;

                case "List of":
                    property.Type = propValidation.Required ? $"List<{cmboListType.SelectedItem}>" : $"List<{cmboListType.SelectedItem}>?";
                    break;

                case "enum":
                    property.Type = propValidation.Required ? "int" : "int?";
                    var enumValuesString = txtEnums.Text;
                    string[] words = enumValuesString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> enumValues = words.Select(w => w.Trim()).ToList();
                    enumProp.prop = property.Name;
                    enumProp.enumValues = enumValues;
                    break;
                default:
                    property.Type = propValidation.Required ? $"{cmboType.SelectedItem}" : $"{cmboType.SelectedItem}?";
                    break;
            }
            if (numOfValidation > 0)
                property.Validation = propValidation;
            else
                propValidation = null;
            
            this.PropertyInfo.GeneralInfo = property;
            this.PropertyInfo.EnumValues = enumProp;
            this.PropertyInfo.Localized = localizedProp;
            this.PropertyInfo.IsSaved = true;
            this.Close();
        }

    }
}