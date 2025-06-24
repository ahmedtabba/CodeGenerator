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
                if (cmboType.SelectedIndex == 12 || cmboType.SelectedIndex == 13)
                    lblFileHint.Visible = true;
                else
                    lblFileHint.Visible = false;
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
                case "List of videos":
                    property.Type = "VDs";
                    break;
                case "File":
                    property.Type = "FL";
                    break;
                case "List of files":
                    property.Type = "FLs";
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
            this.PropertyInfo.GeneratedColumn = chkHasColumn.Checked;
            this.PropertyInfo.HiddenColumn = chkHiddenColumn.Checked;
            this.PropertyInfo.EnumValues = enumProp;
            this.PropertyInfo.Localized = localizedProp;
            this.PropertyInfo.IsSaved = true;
            this.Close();
        }

        private void PropertyForm_Load(object sender, EventArgs e)
        {
            if (PropertyInfo.GeneralInfo.Name == null || PropertyInfo.GeneralInfo.Type == null)
            {
                this.chkLocalized.Visible = this.HasLocalization;
                return;

            }
            this.txtName.Text = PropertyInfo.GeneralInfo.Name;
            this.chkHasColumn.Checked = PropertyInfo.GeneratedColumn;
            this.chkHiddenColumn.Checked = PropertyInfo.HiddenColumn;
            this.chkLocalized.Visible = this.HasLocalization;
            this.chkLocalized.Checked = PropertyInfo.Localized ? true : false;

            if (PropertyInfo.GeneralInfo.Type.StartsWith("List"))
            {
                cmboType.SelectedIndex = 7;
                cmboListType.Visible = true;
                cmboListType.SelectedItem = GetGenericType(PropertyInfo.GeneralInfo.Type);
                FillValidation();
                return;
            }
            if (PropertyInfo.GeneralInfo.Type.StartsWith("int") && PropertyInfo.EnumValues.enumValues != null && PropertyInfo.EnumValues.enumValues.Any())
            {
                cmboType.SelectedIndex = 6;
                txtEnums.Visible = true;
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < PropertyInfo.EnumValues.enumValues.Count; i++)
                {
                    if (sb.Length > 0)
                        sb.Append(",");
                    sb.Append(PropertyInfo.EnumValues.enumValues[i]);
                }
                txtEnums.Text = sb.ToString();
                FillValidation();
                return;
            }
            if (PropertyInfo.GeneralInfo.Type == "GPG")
            {
                cmboType.SelectedIndex = 8;
                FillValidation();
                return;
            }
            if (PropertyInfo.GeneralInfo.Type == "PNGs")
            {
                cmboType.SelectedIndex = 9;
                FillValidation();
                return;
            }
            if (PropertyInfo.GeneralInfo.Type == "VD")
            {
                cmboType.SelectedIndex = 10;
                FillValidation();
                return;
            }
            if (PropertyInfo.GeneralInfo.Type == "VDs")
            {
                cmboType.SelectedIndex = 11;
                FillValidation();
                return;
            }
            if (PropertyInfo.GeneralInfo.Type == "FL")
            {
                cmboType.SelectedIndex = 12;
                FillValidation();
                return;
            }
            if (PropertyInfo.GeneralInfo.Type == "FLs")
            {
                cmboType.SelectedIndex = 13;
                FillValidation();
                return;
            }
            var type = PropertyInfo.GeneralInfo.Type.TrimEnd('?');
            cmboType.SelectedItem = type;
            FillValidation();
        }

        private void FillValidation()
        {
            if (PropertyInfo.GeneralInfo.Validation != null)
            {
                if (PropertyInfo.GeneralInfo.Validation.Required)
                    chkValidation.SetItemChecked(0, true);
                if (PropertyInfo.GeneralInfo.Validation.Unique)
                    chkValidation.SetItemChecked(1, true);
                if (PropertyInfo.GeneralInfo.Validation.MaxLength != null)
                {
                    chkValidation.SetItemChecked(2, true);
                    txtMaxLen.Text = PropertyInfo.GeneralInfo.Validation.MaxLength.ToString();
                    txtMaxLen.Visible = true;
                    lblMaxLen.Visible = true;
                }
                if (PropertyInfo.GeneralInfo.Validation.MinLength != null)
                {
                    chkValidation.SetItemChecked(3, true);
                    txtMinLen.Text = PropertyInfo.GeneralInfo.Validation.MinLength.ToString();
                    txtMinLen.Visible = true;
                    lblMinLen.Visible = true;
                }
                if (PropertyInfo.GeneralInfo.Validation.MaxRange != null)
                {
                    chkValidation.SetItemChecked(4, true);
                    txtMaxRng.Text = PropertyInfo.GeneralInfo.Validation.MaxRange.ToString();
                    txtMaxRng.Visible = true;
                    lblMaxRng.Visible = true;
                }
                if (PropertyInfo.GeneralInfo.Validation.MinRange != null)
                {
                    chkValidation.SetItemChecked(5, true);
                    txtMinRng.Text = PropertyInfo.GeneralInfo.Validation.MinRange.ToString();
                    txtMinRng.Visible = true;
                    lblMinRng.Visible = true;
                }
            }
        }

        string GetGenericType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName) || !typeName.Contains("<"))
                return string.Empty;

            int startIndex = typeName.IndexOf("<") + 1;
            int endIndex = typeName.IndexOf(">");

            if (startIndex > 0 && endIndex > 0)
            {
                return typeName.Substring(startIndex, endIndex - startIndex);
            }

            return string.Empty;
        }

        private void chkHasColumn_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkHasColumn.Checked)
            {
                chkHiddenColumn.Checked = false;
                chkHiddenColumn.Visible = false;
            }
            else
            {
                chkHiddenColumn.Checked = false;
                chkHiddenColumn.Visible = true;
            }
        }
    }
}