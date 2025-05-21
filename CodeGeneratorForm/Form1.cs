using DomainGenerator;
using ApiGenerator;
using InfrastructureGenerator;
using SharedClasses;
using Application = ApplicationGenerator.Application;
using ApplicationGenerator;
using System.Text.RegularExpressions;
using Frontend.VueJsHelper;

namespace CodeGeneratorForm
{
    public partial class Form1 : Form
    {
        private static readonly Regex ClassPattern = new Regex(
    @"public\s+class\s+(\w+)\s*\{",
    RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public List<Relation> Relations { get; set; } = new List<Relation>();
        public SharedClasses.Properties properties { get; set; } = new SharedClasses.Properties();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void btnProperty_Click(object sender, EventArgs e)
        {
            var isLocalized = this.checkBoxLocalization.Checked;
            PropertyForm propertyForm = new PropertyForm(isLocalized);
            propertyForm.ShowDialog();
            var propertyInfo = propertyForm.PropertyInfo;

            if (propertyInfo.IsSaved)
            {

                properties.PropertiesList.Add((propertyInfo.GeneralInfo.Type, propertyInfo.GeneralInfo.Name, propertyInfo.GeneralInfo.Validation));
                if (propertyInfo.Localized)
                    properties.LocalizedProp.Add(propertyInfo.GeneralInfo.Name);
                if (propertyInfo.EnumValues.enumValues != null && propertyInfo.EnumValues.enumValues.Any())
                    properties.EnumProps.Add(propertyInfo.EnumValues);

                UpdatePropertiesDisplay();
                //richtxtProps.AppendText($"Property {propertyInfo.GeneralInfo.Name} has been added." + Environment.NewLine);
            }
        }


        private void btnSave_Click(object sender, EventArgs e)
        {

            var hasLocalization = checkBoxLocalization.Checked;
            var hasPermissions = checkBoxPermissions.Checked;
            var hasVersioning = checkBoxVersioning.Checked;
            var hasUserAction = checkBoxUserActions.Checked;
            var hasNotification = checkBoxNotifications.Checked;
            var entityName = txtEntityName.Text;
            var solutionDir = $"{txtDir.Text}";
           // VueJsHelper.VueJsSolutionPath = "C:\\Ahmed\\Work\\VueJsTemplate\\EvaVehicles.Admin\\src";
           
            if (!ValidateSolution()) 
            {
                ClearForm();
                return;
            }
            #region Generate Code

            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";

            var domainPath = Path.Combine(solutionDir, "Domain", "Entities");
            var repoInterfacePath = Path.Combine(solutionDir, "Application", "Common", "Interfaces", "IRepositories");
            var repoPath = Path.Combine(solutionDir, "Infrastructure", "Repositories");
            var createCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"Create{entityName}");
            var updateCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"Update{entityName}");
            var deleteCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"Delete{entityName}");
            var queryPath = Path.Combine(solutionDir, "Application", entityPlural, "Queries");


            Directory.CreateDirectory(domainPath);
            Directory.CreateDirectory(repoInterfacePath);
            Directory.CreateDirectory(createCommandPath);
            Directory.CreateDirectory(updateCommandPath);
            Directory.CreateDirectory(deleteCommandPath);
            try
            {
                if (hasPermissions)
                {
                    Infrastructure.GeneratePermission(entityName, domainPath, hasLocalization);
                }
                Domain.GenerateEntityClass(entityName, domainPath, (properties.PropertiesList, properties.LocalizedProp, properties.EnumProps), hasLocalization, Relations);
                //GenerateEntityLocalizationClass(entityName, domainPath);
                Infrastructure.UpdateAppDbContext(entityName, domainPath);
                if (hasLocalization)
                    Infrastructure.UpdateAppDbContext($@"{entityName}Localization", domainPath);
                List<string> relatedEntitiesList = new List<string>();
                foreach (var relation in Relations)
                {
                    if (relation.Type == RelationType.ManyToMany)
                    {
                        relatedEntitiesList.Add(relation.RelatedEntity);
                        List<(string Type, string Name, PropertyValidation Validation)> props = new List<(string Type, string Name, PropertyValidation Validation)>
                {
                    ("Guid",$"{entityName}Id",new PropertyValidation()),
                    ("Guid",$"{relation.RelatedEntity}Id",new PropertyValidation())
                };
                        Domain.GenerateEntityClass($"{entityName}{relation.RelatedEntity}", domainPath, (props, new List<string>(), new List<(string, List<string>)>()), false, new List<Relation>());
                        Infrastructure.UpdateAppDbContext($"{entityName}{relation.RelatedEntity}", domainPath);
                        Application.GenerateIRepositoryInterface($"{entityName}{relation.RelatedEntity}", repoInterfacePath);
                        Infrastructure.GenerateRepository($"{entityName}{relation.RelatedEntity}", repoPath);
                        Infrastructure.UpdateDependencyInjection($"{entityName}{relation.RelatedEntity}", domainPath);
                    }
                }

                Infrastructure.GenerateConfiguration(entityName, domainPath, relatedEntitiesList);
                Application.GenerateIRepositoryInterface(entityName, repoInterfacePath);
                if (hasLocalization)
                    Application.GenerateIRepositoryInterface($@"{entityName}Localization", repoInterfacePath);
                Infrastructure.GenerateRepository(entityName, repoPath);
                if (hasLocalization)
                    Infrastructure.GenerateRepository($@"{entityName}Localization", repoPath);
                Infrastructure.UpdateDependencyInjection(entityName, domainPath);
                if (hasLocalization)
                    Infrastructure.UpdateDependencyInjection($@"{entityName}Localization", domainPath);
                if (hasVersioning)
                    ApplicationAssistant.GenerateVersionNeeds(entityName, domainPath, properties.PropertiesList, Relations);
                if (hasNotification)
                    ApplicationAssistant.GenerateNotificationNeeds(entityName, domainPath);
                if (hasUserAction)
                    ApplicationAssistant.GenerateUserActionNeeds(entityName, domainPath);
                if (hasLocalization)
                    Infrastructure.UpdateLocalizationService(entityName, domainPath, properties.LocalizedProp);
                if (hasNotification || hasVersioning || hasUserAction)
                {
                    ApplicationAssistant.GenerateEvents(entityName, domainPath, hasVersioning);
                    ApplicationAssistant.GenerateHandlers(entityName, domainPath, properties.PropertiesList, Relations, hasVersioning, hasUserAction, hasNotification);
                }
                Application.GenerateCreateCommand(entityName, entityPlural, createCommandPath, properties.PropertiesList, properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                Application.GenerateCreateCommandValidator(entityName, entityPlural, createCommandPath, properties.PropertiesList, Relations);

                Application.GenerateUpdateCommand(entityName, entityPlural, updateCommandPath, properties.PropertiesList, properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                Application.GenerateUpdateCommandValidator(entityName, entityPlural, updateCommandPath, properties.PropertiesList, Relations);


                Application.GenerateDeleteCommand(entityName, entityPlural, deleteCommandPath, properties.PropertiesList, hasVersioning, hasNotification, hasUserAction);
                Application.GenerateDeleteCommandValidator(entityName, entityPlural, deleteCommandPath, properties.PropertiesList);


                Application.GenerateGetByIdQuery(entityName, entityPlural, queryPath, hasLocalization, properties.PropertiesList, properties.EnumProps, Relations);

                Application.GenerateGetWithPaginationQuery(entityName, entityPlural, queryPath, hasLocalization, Relations);
                Application.GenerateBaseDto(entityName, entityPlural, properties.PropertiesList, properties.EnumProps, solutionDir, Relations, hasLocalization);

                if (hasLocalization)
                    Application.GenerateGetWithLocalizationQuery(entityName, entityPlural, queryPath, properties.PropertiesList, properties.EnumProps, Relations);

                Api.GenerateNeededDtos(entityName, entityPlural, properties.PropertiesList, properties.EnumProps, solutionDir, hasLocalization, Relations);

                Api.AddRoutesToApiRoutes(entityName, entityPlural, solutionDir, hasLocalization);

                Api.GenerateController(entityName, entityPlural, properties.PropertiesList, properties.EnumProps, solutionDir, hasLocalization, hasPermissions);




               // VueJsHelper.GenerateStoreFile(entityName, properties);
            }
            catch( Exception ex )
            {
                throw;
            }

            #endregion
            MessageBox.Show($"Entity : {entityName} has been added.");
            ClearForm();
        }

        private void checkBoxLocalization_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLocalization.Checked)
                lblLocalization.ForeColor = Color.Green;
            else
                lblLocalization.ForeColor = Color.Black;
        }

        private void checkBoxPermissions_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxPermissions.Checked)
                lblPermissions.ForeColor = Color.Green;
            else
                lblPermissions.ForeColor = Color.Black;
        }

        private void checkBoxUserActions_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxUserActions.Checked)
                lblUserActions.ForeColor = Color.Green;
            else
                lblUserActions.ForeColor = Color.Black;
        }

        private void checkBoxVersioning_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxVersioning.Checked)
                lblVersioning.ForeColor = Color.Green;
            else
                lblVersioning.ForeColor = Color.Black;
        }

        private void checkBoxNotifications_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxNotifications.Checked)
                lblNotifications.ForeColor = Color.Green;
            else
                lblNotifications.ForeColor = Color.Black;
        }

        private void btnNewRelation_Click(object sender, EventArgs e)
        {
            RelationForm relationForm = new RelationForm();
            relationForm.ShowDialog();
            var relation = relationForm.Relation;
            if (relationForm.IsSaved)
            {
                Relations.Add(relation);
            }
            UpdateRelationDisplay();
        }

        private void UpdateRelationDisplay()
        {
            pnlRelations.Controls.Clear();
            int yPosition = 18; // Starting position inside panel
            foreach (var relation in Relations)
            {
                // Create container panel for each property
                Panel relPanel = new Panel
                {
                    Location = new Point(10, yPosition),
                    Size = new Size(pnlScrollable.ClientSize.Width - 30, 40),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White,
                };
                Label lblRel = new Label
                {
                    AutoSize = true,
                    Location = new Point(5, 5),
                    Text = $"{relation.Type} with {relation.RelatedEntity}"
                };

                // Edit button
                Button btnRelEdit = new Button
                {
                    Text = "Edit",
                    Tag = relation.RelatedEntity,
                    Location = new Point(relPanel.Width - 140, 5),
                    Size = new Size(60, 25)
                };
                btnRelEdit.Click += BtnRelEdit_Click;

                // Delete button
                Button btnRelDelete = new Button
                {
                    Text = "Delete",
                    Tag = relation.RelatedEntity,
                    Location = new Point(relPanel.Width - 75, 5),
                    Size = new Size(60, 25)
                };
                btnRelDelete.Click += BtnRelDelete_Click;

                // Add controls to panel
                relPanel.Controls.AddRange(new Control[] { lblRel, btnRelEdit, btnRelDelete });
                pnlRelations.Controls.Add(relPanel);

                yPosition += relPanel.Height + 10;
            }
        }
        private void BtnRelEdit_Click(object sender, EventArgs e)
        {
            string relationEntityRelated = ((Button)sender).Tag.ToString();
            var oldRelationInfo = GetRelationInfo(relationEntityRelated);
            RelationForm editForm = new RelationForm();
            editForm.Relation.RelatedEntity = oldRelationInfo.RelatedEntity;
            editForm.Relation.Type = oldRelationInfo.Type;
            editForm.ShowDialog();

            if (editForm.IsSaved)
            {
                UpdateRelations(editForm.Relation, oldRelationInfo);
                UpdateRelationDisplay();
            }
        }

        private void BtnRelDelete_Click(object sender, EventArgs e)
        {
            string relationEntityRelated = ((Button)sender).Tag.ToString();
            if (MessageBox.Show($"Are you sure you want to delete relation with '{relationEntityRelated}'?",
                                "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                RemoveRelation(relationEntityRelated);
                UpdateRelationDisplay();
            }
        }
        private Relation GetRelationInfo(string relationEntityRelated)
        {
            return new Relation
            {
                RelatedEntity = Relations.FirstOrDefault(r => r.RelatedEntity == relationEntityRelated).RelatedEntity,
                Type = Relations.FirstOrDefault(r => r.RelatedEntity == relationEntityRelated).Type
            };
        }

        private void UpdateRelations(Relation updatedInfo,Relation oldRelation)
        {
            // Update relations list here based on your needs
            RemoveRelation(oldRelation.RelatedEntity);
            Relations.Add(updatedInfo);
            //var index = Relations.FindIndex(r => r.RelatedEntity == updatedInfo.RelatedEntity);
            //if (index != -1)
            //{
            //    Relations[index] = new Relation
            //    {
            //        RelatedEntity = updatedInfo.RelatedEntity,
            //        Type = updatedInfo.Type
            //    };
            //}
        }
        private void RemoveRelation(string relationEntityRelated)
        {
            Relations.RemoveAll(r => r.RelatedEntity == relationEntityRelated);
        }



        private void ClearForm()
        {
            checkBoxLocalization.Checked = false;
            checkBoxNotifications.Checked = false;
            checkBoxPermissions.Checked = false;
            checkBoxUserActions.Checked = false;
            checkBoxVersioning.Checked = false;
            txtEntityName.Clear();
            pnlScrollable.Controls.Clear();
            pnlRelations.Controls.Clear();
            properties = null!;
            properties = new SharedClasses.Properties();
            Relations.Clear();
        }
        private bool ValidateSolution()
        {
            var entityName = txtEntityName.Text;

            if (string.IsNullOrWhiteSpace(entityName))
            {
                MessageBox.Show("Entity Name missing");
                return false;
            }
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";

            if (!Directory.Exists($"{txtDir.Text}"))
            {
                MessageBox.Show("Solution Dir is invalid");
                return false;
            }
            //Api Validation
            string filePath = Path.Combine($"{txtDir.Text}", "Api", "Utilities", "ApiRoutes.cs");

            if (!File.Exists(filePath))
            {
                MessageBox.Show("ApiRoutes.cs is not found");
                return false;
            }
            string content = File.ReadAllText(filePath);
            var matches = ClassPattern.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups[1].Value == entityName)
                {
                    MessageBox.Show($"?? ApiRoutes already contains routes for {entityName}, the generator will continue.");
                }
            }

            filePath = Path.Combine($"{txtDir.Text}", "Api", "Utilities", "Extensions.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Extensions.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if(!content.Contains("//Add Using Here") || !content.Contains("//Add Extension Here"))
            {
                MessageBox.Show("Extensions.cs dose not contain necessary strings for generator");
                return false;
            }
            //Application Validation
            filePath = Path.Combine($"{txtDir.Text}", "Application", "Common", "Interfaces", "Db", "IApplicationDbContext.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("IApplicationDbContext.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Generate Here"))
            {
                MessageBox.Show("IApplicationDbContext.cs dose not contain necessary strings for generator");
                return false;
            }
            if (checkBoxLocalization.Checked)
            {
                filePath = Path.Combine($"{txtDir.Text}", "Application", "Common", "Interfaces", "Services", "ILocalizationService.cs");
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("ILocalizationService.cs is not found");
                    return false;
                }
                content = File.ReadAllText(filePath);
                if (!content.Contains("//Add Using Here") || !content.Contains("//Define Localization Method Here"))
                {
                    MessageBox.Show("ILocalizationService.cs dose not contain necessary strings for generator");
                    return false;
                }
            }
            
            filePath = Path.Combine($"{txtDir.Text}", "Application", "Common", "Models", "Localization");
            if (!Directory.Exists(filePath))
            {
                MessageBox.Show("Localization Model Directory not found");
                return false;
            }
            if (checkBoxVersioning.Checked)
            {
                filePath = Path.Combine($"{txtDir.Text}", "Application", "Common", "Models", "Versioning");
                if (!Directory.Exists(filePath))
                {
                    MessageBox.Show("Versioning Model Directory not found");
                    return false;
                }
            }
            filePath = Path.Combine($"{txtDir.Text}", "Application", "Utilities", "NotificationConsistent.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("NotificationConsistent.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add To Group Here"))
            {
                MessageBox.Show("NotificationConsistent.cs dose not contain necessary strings for generator");
                return false;
            }
            if (checkBoxNotifications.Checked)
            {
                content = File.ReadAllText(filePath);
                matches = ClassPattern.Matches(content);
                foreach (Match match in matches)
                {
                    if (match.Groups[1].Value == entityPlural)
                    {
                        MessageBox.Show($"?? NotificationConsistent already contains notifications for {entityName}, the generator will continue.");
                    }
                }
            }

            //Domain Validation
            filePath = Path.Combine($"{txtDir.Text}", "Domain", "Entities", "Language.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Language.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Generate Here"))
            {
                MessageBox.Show("Language.cs dose not contain necessary strings for generator");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Domain", "Enums", "NotificationObjectTypes.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("NotificationObjectTypes.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add Here"))
            {
                MessageBox.Show("NotificationObjectTypes.cs dose not contain necessary strings for generator");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Domain", "Enums", "UserActionEntityType.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("UserActionEntityType.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add Here"))
            {
                MessageBox.Show("UserActionEntityType.cs dose not contain necessary strings for generator");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Domain", "Enums", "VersionEntityType.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("VersionEntityType.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add Here"))
            {
                MessageBox.Show("VersionEntityType.cs dose not contain necessary strings for generator");
                return false;
            }

            //Infrastructure Validation
            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Data", "Configurations");
            if (!Directory.Exists(filePath))
            {
                MessageBox.Show("Configurations Directory not found");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Data", "AppDbContext.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("AppDbContext.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Generate Here"))
            {
                MessageBox.Show("AppDbContext.cs dose not contain necessary strings for generator");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Data", "ApplicationDbContextInitialiser.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("ApplicationDbContextInitialiser.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add Permission Here") || !content.Contains("//Add To rolesAfterInitialize Here") || !content.Contains("//Except rolesAfterInitialize Here")
                || !content.Contains("//Add RoleConsistent Loop") || !content.Contains("//Add Notifications Here"))
            {
                MessageBox.Show("ApplicationDbContextInitialiser.cs dose not contain necessary strings for generator");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Utilities", "RoleConsistent.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("RoleConsistent.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add To Group Here"))
            {
                MessageBox.Show("RoleConsistent.cs dose not contain necessary strings for generator");
                return false;
            }
            if (checkBoxPermissions.Checked)
            {
                content = File.ReadAllText(filePath);
                matches = ClassPattern.Matches(content);
                foreach (Match match in matches)
                {
                    if (match.Groups[1].Value == entityName)
                    {
                        MessageBox.Show($"?? RoleConsistent already contains roles for {entityName}, the generator will continue.");
                    }
                }
            }
            if (checkBoxLocalization.Checked)
            {
                filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Services", "LocalizationService.cs");
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("LocalizationService.cs is not found");
                    return false;
                }
                content = File.ReadAllText(filePath);
                if (!content.Contains("//Add Using Here") || !content.Contains("//Add Private Field Here")
                    || !content.Contains("//Inject Service Here") || !content.Contains("//Add Field Here") || !content.Contains("//Implement Method Here"))
                {
                    MessageBox.Show("LocalizationService.cs dose not contain necessary strings for generator");
                    return false;
                }
            }

            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "DependencyInjection.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("DependencyInjection.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Register Here"))
            {
                MessageBox.Show("DependencyInjection.cs dose not contain necessary strings for generator");
                return false;
            }

            //Relations Validation
            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Data", "AppDbContext.cs");
            content = File.ReadAllText(filePath);
            foreach(var item in this.Relations)
            {
                if (!content.Contains($"DbSet<{item.RelatedEntity}>"))
                {
                    MessageBox.Show($"Relation with {item.RelatedEntity} is failed : DbSet of {item.RelatedEntity} dose not found in AppDbContext");
                    return false;
                }
            }
            if (!content.Contains("//Generate Here"))
            {
                MessageBox.Show("AppDbContext.cs dose not contain necessary strings for generator");
                return false;
            }

            return true;
        }

        private void UpdatePropertiesDisplay()
        {
            pnlScrollable.Controls.Clear();
            int yPosition = 18; // Starting position inside panel
            foreach (var prop in properties.PropertiesList)
            {
                // Create container panel for each property
                Panel propPanel = new Panel
                {
                    Location = new Point(10, yPosition),
                    Size = new Size(pnlScrollable.ClientSize.Width - 30, 40),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White,
                };
                string type = null!;
                // Property label
                switch (prop.Type)
                {
                    case "PNGs":
                        type = (prop.Validation != null && prop.Validation.Required) ? "List<string>" : "List<string>?";
                        break;
                    case "GPG":
                        type = (prop.Validation != null && prop.Validation.Required) ? "string" : "string?";
                        break;
                    case "VD":
                        type = (prop.Validation != null && prop.Validation.Required) ? "string" : "string?";
                        break;
                    default:
                        type = prop.Type;
                        break;
                }
                Label lblProperty = new Label
                {
                    AutoSize = true,
                    Location = new Point(5, 5),
                    Text = $"{type} {prop.Name}"
                };

                // Edit button
                Button btnEdit = new Button
                {
                    Text = "Edit",
                    Tag = prop.Name,
                    Location = new Point(propPanel.Width - 140, 5),
                    Size = new Size(60, 25)
                };
                btnEdit.Click += BtnEdit_Click;

                // Delete button
                Button btnDelete = new Button
                {
                    Text = "Delete",
                    Tag = prop.Name,
                    Location = new Point(propPanel.Width - 75, 5),
                    Size = new Size(60, 25)
                };
                btnDelete.Click += BtnDelete_Click;

                // Add controls to panel
                propPanel.Controls.AddRange(new Control[] { lblProperty, btnEdit, btnDelete });
                pnlScrollable.Controls.Add(propPanel);

                yPosition += propPanel.Height + 10;
            }

            // Adjust panel height
            //pnlScrollable.Height = Math.Max(yPosition + 20, 100); // Minimum height of 100 pixels
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            string propertyName = ((Button)sender).Tag.ToString();
            var oldPropertyInfo = GetPropertyInfo(propertyName);

            PropertyForm editForm = new PropertyForm(this.checkBoxLocalization.Checked); 
            editForm.PropertyInfo.Localized = oldPropertyInfo.Localized;
            editForm.PropertyInfo.EnumValues = oldPropertyInfo.EnumValues;
            editForm.PropertyInfo.GeneralInfo = oldPropertyInfo.GeneralInfo;
            editForm.ShowDialog();

            if (editForm.PropertyInfo.IsSaved)
            {
                UpdatePropertiesList(editForm.PropertyInfo,oldPropertyInfo);
                UpdatePropertiesDisplay();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            string propertyName = ((Button)sender).Tag.ToString();
            if (MessageBox.Show($"Are you sure you want to delete property '{propertyName}'?",
                                "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                RemoveProperty(propertyName);
                UpdatePropertiesDisplay();
            }
        }

        private PropertyInfo GetPropertyInfo(string propertyName)
        {
            var res = new PropertyInfo
            {
                GeneralInfo = properties.PropertiesList.FirstOrDefault(p => p.Name == propertyName),
                Localized = properties.LocalizedProp.Contains(propertyName),
                EnumValues = properties.EnumProps.FirstOrDefault(e => e.prop == propertyName)
            };

            return res;
            
        }

        private void UpdatePropertiesList(PropertyInfo updatedInfo, PropertyInfo oldInfo)
        {
            // Update properties lists here based on your needs
            RemoveProperty(oldInfo.GeneralInfo.Name);
            var index = properties.PropertiesList.Count - 1;

            (string, string, PropertyValidation) x = (updatedInfo.GeneralInfo.Type, updatedInfo.GeneralInfo.Name, updatedInfo.GeneralInfo.Validation);
            properties.PropertiesList.Add(x);
            //if (index > -1)
            //{
            //    properties.PropertiesList[index] = (
            //        updatedInfo.GeneralInfo.Type,
            //        updatedInfo.GeneralInfo.Name,
            //        updatedInfo.GeneralInfo.Validation
            //    );
            //}

            if (updatedInfo.Localized && !properties.LocalizedProp.Contains(updatedInfo.GeneralInfo.Name))
            {
                properties.LocalizedProp.Add(updatedInfo.GeneralInfo.Name);
            }
            if (updatedInfo.EnumValues.enumValues != null && updatedInfo.EnumValues.enumValues.Any())
            {
                properties.EnumProps.Add(updatedInfo.EnumValues);
            }
        }

        private void RemoveProperty(string propertyName)
        {
            properties.PropertiesList.RemoveAll(p => p.Name == propertyName);
            properties.LocalizedProp.Remove(propertyName);
            properties.EnumProps.RemoveAll(e => e.prop.Contains(propertyName));
        }
    }
}
