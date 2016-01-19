Based on [CodePlexMigrationTools](https://github.com/objorke/CodePlexMigrationTools) by [Oystein Bjorke (@objorke)](https://github.com/objorke)

### IssueMigrator

Migrates issues from a CodePlex project to GitHub.

```
IssueMigrator.exe --from <from> --to <to> --owner <owner> --token <token> [--issue <nn>] [--rate-pause <nn>] [--rate-limit <nn>] [--add-codeplex]

  --from            Required. CodePlex project to migrate.
  --to              Required. GitHub repository.
  --owner           Required. GitHub repository owner/organization.
  --key             Required. GitHub Access Token.
  --issue           Only migrate selected issue.
  --rate-limit      (Default: 0) Number of calls to GitHubApi to make pause.
  --rate-pause      (Default: 60) Pause in seconds.
  --add-codeplex    (Default: False) Indicates if we should add "CodePlex" label.
  --help            Display help screen.

IssueMigrator.exe --from openriaservices --to TestIssues --owner OpenRIAServices --token <secret> --rate-pause 120 --rate-limit 15
```

Note:
- The GitHub repository should be created before executing the migration command.
