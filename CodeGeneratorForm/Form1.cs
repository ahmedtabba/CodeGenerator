using DomainGenerator;
using ApiGenerator;
using InfrastructureGenerator;
using SharedClasses;
using Application = ApplicationGenerator.Application;
using ApplicationGenerator;
using System.Text.RegularExpressions;
using Frontend.VueJsHelper;
using System.Reflection;
using PropertyInfo = SharedClasses.PropertyInfo;

namespace CodeGeneratorForm
{
    public partial class Form1 : Form
    {
        private static readonly Regex ClassPattern = new Regex(
    @"public\s+class\s+(\w+)\s*\{",
    RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public List<Relation> Relations { get; set; } = new List<Relation>();
        public SharedClasses.Properties properties { get; set; } = new SharedClasses.Properties();
        public List<string> NotGeneratedTableColumns { get; set; } = new List<string>();
        public List<string> HiddenTableColumns { get; set; } = new List<string>();
        public List<string> NotGeneratedTableRelations { get; set; } = new List<string>();
        public List<string> HiddenTableRelations { get; set; } = new List<string>();
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
                if (propertyInfo.GeneratedColumn == false)
                    NotGeneratedTableColumns.Add(propertyInfo.GeneralInfo.Name);
                else
                {
                    if (propertyInfo.HiddenColumn)
                        HiddenTableColumns.Add(propertyInfo.GeneralInfo.Name);
                }
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
            var bulk = checkBoxBulk.Checked;
            var entityName = txtEntityName.Text;
            var solutionDir = $"{txtDir.Text}";

            if (checkBoxBulk.Checked)
            {
                if (Relations.Count == 0)
                {
                    MessageBox.Show("You choose Bulk, so you should add relation");
                    return;
                }
                if (!Relations.Any(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable))
                {
                    MessageBox.Show("You choose Bulk, so you should add relation (ManyToOne)");
                    return;
                }
            }

            if (!ValidateSolution()) 
            {
                ClearForm();
                return;
            }

            string entityPlural = entityName.GetPluralName();
            bool hasAssets = properties.PropertiesList.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD" || p.Type == "VDs" || p.Type == "FL" || p.Type == "FLs");
            if (!hasAssets)
                VueJsHelper.GenerateStoreFile(entityName, properties,NotGeneratedTableColumns,HiddenTableColumns,Relations, VueJsHelper.VueJsSolutionPath);
            else
                VueJsHelper.GenerateStoreFileWithAssets(entityName, properties, NotGeneratedTableColumns, HiddenTableColumns, Relations, VueJsHelper.VueJsSolutionPath);
            
            VueJsHelper.UpdateConstantsJs(entityName, VueJsHelper.VueJsSolutionPath);
            VueJsHelper.UpdateRouterIndexJs(entityName, VueJsHelper.VueJsSolutionPath);
            VueJsHelper.UpdateAppMenu(entityName, VueJsHelper.VueJsSolutionPath);
            VueJsHelper.GenerateTableView(entityName, VueJsHelper.VueJsSolutionPath, properties.PropertiesList, properties.EnumProps, NotGeneratedTableColumns, HiddenTableColumns,Relations);
            VueJsHelper.GenerateSingleView(entityName, VueJsHelper.VueJsSolutionPath, properties.PropertiesList, properties.EnumProps, Relations,hasAssets);
            // Save metadata before generating code
            try
            {
                MetadataManager.SaveEntityMetadata(solutionDir, entityName, entityPlural,
                    hasLocalization, hasPermissions, hasVersioning, hasNotification,
                    hasUserAction, bulk, 
                    (properties.PropertiesList, 
                    properties.LocalizedProp,
                    properties.EnumProps),
                    Relations);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Warning: Failed to save metadata: {ex.Message}");
                // Continue with code generation even if metadata saving fails
            }

            var domainPath = Path.Combine(solutionDir, "Domain", "Entities");
            var repoInterfacePath = Path.Combine(solutionDir, "Application", "Common", "Interfaces", "IRepositories");
            var repoPath = Path.Combine(solutionDir, "Infrastructure", "Repositories");
            var createCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"Create{entityName}");
            var updateCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"Update{entityName}");
            var deleteCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"Delete{entityName}");
            var createBulkCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"CreateBulk{entityName}");
            var updateBulkCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"UpdateBulk{entityName}");
            var deleteBulkCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"DeleteBulk{entityName}");
            var queryPath = Path.Combine(solutionDir, "Application", entityPlural, "Queries");

            Directory.CreateDirectory(domainPath);
            Directory.CreateDirectory(repoInterfacePath);
            Directory.CreateDirectory(createCommandPath);
            Directory.CreateDirectory(updateCommandPath);
            Directory.CreateDirectory(deleteCommandPath);
            if (bulk)
            {
                Directory.CreateDirectory(createBulkCommandPath);
                Directory.CreateDirectory(updateBulkCommandPath);
                Directory.CreateDirectory(deleteBulkCommandPath);
            }

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
                Application.GenerateIRepositoryInterface(entityName, repoInterfacePath, Relations);
                if (hasLocalization)
                    Application.GenerateIRepositoryInterface($@"{entityName}Localization", repoInterfacePath);
                Infrastructure.GenerateRepository(entityName, repoPath, Relations);
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
                    ApplicationAssistant.GenerateEvents(entityName, domainPath, hasVersioning,bulk);
                    ApplicationAssistant.GenerateHandlers(entityName, domainPath, properties.PropertiesList, Relations, hasVersioning, hasUserAction, hasNotification, bulk);
                }
                Application.GenerateCreateCommand(entityName, entityPlural, createCommandPath, properties.PropertiesList, properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                Application.GenerateCreateCommandValidator(entityName, entityPlural, createCommandPath, properties.PropertiesList, Relations);
                
                Application.GenerateUpdateCommand(entityName, entityPlural, updateCommandPath, properties.PropertiesList, properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                Application.GenerateUpdateCommandValidator(entityName, entityPlural, updateCommandPath, properties.PropertiesList, Relations);


                Application.GenerateDeleteCommand(entityName, entityPlural, deleteCommandPath, properties.PropertiesList, Relations,hasVersioning, hasNotification, hasUserAction);
                Application.GenerateDeleteCommandValidator(entityName, entityPlural, deleteCommandPath, properties.PropertiesList,Relations);

                if (bulk)
                {
                    Application.GenerateSingleEntity(entityName, entityPlural, createBulkCommandPath, properties.PropertiesList, properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                    Application.GenerateCreateBulkCommand(entityName, entityPlural, createBulkCommandPath, properties.PropertiesList, properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                    Application.GenerateCreateBulkCommandValidator(entityName, entityPlural, createBulkCommandPath, properties.PropertiesList, Relations);

                    Application.GenerateSingleUpdateEntity(entityName, entityPlural, updateBulkCommandPath, properties.PropertiesList, properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                    Application.GenerateUpdateBulkCommand(entityName, entityPlural, updateBulkCommandPath, properties.PropertiesList, properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                    Application.GenerateUpdateBulkCommandValidator(entityName, entityPlural, updateBulkCommandPath, properties.PropertiesList, Relations);

                    Application.GenerateDeleteBulkCommand(entityName, entityPlural, deleteBulkCommandPath, properties.PropertiesList, properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                    Application.GenerateDeleteBulkCommandValidator(entityName, entityPlural, deleteBulkCommandPath, properties.PropertiesList);

                }

                Application.GenerateGetByIdQuery(entityName, entityPlural, queryPath, hasLocalization, properties.PropertiesList, properties.EnumProps, Relations);

                Application.GenerateGetWithPaginationQuery(entityName, entityPlural, queryPath, hasLocalization,properties.PropertiesList,properties.EnumProps, Relations);
                Application.GenerateBaseDto(entityName, entityPlural, properties.PropertiesList, properties.EnumProps, solutionDir, Relations, hasLocalization);

                if (hasLocalization)
                    Application.GenerateGetWithLocalizationQuery(entityName, entityPlural, queryPath, properties.PropertiesList, properties.EnumProps, Relations);

                Api.GenerateNeededDtos(entityName, entityPlural, properties.PropertiesList, properties.EnumProps, solutionDir, hasLocalization, Relations, bulk);

                Api.AddRoutesToApiRoutes(entityName, entityPlural, solutionDir, hasLocalization,bulk);

                Api.GenerateController(entityName, entityPlural, properties.PropertiesList, properties.EnumProps, solutionDir, hasLocalization, hasPermissions, bulk);
                // VueJsHelper.GenerateStoreFile(entityName, properties);
            }
            catch (Exception ex)
            {
                throw;
            }

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
            var solutionDir = txtDir.Text;
            if (string.IsNullOrWhiteSpace(solutionDir))
            {
                MessageBox.Show("Please enter the solution directory first");
                return;
            }

            RelationForm relationForm = new RelationForm(solutionDir,properties, txtEntityName.Text);
            relationForm.ShowDialog();
            var relation = relationForm.Relation;
            if (relationForm.IsSaved)
            {
                Relations.Add(relation);
                //if (relation.IsGeneratedInTable == false)
                //    NotGeneratedTableRelations.Add(relation.RelatedEntity);
                //else
                //{
                //    if (relation.HiddenInTable)
                //        HiddenTableRelations.Add(relation.RelatedEntity);
                //}
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
            var solutionDir = txtDir.Text;
            if (string.IsNullOrWhiteSpace(solutionDir))
            {
                MessageBox.Show("Please enter the solution directory first");
                return;
            }

            string relationEntityRelated = ((Button)sender).Tag.ToString();
            var oldRelationInfo = GetRelationInfo(relationEntityRelated);
            RelationForm editForm = new RelationForm(solutionDir, properties, txtEntityName.Text);
            editForm.Relation.RelatedEntity = oldRelationInfo.RelatedEntity;
            editForm.Relation.Type = oldRelationInfo.Type;
            editForm.Relation.DisplayedProperty = oldRelationInfo.DisplayedProperty;
            editForm.Relation.IsGeneratedInTable = oldRelationInfo.IsGeneratedInTable;
            editForm.Relation.HiddenInTable = oldRelationInfo.HiddenInTable;

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
                Type = Relations.FirstOrDefault(r => r.RelatedEntity == relationEntityRelated).Type,
                DisplayedProperty = Relations.FirstOrDefault(r => r.RelatedEntity == relationEntityRelated).DisplayedProperty,
                IsGeneratedInTable = Relations.FirstOrDefault(r => r.RelatedEntity == relationEntityRelated).IsGeneratedInTable,
                HiddenInTable = Relations.FirstOrDefault(r => r.RelatedEntity == relationEntityRelated).HiddenInTable
            };
        }

        private void UpdateRelations(Relation updatedInfo, Relation oldRelation)
        {
            // Update relations list here based on your needs
            RemoveRelation(oldRelation.RelatedEntity);
            Relations.Add(updatedInfo);
            if (!updatedInfo.IsGeneratedInTable)
            {
                NotGeneratedTableRelations.Add(updatedInfo.RelatedEntity);
            }
            else if(updatedInfo.HiddenInTable)
            {
                HiddenTableRelations.Add(updatedInfo.RelatedEntity);
            }

        }
        private void RemoveRelation(string relationEntityRelated)
        {
            Relations.RemoveAll(r => r.RelatedEntity == relationEntityRelated);
            NotGeneratedTableRelations.RemoveAll(r => r == relationEntityRelated);
            HiddenTableRelations.RemoveAll(r => r == relationEntityRelated);
        }



        private void ClearForm()
        {
            checkBoxLocalization.Checked = false;
            checkBoxNotifications.Checked = false;
            checkBoxPermissions.Checked = false;
            checkBoxUserActions.Checked = false;
            checkBoxVersioning.Checked = false;
            checkBoxBulk.Checked = false; 
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
            if (!content.Contains("//Add Using Here") || !content.Contains("//Add Extension Here"))
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
            foreach (var item in this.Relations)
            {
                if (item.Type != RelationType.OneToOneSelfJoin && !content.Contains($"DbSet<{item.RelatedEntity}>"))
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
                    case "VDs":
                        type = (prop.Validation != null && prop.Validation.Required) ? "List<string>" : "List<string>?";
                        break;
                    case "FL":
                        type = (prop.Validation != null && prop.Validation.Required) ? "string" : "string?";
                        break;
                    case "FLs":
                        type = (prop.Validation != null && prop.Validation.Required) ? "List<string>" : "List<string>?";
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
            editForm.PropertyInfo.GeneratedColumn = oldPropertyInfo.GeneratedColumn;
            editForm.PropertyInfo.HiddenColumn = oldPropertyInfo.HiddenColumn;
            editForm.ShowDialog();

            if (editForm.PropertyInfo.IsSaved)
            {
                UpdatePropertiesList(editForm.PropertyInfo, oldPropertyInfo);
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
                EnumValues = properties.EnumProps.FirstOrDefault(e => e.prop == propertyName),
            };
            if (NotGeneratedTableColumns.Any(p => p == propertyName))
            {
                res.GeneratedColumn = false;
            }
            else if (HiddenTableColumns.Any(p => p == propertyName))
            {
                res.HiddenColumn = true;
            }

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
            if (!updatedInfo.GeneratedColumn)
            {
                NotGeneratedTableColumns.Add(updatedInfo.GeneralInfo.Name);
            }
            else if(updatedInfo.HiddenColumn)
            {
                HiddenTableColumns.Add(updatedInfo.GeneralInfo.Name);
            }

        }

        private void RemoveProperty(string propertyName)
        {
            properties.PropertiesList.RemoveAll(p => p.Name == propertyName);
            properties.LocalizedProp.Remove(propertyName);
            properties.EnumProps.RemoveAll(e => e.prop.Contains(propertyName));
            NotGeneratedTableColumns.RemoveAll(p => p == propertyName);
            HiddenTableColumns.RemoveAll(p => p == propertyName);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxNotifications.Checked)
                lblNotifications.ForeColor = Color.Green;
            else
                lblNotifications.ForeColor = Color.Black;
        }

        private void checkBoxBulk_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxBulk.Checked)
                lblBulk.ForeColor = Color.Green;
            else
                lblBulk.ForeColor = Color.Black;
        }
    }
}
