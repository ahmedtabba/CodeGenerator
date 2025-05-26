using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SharedClasses
{
    public static class MetadataManager
    {
        private const string MetaFileName = "meta.json";
        private const string BackupExtension = ".bak";

        public static void SaveEntityMetadata(string projectPath, string entityName, string entityPlural,
            bool hasLocalization, bool hasPermissions, bool hasVersioning, bool hasNotification,
            bool hasUserAction, bool bulk,
            (List<(string Type, string Name, PropertyValidation Validation)>, List<string>, List<(string prop, List<string> enumValues)>) properties,
            List<Relation> relations)
        {
            var metaFilePath = Path.Combine(projectPath, MetaFileName);
            
            try
            {
                // Create backup of existing file if it exists
                if (File.Exists(metaFilePath))
                {
                    File.Copy(metaFilePath, metaFilePath + BackupExtension, true);
                }

                var projectMetadata = LoadOrCreateMetadata(metaFilePath, projectPath);

                var entityMetadata = new EntityMetadata
                {
                    Name = entityName,
                    PluralName = entityPlural,
                    HasLocalization = hasLocalization,
                    HasPermissions = hasPermissions,
                    HasVersioning = hasVersioning,
                    HasNotification = hasNotification,
                    HasUserAction = hasUserAction,
                    HasBulk = bulk,
                    Properties = properties.Item1.Select(p => new PropertyMetadata 
                    { 
                        Type = p.Type, 
                        Name = p.Name, 
                        Validation = p.Validation 
                    }).ToList(),
                    LocalizedProperties = properties.Item2,
                    EnumProperties = properties.Item3,
                    Relations = relations,
                    GeneratedAt = DateTime.UtcNow
                };

                // Remove existing entity metadata if it exists
                var existingEntity = projectMetadata.Entities.Find(e => e.Name == entityName);
                if (existingEntity != null)
                {
                    Console.WriteLine($"Updating existing entity metadata for {entityName}");
                    projectMetadata.Entities.RemoveAll(e => e.Name == entityName);
                }
                else
                {
                    Console.WriteLine($"Adding new entity metadata for {entityName}");
                }

                projectMetadata.Entities.Add(entityMetadata);
                projectMetadata.LastUpdated = DateTime.UtcNow;

                SaveMetadata(metaFilePath, projectMetadata);

                // If save was successful, remove backup
                if (File.Exists(metaFilePath + BackupExtension))
                {
                    File.Delete(metaFilePath + BackupExtension);
                }

                Console.WriteLine($"Successfully saved metadata for {entityName}. Total entities in metadata: {projectMetadata.Entities.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving metadata: {ex.Message}");
                if (File.Exists(metaFilePath + BackupExtension))
                {
                    try
                    {
                        File.Copy(metaFilePath + BackupExtension, metaFilePath, true);
                        Console.WriteLine("Successfully restored metadata from backup");
                    }
                    catch (Exception backupEx)
                    {
                        Console.WriteLine($"Error restoring backup: {backupEx.Message}");
                        throw;
                    }
                }
                throw;
            }
        }

        private static ProjectMetadata LoadOrCreateMetadata(string metaFilePath, string projectPath)
        {
            if (File.Exists(metaFilePath))
            {
                try
                {
                    var json = File.ReadAllText(metaFilePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    };
                    var metadata = JsonSerializer.Deserialize<ProjectMetadata>(json, options);
                    if (metadata != null)
                    {
                        Console.WriteLine($"Loaded existing metadata with {metadata.Entities.Count} entities");
                        return metadata;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading metadata file: {ex.Message}");
                    if (File.Exists(metaFilePath + BackupExtension))
                    {
                        try
                        {
                            var backupJson = File.ReadAllText(metaFilePath + BackupExtension);
                            var backupMetadata = JsonSerializer.Deserialize<ProjectMetadata>(backupJson);
                            if (backupMetadata != null)
                            {
                                Console.WriteLine("Successfully loaded metadata from backup file");
                                return backupMetadata;
                            }
                        }
                        catch (Exception backupEx)
                        {
                            Console.WriteLine($"Error loading backup metadata: {backupEx.Message}");
                        }
                    }
                }
            }

            var newMetadata = CreateNewMetadata(projectPath);
            Console.WriteLine("Created new metadata file");
            return newMetadata;
        }

        private static ProjectMetadata CreateNewMetadata(string projectPath)
        {
            return new ProjectMetadata
            {
                ProjectName = Path.GetFileName(projectPath),
                ProjectPath = projectPath,
                LastUpdated = DateTime.UtcNow,
                Entities = new List<EntityMetadata>()
            };
        }

        private static void SaveMetadata(string metaFilePath, ProjectMetadata metadata)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
            var json = JsonSerializer.Serialize(metadata, options);
            File.WriteAllText(metaFilePath, json);
        }

        public static ProjectMetadata LoadMetadata(string projectPath)
        {
            var metaFilePath = Path.Combine(projectPath, MetaFileName);
            return LoadOrCreateMetadata(metaFilePath, projectPath);
        }
    }
} 