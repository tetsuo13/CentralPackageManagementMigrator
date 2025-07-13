using CentralPackageManagementMigrator;

return await new MigratorCommand()
    .Parse(args)
    .InvokeAsync();
