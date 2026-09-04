using System.Diagnostics;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services;
using WartalesEditor.Services.Operations;
using WartalesEditor.Services.Validation;
using WartalesEditor.ViewModels;

if (args.Length >= 1 &&
    string.Equals(
        args[0],
        "--quickbms-process-tree-grandchild",
        StringComparison.Ordinal))
{
    await Task.Delay(
        TimeSpan.FromSeconds(30));
    return;
}

if (args.Length >= 2 &&
    string.Equals(
        args[0],
        "--quickbms-process-tree-child",
        StringComparison.Ordinal))
{
    string selfExecutable =
        Path.ChangeExtension(
            System.Reflection.Assembly
                .GetExecutingAssembly()
                .Location,
            ".exe");
    ProcessStartInfo grandchildStart =
        new()
        {
            FileName = selfExecutable,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    grandchildStart.ArgumentList.Add(
        "--quickbms-process-tree-grandchild");
    using Process grandchild =
        Process.Start(grandchildStart)
        ?? throw new InvalidOperationException(
            "The process-tree grandchild did not start.");
    File.WriteAllText(
        args[1],
        grandchild.Id.ToString(
            System.Globalization.CultureInfo.InvariantCulture));

    try
    {
        await Task.Delay(
            TimeSpan.FromSeconds(30));
    }
    finally
    {
        if (!grandchild.HasExited)
        {
            grandchild.Kill();
        }
    }

    return;
}

if (args.Length >= 3 &&
    string.Equals(
        args[0],
        "--quickbms-process-tree-parent",
        StringComparison.Ordinal))
{
    string selfExecutable =
        Path.ChangeExtension(
            System.Reflection.Assembly
                .GetExecutingAssembly()
                .Location,
            ".exe");
    ProcessStartInfo childStart =
        new()
        {
            FileName = selfExecutable,
            UseShellExecute = false,
            CreateNoWindow = true
    };
    childStart.ArgumentList.Add(
        "--quickbms-process-tree-child");
    childStart.ArgumentList.Add(args[2]);
    using Process child =
        Process.Start(childStart)
        ?? throw new InvalidOperationException(
            "The process-tree child did not start.");
    File.WriteAllText(
        args[1],
        child.Id.ToString(
            System.Globalization.CultureInfo.InvariantCulture));

    try
    {
        await Task.Delay(
            TimeSpan.FromSeconds(30));
    }
    finally
    {
        if (!child.HasExited)
        {
            child.Kill();
        }
    }

    return;
}

if (args.Length >= 3 &&
    string.Equals(
        args[0],
        "--quickbms-root-exits-first",
        StringComparison.Ordinal))
{
    string selfExecutable =
        Path.ChangeExtension(
            System.Reflection.Assembly
                .GetExecutingAssembly()
                .Location,
            ".exe");
    ProcessStartInfo childStart =
        new()
        {
            FileName = selfExecutable,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    childStart.ArgumentList.Add(
        "--quickbms-process-tree-child");
    childStart.ArgumentList.Add(args[2]);
    using Process child =
        Process.Start(childStart)
        ?? throw new InvalidOperationException(
            "The independent process-tree child did not start.");
    File.WriteAllText(
        args[1],
        child.Id.ToString(
            System.Globalization.CultureInfo.InvariantCulture));

    Stopwatch markerWait = Stopwatch.StartNew();

    while (!File.Exists(args[2])
           && markerWait.Elapsed < TimeSpan.FromSeconds(5))
    {
        await Task.Delay(25);
    }

    if (!File.Exists(args[2]))
    {
        throw new InvalidOperationException(
            "The process-tree grandchild marker was not created.");
    }

    return;
}

if (args.Contains(
        "--quickbms-real-import",
        StringComparer.Ordinal))
{
    await VerifyRealQuickBmsImport();
    return;
}

if (args.Contains(
        "--quickbms-import-only",
        StringComparer.Ordinal))
{
    await VerifyQuickBmsImport();
    Console.WriteLine(
        "ALL QUICKBMS IMPORT CHECKS PASSED");
    return;
}

if (args.Contains(
        "--restore-previous-values-only",
        StringComparer.Ordinal))
{
    VerifyRestorePreviousValuesContract();
    Console.WriteLine(
        "ALL RESTORE PREVIOUS VALUES CHECKS PASSED");
    return;
}

if (args.Contains(
        "--language-data-only",
        StringComparer.Ordinal))
{
    VerifyLanguageDataSetup();
    Console.WriteLine(
        "ALL LANGUAGE DATA CHECKS PASSED");
    return;
}

if (args.Length >= 2 &&
    string.Equals(
        args[0],
        "--language-data-real-file",
        StringComparison.Ordinal))
{
    VerifyRealLanguageDataFile(
        args[1]);
    return;
}

VerifyPropertyRemovalMutationPrimitive();
VerifyPropertyRemovalBoundaryRejections();
VerifyPropertyRemovalAdvancedTransactions();
VerifyScalarVanillaRestoration();
VerifyMiningBaselineScaling();
VerifyVendorBaselineScaling();
VerifyResourceReplenishment();
VerifyLecternKnowledgeGain();
VerifyPositiveRandomTraits();
VerifyRandomTraitExclusions();
VerifyProfileUpdate();
VerifyProfileUpdateIntegrityAndAccounting();
VerifyUpdateProfileFinalBlockerCorrections();
VerifyCampfireExpansion();
VerifyBattleCameraBaselineDrift();
VerifyLegacyValour();
VerifyLegacyCarrying();
VerifySnapshotPathCompatibility();
VerifyLegacyProfileReconciliation();
VerifyApplyFeedbackState();
VerifyCatalogCoverage();
VerifyMalformedTargets();
VerifyRestorePreviousValuesContract();

Console.WriteLine("ALL CLASS A COMPATIBILITY CHECKS PASSED");

static void VerifyLanguageDataSetup()
{
    string root =
        Path.Combine(
            Path.GetTempPath(),
            "WartalesEditorLanguageDataTests",
            Guid.NewGuid().ToString("N"));

    Directory.CreateDirectory(root);

    try
    {
        string storage =
            Path.Combine(root, "storage");
        LocalizationService localization = new();
        LanguageDataService service =
            new(localization, storage);

        LanguageDataState missing =
            service.LoadCanonical();
        Check(missing.Availability ==
                  LanguageDataAvailability.Unavailable &&
              localization.EntryCount == 0 &&
              Path.GetFileName(service.GetCanonicalPath()) ==
                  "export.xml",
            "missing canonical language data is nonfatal and uses the generic canonical filename");

        string invalidInitial =
            Path.Combine(root, "invalid-initial.xml");
        File.WriteAllText(
            invalidInitial,
            "<cdb");
        CheckThrows<System.Xml.XmlException>(
            () => service.Install(invalidInitial),
            "malformed initial setup is rejected");
        Check(!File.Exists(service.GetCanonicalPath()) &&
              (!Directory.Exists(storage) ||
               Directory.GetFiles(storage).Length == 0),
            "failed initial setup leaves no canonical or temporary language-data file");

        string englishSource =
            Path.Combine(root, "renamed-language-file.xml");
        WriteLanguageExport(
            englishSource,
            "en",
            "ItemA",
            "Localized Item A",
            version: "143",
            revision: "46573");

        LanguageDataState english =
            service.Install(englishSource);
        string canonical =
            service.GetCanonicalPath();
        Check(english.IsAvailable &&
              english.Metadata?.LanguageCode == "en" &&
              english.Metadata.Version == "143" &&
              english.Metadata.Revision == "46573" &&
              english.MappingCount == 1 &&
              localization.GetLocalizedName("ItemA") ==
                  "Localized Item A" &&
              File.Exists(canonical),
            "valid renamed English export installs with embedded metadata authority");

        File.Delete(englishSource);
        LocalizationService reloadedLocalization =
            new();
        LanguageDataService reloadedService =
            new(reloadedLocalization, storage);
        LanguageDataState reloaded =
            reloadedService.LoadCanonical();
        Check(reloaded.IsAvailable &&
              reloaded.Metadata?.LanguageCode == "en" &&
              reloaded.MappingCount == 1 &&
              reloadedLocalization.GetLocalizedName("ItemA") ==
                  "Localized Item A" &&
              !File.Exists(englishSource),
            "a fresh service auto-loads canonical language data after the original source is deleted");

        localization = reloadedLocalization;
        service = reloadedService;

        string russianNamedEnglish =
            Path.Combine(root, "export_ru.xml");
        WriteLanguageExport(
            russianNamedEnglish,
            "en",
            "Mismatch",
            "Embedded English");
        LanguageDataState mismatch =
            service.Install(russianNamedEnglish);
        Check(mismatch.Metadata?.LanguageCode == "en" &&
              localization.GetLocalizedName("Mismatch") ==
                  "Embedded English",
            "embedded language overrides a mismatched source filename");

        string nonEnglishSource =
            Path.Combine(root, "anything.xml");
        WriteLanguageExport(
            nonEnglishSource,
            "ru",
            "SharedKey",
            "First value",
            duplicateText: "Последнее значение");
        LanguageDataState russian =
            service.Install(nonEnglishSource);
        Check(russian.Metadata?.LanguageCode == "ru" &&
              russian.MappingCount == 1 &&
              localization.GetLocalizedName("SharedKey") ==
                  "Последнее значение",
            "non-English embedded language and duplicate-key last-write semantics are preserved");

        byte[] previousCanonical =
            File.ReadAllBytes(canonical);
        LanguageDataState previousState =
            service.CurrentState;
        string previousName =
            localization.GetLocalizedName("SharedKey")!;

        string invalidReplacement =
            Path.Combine(root, "invalid.xml");
        File.WriteAllText(
            invalidReplacement,
            "<cdb project=\"Other\" lang=\"fr\"><sheet><A><name>A</name></A></sheet></cdb>");
        CheckThrows<InvalidDataException>(
            () => service.Install(invalidReplacement),
            "wrong-project replacement is rejected");
        Check(File.ReadAllBytes(canonical)
                  .SequenceEqual(previousCanonical) &&
              ReferenceEquals(service.CurrentState, previousState) &&
              localization.GetLocalizedName("SharedKey") ==
                  previousName,
            "failed replacement preserves canonical bytes, active dictionary, and metadata state");

        VerifyRejectedLanguageDataFixtures(
            service,
            root);

        string frenchSource =
            Path.Combine(root, "export_en.xml");
        WriteLanguageExport(
            frenchSource,
            "fr",
            "ItemA",
            "Objet localisé");
        LanguageDataState french =
            service.Install(frenchSource);
        Check(french.Metadata?.LanguageCode == "fr" &&
              localization.GetLocalizedName("ItemA") ==
                  "Objet localisé" &&
              localization.GetLocalizedName("SharedKey") == null,
            "successful replacement publishes the stored candidate language and mappings");

        string[] storedFiles =
            Directory.GetFiles(storage);
        Check(storedFiles.Length == 1 &&
              string.Equals(
                  storedFiles[0],
                  canonical,
                  StringComparison.OrdinalIgnoreCase),
            "successful setup retains one canonical file without language, backup, or archive accumulation");

        ProjectModel project =
            CreateProject(
                Sheet(
                    "item",
                    new JObject
                    {
                        ["id"] = "ItemA",
                        ["price"] = 10
                    }));
        MainViewModel viewModel =
            CreateMainViewModel(
                localization,
                ReferenceDataService.Instance,
                languageDataService: service);
        viewModel.Project = project;
        viewModel.SelectedSheet =
            project.Sheets.Single();
        viewModel.SelectedEntry =
            project.Sheets.Single().Entries.Single();
        Check(viewModel.SelectedSettingTitle ==
                  "Objet localisé" &&
              viewModel.SelectedSettingContext ==
                  "item · ItemA" &&
              !viewModel.ShouldShowLanguageDataSetup &&
              viewModel.LocalizationStatus ==
                  "Language Data: fr",
            "available setup supplies generic status and selected-setting presentation");

        viewModel.SearchText =
            "Objet localisé";
        Check(viewModel.SearchResults.Count == 1,
            "search fixture begins with the current localized name");

        int selectedContextNotifications = 0;
        viewModel.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName ==
                nameof(MainViewModel.SelectedSettingContext))
            {
                selectedContextNotifications++;
            }
        };

        PropertyModel changedProperty =
            project.Sheets.Single()
                .Entries.Single()
                .Properties.Single(property =>
                    property.Name == "price");
        changedProperty.Value = 20;

        ChangeSummaryViewModel openSummary =
            new(
                [
                    new ChangeSummaryItemModel(
                        project.Sheets.Single(),
                        project.Sheets.Single().Entries.Single(),
                        changedProperty,
                        "Objet localisé",
                        "10",
                        "20")
                ],
                _ => { });

        typeof(MainViewModel)
            .GetField(
                "changeSummaryViewModel",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!
            .SetValue(
                viewModel,
                openSummary);

        string germanSource =
            Path.Combine(root, "localized.xml");
        WriteLanguageExport(
            germanSource,
            "de",
            "ItemA",
            "German name");
        viewModel.InstallLanguageData(
            germanSource);
        Check(viewModel.SelectedSettingTitle ==
                  "German name" &&
              viewModel.SelectedSettingContext ==
                  "item · ItemA" &&
              selectedContextNotifications > 0 &&
              viewModel.SearchResults.Count == 0 &&
              openSummary.Items.Count == 1 &&
              openSummary.Items[0].SettingName ==
                  "German name" &&
              viewModel.LocalizationStatus ==
                  "Language Data: de",
            "runtime setup removes old localized search results and refreshes selection, context, status, and open Change Summary");

        viewModel.SearchText =
            "German name";
        Check(viewModel.SearchResults.Count == 1,
            "runtime setup adds the new localized search result");

        viewModel.SearchText =
            "ItemA";
        Check(viewModel.SearchResults.Count == 1,
            "internal-ID search remains available after language replacement");

        File.WriteAllText(
            canonical,
            "<truncated");
        LanguageDataState corrupt =
            service.LoadCanonical();
        Check(corrupt.Availability ==
                  LanguageDataAvailability.Invalid &&
              localization.EntryCount == 0 &&
              localization.GetLocalizedName("ItemA") == null,
            "invalid canonical startup state is nonfatal and clears stale localization");

        MainViewModel fallbackViewModel =
            CreateMainViewModel(
                localization,
                ReferenceDataService.Instance,
                languageDataService: service);
        fallbackViewModel.Project = project;
        fallbackViewModel.SelectedSheet =
            project.Sheets.Single();
        fallbackViewModel.SelectedEntry =
            project.Sheets.Single().Entries.Single();
        Check(fallbackViewModel.SelectedSettingTitle == "ItemA" &&
              fallbackViewModel.ShouldShowLanguageDataSetup &&
              fallbackViewModel.LocalizationStatus ==
                  "Language Data: invalid",
            "Detailed Editor remains usable with raw IDs and setup visibility after invalid startup data");

        File.Delete(canonical);
        File.WriteAllText(
            Path.Combine(storage, "texts_ru.xml"),
            "<texts project=\"Wartales\" lang=\"ru\"/>");
        LanguageDataState textsOnly =
            service.LoadCanonical();
        Check(textsOnly.Availability ==
                  LanguageDataAvailability.Unavailable &&
              localization.EntryCount == 0,
            "texts files are ignored and never satisfy language-data setup");

        VerifyLanguageDataRecovery(root);
        VerifyLanguageDataSourceDiscovery(
            root);
        VerifyLanguageDataSuccessStyling();

        Console.WriteLine(
            "PASS generic one-time language-data setup, validation, source discovery, persistence, replacement, refresh, styling, and fallback");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(
                root,
                recursive: true);
        }
    }
}

static void VerifyLanguageDataSourceDiscovery(
    string root)
{
    string installationDirectory =
        Path.Combine(root, "detected-installation");
    Directory.CreateDirectory(
        installationDirectory);
    File.WriteAllText(
        Path.Combine(
            installationDirectory,
            "Wartales.exe"),
        "fixture executable");
    File.WriteAllBytes(
        Path.Combine(
            installationDirectory,
            "res.pak"),
        [(byte)'P', (byte)'A', (byte)'K', 0, 1]);

    string validFrench =
        Path.Combine(
            installationDirectory,
            "export_ru.xml");
    WriteLanguageExport(
        validFrench,
        "fr",
        "DiscoveryKey",
        "Nom français");

    string validGerman =
        Path.Combine(
            installationDirectory,
            "export_de.xml");
    WriteLanguageExport(
        validGerman,
        "de",
        "DiscoveryKey",
        "Deutscher Name");

    File.WriteAllText(
        Path.Combine(
            installationDirectory,
            "export_invalid.xml"),
        "<cdb project=\"Other\" lang=\"en\"><sheet/></cdb>");
    WriteLanguageExport(
        Path.Combine(
            installationDirectory,
            "localized.xml"),
        "en",
        "IgnoredKey",
        "Ignored Name");

    string storage =
        Path.Combine(root, "discovery-storage");
    LocalizationService localization = new();
    LanguageDataService languageDataService =
        new(localization, storage);

    IReadOnlyList<string> candidates =
        languageDataService.DiscoverValidSources(
            installationDirectory);

    Check(candidates.Count == 2 &&
          string.Equals(
              candidates[0],
              validGerman,
              StringComparison.OrdinalIgnoreCase) &&
          candidates.Contains(
              validFrench,
              StringComparer.OrdinalIgnoreCase),
        "source discovery validates generic export_*.xml files, ignores invalid/nonmatching files, and orders multiple choices deterministically");

    TestFileDialogService detectedDialog =
        new(validFrench);
    MainViewModel detectedViewModel =
        CreateMainViewModel(
            localization,
            ReferenceDataService.Instance,
            fileDialogService: detectedDialog,
            languageDataService: languageDataService,
            wartalesInstallationService:
                new WartalesInstallationService(
                    Path.Combine(
                        root,
                        "location",
                        "location.json"),
                    () => Array.Empty<string>()),
            quickBmsImportOptions:
                new QuickBmsImportOptions
                {
                    WartalesInstallationDirectory =
                        installationDirectory
                });

    InvokeLanguageDataSelection(
        detectedViewModel);

    Check(detectedDialog.OpenCallCount == 1 &&
          string.Equals(
              detectedDialog.LastOpenInitialDirectory,
              installationDirectory,
              StringComparison.OrdinalIgnoreCase) &&
          string.Equals(
              detectedDialog.LastOpenInitialFileName,
              validGerman,
              StringComparison.OrdinalIgnoreCase) &&
          languageDataService.CurrentState.Metadata?.LanguageCode ==
              "fr",
        "validated Wartales installation context opens the picker in the game root with a validated candidate preselected and installs the user's selected valid source");

    LanguageDataState installed =
        languageDataService.CurrentState;
    string canonical =
        languageDataService.GetCanonicalPath();
    File.Delete(validFrench);
    LanguageDataState reloaded =
        new LanguageDataService(
                new LocalizationService(),
                storage)
            .LoadCanonical();

    Check(installed.Metadata?.LanguageCode == "fr" &&
          File.Exists(canonical) &&
          !File.Exists(validFrench) &&
          reloaded.Metadata?.LanguageCode == "fr",
        "discovered source keeps embedded language authority and is copied to source-independent canonical storage");

    File.Delete(validGerman);

    TestFileDialogService noCandidateDialog =
        new();
    MainViewModel noCandidateViewModel =
        CreateMainViewModel(
            new LocalizationService(),
            ReferenceDataService.Instance,
            fileDialogService: noCandidateDialog,
            languageDataService:
                new LanguageDataService(
                    new LocalizationService(),
                    Path.Combine(root, "no-candidate-storage")),
            wartalesInstallationService:
                new WartalesInstallationService(
                    Path.Combine(
                        root,
                        "no-candidate-location",
                        "location.json"),
                    () => Array.Empty<string>()),
            quickBmsImportOptions:
                new QuickBmsImportOptions
                {
                    WartalesInstallationDirectory =
                        installationDirectory
                });

    InvokeLanguageDataSelection(
        noCandidateViewModel);

    Check(noCandidateDialog.OpenCallCount == 1 &&
          noCandidateDialog.LastOpenInitialFileName == null &&
          string.Equals(
              noCandidateDialog.LastOpenInitialDirectory,
              installationDirectory,
              StringComparison.OrdinalIgnoreCase),
        "an installation with no valid export candidate still opens manual selection in the detected game root");

    TestFileDialogService detectionFailureDialog =
        new();
    MainViewModel detectionFailureViewModel =
        CreateMainViewModel(
            new LocalizationService(),
            ReferenceDataService.Instance,
            fileDialogService: detectionFailureDialog,
            languageDataService:
                new LanguageDataService(
                    new LocalizationService(),
                    Path.Combine(root, "detection-failure-storage")),
            wartalesInstallationService:
                new WartalesInstallationService(
                    Path.Combine(
                        root,
                        "detection-failure-location",
                        "location.json"),
                    () => Array.Empty<string>()),
            quickBmsImportOptions:
                new QuickBmsImportOptions
                {
                    WartalesInstallationDirectory =
                        Path.Combine(root, "missing-installation")
                });

    InvokeLanguageDataSelection(
        detectionFailureViewModel);

    Check(detectionFailureDialog.OpenCallCount == 1 &&
          detectionFailureDialog.LastOpenInitialFileName == null &&
          string.Equals(
              detectionFailureDialog.LastOpenInitialDirectory,
              root,
              StringComparison.OrdinalIgnoreCase),
        "installation detection failure preserves the generic manual picker fallback near the stale path");
}

static void VerifyLanguageDataSuccessStyling()
{
    DirectoryInfo? directory =
        new(AppContext.BaseDirectory);
    string? dialogPath = null;

    while (directory != null)
    {
        string candidate =
            Path.Combine(
                directory.FullName,
                "Views",
                "LanguageDataDialog.xaml");

        if (File.Exists(candidate))
        {
            dialogPath = candidate;
            break;
        }

        directory = directory.Parent;
    }

    Check(dialogPath != null,
        "Language Data dialog XAML is available for structural style verification");

    string xaml =
        File.ReadAllText(dialogPath!);

    Check(xaml.Contains(
              "BasedOn=\"{StaticResource InformationalPanelStyle}\"",
              StringComparison.Ordinal) &&
          xaml.Contains(
              "<DataTrigger Binding=\"{Binding IsAvailable}\"",
              StringComparison.Ordinal) &&
          xaml.Contains(
              "Value=\"{StaticResource ApplyFeedbackBackgroundBrush}\"",
              StringComparison.Ordinal) &&
          xaml.Contains(
              "Value=\"{StaticResource ApplyFeedbackBorderBrush}\"",
              StringComparison.Ordinal),
        "available Language Data uses existing success brushes while non-success states retain the informational base treatment");
}

static void VerifyRejectedLanguageDataFixtures(
    LanguageDataService service,
    string root)
{
    IReadOnlyList<(string Name, string Contents, Type ExceptionType)> fixtures =
    [
        ("missing-lang.xml",
         "<cdb project=\"Wartales\"><sheet><A><name>A</name></A></sheet></cdb>",
         typeof(InvalidDataException)),
        ("empty-lang.xml",
         "<cdb project=\"Wartales\" lang=\"   \"><sheet><A><name>A</name></A></sheet></cdb>",
         typeof(InvalidDataException)),
        ("wrong-root.xml",
         "<root project=\"Wartales\" lang=\"en\"><sheet><A><name>A</name></A></sheet></root>",
         typeof(InvalidDataException)),
        ("no-sheet.xml",
         "<cdb project=\"Wartales\" lang=\"en\"><A><name>A</name></A></cdb>",
         typeof(InvalidDataException)),
        ("zero-mappings.xml",
         "<cdb project=\"Wartales\" lang=\"en\"><sheet name=\"empty\"><A/></sheet></cdb>",
         typeof(InvalidDataException)),
        ("malformed.xml",
         "<cdb",
         typeof(System.Xml.XmlException))
    ];

    foreach ((string name, string contents, Type exceptionType)
             in fixtures)
    {
        string file =
            Path.Combine(root, name);
        File.WriteAllText(file, contents);

        Exception? exception =
            RecordException(
                () => service.Install(file));
        Check(exception != null &&
              exceptionType.IsAssignableFrom(
                  exception.GetType()),
            $"{name} is rejected by production language-data validation");
    }
}

static void VerifyLanguageDataRecovery(
    string root)
{
    VerifySuccessfulLanguageDataRollback(root);
    VerifyMissingLanguageDataRollback(root);
    VerifyUnusableLanguageDataRollback(root);
    VerifyLanguageDataCleanupFailure(root);
    VerifyLanguageDataFailureMessages(root);
}

static void VerifySuccessfulLanguageDataRollback(
    string root)
{
    string storage =
        Path.Combine(root, "recovery-success");
    string languageA =
        Path.Combine(root, "recovery-a.xml");
    string languageB =
        Path.Combine(root, "recovery-b.xml");

    WriteLanguageExport(
        languageA,
        "a",
        "RecoveryKey",
        "Language A");
    WriteLanguageExport(
        languageB,
        "b",
        "RecoveryKey",
        "Language B");

    LocalizationService seedLocalization = new();
    LanguageDataService seedService =
        new(seedLocalization, storage);
    seedService.Install(languageA);
    byte[] canonicalA =
        File.ReadAllBytes(
            seedService.GetCanonicalPath());

    LocalizationService localization = new();
    ThrowAfterPromotionHooks hooks = new();
    LanguageDataService service =
        new(localization, storage, hooks);
    service.LoadCanonical();

    Exception? failure =
        RecordException(
            () => service.Install(languageB));

    Check(failure is LanguageDataInstallException
          {
              FailureKind:
                  LanguageDataInstallFailureKind.PreviousSetupRestored
          } &&
          File.ReadAllBytes(service.GetCanonicalPath())
              .SequenceEqual(canonicalA) &&
          ReadEmbeddedLanguage(service.GetCanonicalPath()) == "a" &&
          localization.GetLocalizedName("RecoveryKey") ==
              "Language A" &&
          service.CurrentState.IsAvailable &&
          service.CurrentState.Metadata?.LanguageCode == "a" &&
          !File.Exists(service.GetCanonicalPath() + ".tmp") &&
          !File.Exists(service.GetCanonicalPath() + ".rollback.tmp"),
        "post-promotion publication failure restores canonical bytes, active localization, metadata, and temporary-file cleanliness");
}

static void VerifyMissingLanguageDataRollback(
    string root)
{
    string storage =
        Path.Combine(root, "recovery-missing");
    string languageA =
        Path.Combine(root, "missing-a.xml");
    string languageB =
        Path.Combine(root, "missing-b.xml");

    WriteLanguageExport(
        languageA,
        "a",
        "RecoveryKey",
        "Language A");
    WriteLanguageExport(
        languageB,
        "b",
        "RecoveryKey",
        "Language B");

    LocalizationService seedLocalization = new();
    new LanguageDataService(seedLocalization, storage)
        .Install(languageA);

    LocalizationService localization = new();
    MissingRollbackHooks hooks = new();
    LanguageDataService service =
        new(localization, storage, hooks);
    service.LoadCanonical();

    Exception? failure =
        RecordException(
            () => service.Install(languageB));

    Check(failure is LanguageDataInstallException
          {
              FailureKind:
                  LanguageDataInstallFailureKind.RecoveryFailed
          } &&
          ReadEmbeddedLanguage(service.GetCanonicalPath()) == "b" &&
          localization.EntryCount == 0 &&
          localization.GetLocalizedName("RecoveryKey") == null &&
          service.CurrentState.Availability ==
              LanguageDataAvailability.Invalid &&
          service.CurrentState.Metadata == null &&
          !File.Exists(service.GetCanonicalPath() + ".tmp") &&
          !File.Exists(service.GetCanonicalPath() + ".rollback.tmp"),
        "missing rollback produces explicit invalid cleared state instead of canonical B with active state A");
}

static void VerifyUnusableLanguageDataRollback(
    string root)
{
    string storage =
        Path.Combine(root, "recovery-unusable");
    string languageA =
        Path.Combine(root, "unusable-a.xml");
    string languageB =
        Path.Combine(root, "unusable-b.xml");

    WriteLanguageExport(
        languageA,
        "a",
        "RecoveryKey",
        "Language A");
    WriteLanguageExport(
        languageB,
        "b",
        "RecoveryKey",
        "Language B");

    LocalizationService seedLocalization = new();
    new LanguageDataService(seedLocalization, storage)
        .Install(languageA);

    LocalizationService localization = new();
    using UnusableRollbackHooks hooks = new();
    LanguageDataService service =
        new(localization, storage, hooks);
    service.LoadCanonical();

    Exception? failure =
        RecordException(
            () => service.Install(languageB));

    Check(failure is LanguageDataInstallException
          {
              FailureKind:
                  LanguageDataInstallFailureKind.RecoveryFailed
          } &&
          ReadEmbeddedLanguage(service.GetCanonicalPath()) == "b" &&
          localization.EntryCount == 0 &&
          service.CurrentState.Availability ==
              LanguageDataAvailability.Invalid &&
          File.Exists(service.GetCanonicalPath() + ".rollback.tmp"),
        "unusable rollback preserves the recovery file and produces explicit invalid cleared state without false coherence");
}

static void VerifyLanguageDataCleanupFailure(
    string root)
{
    string storage =
        Path.Combine(root, "cleanup-failure");
    string languageA =
        Path.Combine(root, "cleanup-a.xml");
    string languageB =
        Path.Combine(root, "cleanup-b.xml");
    string languageC =
        Path.Combine(root, "cleanup-c.xml");

    WriteLanguageExport(
        languageA,
        "a",
        "CleanupKey",
        "Language A");
    WriteLanguageExport(
        languageB,
        "b",
        "CleanupKey",
        "Language B");
    WriteLanguageExport(
        languageC,
        "c",
        "CleanupKey",
        "Language C");

    LocalizationService seedLocalization = new();
    new LanguageDataService(seedLocalization, storage)
        .Install(languageA);

    LocalizationService localization = new();
    CleanupFailureHooks hooks = new();
    LanguageDataService service =
        new(localization, storage, hooks);
    service.LoadCanonical();

    Exception? failure =
        RecordException(
            () => service.Install(languageB));

    Check(failure is LanguageDataInstallException
          {
              FailureKind:
                  LanguageDataInstallFailureKind.CleanupFailed
          } &&
          ReadEmbeddedLanguage(service.GetCanonicalPath()) == "b" &&
          localization.GetLocalizedName("CleanupKey") ==
              "Language B" &&
          service.CurrentState.Metadata?.LanguageCode == "b" &&
          File.Exists(service.GetCanonicalPath() + ".rollback.tmp"),
        "rollback cleanup failure is explicit while coherent replacement B remains active");

    LocalizationService retryLocalization = new();
    LanguageDataService retryService =
        new(retryLocalization, storage);
    retryService.LoadCanonical();
    LanguageDataState retryState =
        retryService.Install(languageC);

    string[] remainingFiles =
        Directory.GetFiles(storage);

    Check(retryState.Metadata?.LanguageCode == "c" &&
          retryLocalization.GetLocalizedName("CleanupKey") ==
              "Language C" &&
          remainingFiles.Length == 1 &&
          string.Equals(
              remainingFiles[0],
              retryService.GetCanonicalPath(),
              StringComparison.OrdinalIgnoreCase),
        "the next transaction removes stale rollback ownership before publishing one clean canonical file");
}

static void VerifyLanguageDataFailureMessages(
    string root)
{
    string invalidSource =
        Path.Combine(root, "message-invalid.xml");
    File.WriteAllText(
        invalidSource,
        "<cdb");

    string initialStorage =
        Path.Combine(root, "message-initial");
    LocalizationService initialLocalization = new();
    LanguageDataService initialService =
        new(initialLocalization, initialStorage);
    TestFileDialogService initialFiles =
        new(invalidSource);
    TestMessageDialogService initialMessages =
        new();
    MainViewModel initialViewModel =
        CreateMainViewModel(
            initialLocalization,
            ReferenceDataService.Instance,
            initialMessages,
            initialFiles,
            initialService);

    InvokeLanguageDataSelection(initialViewModel);

    Check(initialMessages.LastError?.Contains(
              "continue using internal IDs",
              StringComparison.Ordinal) == true &&
          initialMessages.LastError?.Contains(
              "previous language data was preserved",
              StringComparison.OrdinalIgnoreCase) == false,
        "failed initial setup reports internal-ID fallback without claiming prior setup preservation");

    string languageA =
        Path.Combine(root, "message-a.xml");
    string languageB =
        Path.Combine(root, "message-b.xml");
    WriteLanguageExport(
        languageA,
        "a",
        "MessageKey",
        "Language A");
    WriteLanguageExport(
        languageB,
        "b",
        "MessageKey",
        "Language B");

    string restoredStorage =
        Path.Combine(root, "message-restored");
    LocalizationService restoredSeed = new();
    new LanguageDataService(restoredSeed, restoredStorage)
        .Install(languageA);
    LocalizationService restoredLocalization = new();
    LanguageDataService restoredService =
        new(
            restoredLocalization,
            restoredStorage,
            new ThrowAfterPromotionHooks());
    restoredService.LoadCanonical();
    TestMessageDialogService restoredMessages =
        new();
    MainViewModel restoredViewModel =
        CreateMainViewModel(
            restoredLocalization,
            ReferenceDataService.Instance,
            restoredMessages,
            new TestFileDialogService(languageB),
            restoredService);

    InvokeLanguageDataSelection(restoredViewModel);

    Check(restoredMessages.LastError?.Contains(
              "previous language data was restored",
              StringComparison.OrdinalIgnoreCase) == true,
        "failed replacement reports previous setup preservation only after proven rollback");

    string failedStorage =
        Path.Combine(root, "message-recovery-failed");
    LocalizationService failedSeed = new();
    new LanguageDataService(failedSeed, failedStorage)
        .Install(languageA);
    LocalizationService failedLocalization = new();
    LanguageDataService failedService =
        new(
            failedLocalization,
            failedStorage,
            new MissingRollbackHooks());
    failedService.LoadCanonical();
    TestMessageDialogService failedMessages =
        new();
    MainViewModel failedViewModel =
        CreateMainViewModel(
            failedLocalization,
            ReferenceDataService.Instance,
            failedMessages,
            new TestFileDialogService(languageB),
            failedService);

    InvokeLanguageDataSelection(failedViewModel);

    Check(failedMessages.LastError?.Contains(
              "previous setup could not be restored",
              StringComparison.OrdinalIgnoreCase) == true &&
          failedMessages.LastError?.Contains(
              "previous language data was preserved",
              StringComparison.OrdinalIgnoreCase) == false &&
          failedService.CurrentState.Availability ==
              LanguageDataAvailability.Invalid,
        "unrecoverable replacement reports internal-ID fallback without a false preservation claim");

    string cleanupStorage =
        Path.Combine(root, "message-cleanup");
    LocalizationService cleanupSeed = new();
    new LanguageDataService(cleanupSeed, cleanupStorage)
        .Install(languageA);
    LocalizationService cleanupLocalization = new();
    LanguageDataService cleanupService =
        new(
            cleanupLocalization,
            cleanupStorage,
            new CleanupFailureHooks());
    cleanupService.LoadCanonical();
    TestMessageDialogService cleanupMessages =
        new();
    MainViewModel cleanupViewModel =
        CreateMainViewModel(
            cleanupLocalization,
            ReferenceDataService.Instance,
            cleanupMessages,
            new TestFileDialogService(languageB),
            cleanupService);

    InvokeLanguageDataSelection(cleanupViewModel);

    Check(cleanupMessages.LastWarning?.Contains(
              "new language data remains active",
              StringComparison.OrdinalIgnoreCase) == true &&
          cleanupMessages.LastError == null &&
          cleanupService.CurrentState.Metadata?.LanguageCode == "b",
        "cleanup-only failure reports coherent successful replacement with a bounded warning");
}

static void InvokeLanguageDataSelection(
    MainViewModel viewModel)
{
    typeof(MainViewModel)
        .GetMethod(
            "OnLanguageDataSelectionRequested",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic)!
        .Invoke(
            viewModel,
            [null, EventArgs.Empty]);
}

static string ReadEmbeddedLanguage(
    string fileName) =>
    System.Xml.Linq.XDocument.Load(fileName)
        .Root!
        .Attribute("lang")!
        .Value;

static void WriteLanguageExport(
    string fileName,
    string languageCode,
    string key,
    string text,
    string version = "",
    string revision = "",
    string? duplicateText = null)
{
    string diagnosticAttributes =
        (string.IsNullOrWhiteSpace(version)
            ? string.Empty
            : $" version=\"{version}\"") +
        (string.IsNullOrWhiteSpace(revision)
            ? string.Empty
            : $" revision=\"{revision}\"");
    string duplicate =
        duplicateText == null
            ? string.Empty
            : $"<sheet name=\"second\"><{key}><title>{duplicateText}</title></{key}></sheet>";

    File.WriteAllText(
        fileName,
        $"<cdb project=\"Wartales\" lang=\"{languageCode}\"{diagnosticAttributes}>" +
        $"<sheet name=\"first\"><{key}><name>{text}</name></{key}></sheet>" +
        duplicate +
        "</cdb>");
}

static Exception? RecordException(
    Action action)
{
    try
    {
        action();
        return null;
    }
    catch (Exception exception)
    {
        return exception;
    }
}

static void VerifyRealLanguageDataFile(
    string sourceFile)
{
    string root =
        Path.Combine(
            Path.GetTempPath(),
            "WartalesEditorRealLanguageData",
            Guid.NewGuid().ToString("N"));

    try
    {
        LocalizationService localization = new();
        LanguageDataService service =
            new(localization, root);
        LanguageDataState state =
            service.Install(sourceFile);

        KeyValuePair<string, string> sample =
            localization.Capture()
                .Names
                .First();

        LocalizationService reloadedLocalization =
            new();
        LanguageDataService reloadedService =
            new(reloadedLocalization, root);
        LanguageDataState reloaded =
            reloadedService.LoadCanonical();

        Check(state.IsAvailable &&
              state.MappingCount > 0 &&
              !string.IsNullOrWhiteSpace(
                  state.Metadata?.LanguageCode) &&
              File.Exists(service.GetCanonicalPath()) &&
              reloaded.IsAvailable &&
              reloaded.Metadata?.LanguageCode ==
                  state.Metadata?.LanguageCode &&
              reloaded.MappingCount == state.MappingCount &&
              reloadedLocalization.GetLocalizedName(sample.Key) ==
                  sample.Value,
            "real Wartales export validates, installs, fresh-service reloads, and publishes localized lookup data");

        Console.WriteLine(
            $"PASS real Wartales language data: {state.Metadata!.LanguageCode}, {state.MappingCount:N0} localized names, reload and lookup {sample.Key}");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void VerifyRestorePreviousValuesContract()
{
    VerifySharedPresetPreviousValues();
    VerifyOrdinaryOpenTimeBetweenRests();
    VerifyPartyEconomyPreviousValues();
    VerifyMovementPreviousValues();
    VerifyRainPreviousValues();
    VerifyRandomTraitExclusionsRestoreLifecycle();
    VerifyRteAccountingAndPartyImmediateRestore();
    Console.WriteLine(
        "PASS unified Restore Previous Values authority, persistence, profiles, and history");
}

static void VerifyOrdinaryOpenTimeBetweenRests()
{
    ProjectModel project = WithUnknownSource(CreateProject(
        Sheet(
            "constant",
            new JObject
            {
                ["id"] = "TirednessAmountHours",
                ["value"] = 30
            })));
    ProjectMutationService mutation = new();
    GameplayOperationStateService states = new(mutation);
    GameplayPresetService service = new(mutation, states);
    ProjectOperationService executor = new();

    Check(ApplyPreset(
            executor,
            service,
            project,
            ProgressionType.TimeBetweenRests,
            "Longer").Succeeded &&
          states.CanRestorePreviousValues(
              project,
              ProgressionType.TimeBetweenRests),
        "ordinary-open Time Between Rests enables bounded local Restore");
    ProjectOperationResult restore = executor.Execute(
        new GameplayPresetOperation(
            service,
            ProgressionType.TimeBetweenRests,
            "Vanilla",
            true),
        project);
    Check(restore.Succeeded,
        "ordinary-open Time Between Rests Restore succeeds");
    CheckNumber(
        project,
        "constant",
        "TirednessAmountHours",
        "value",
        30);
}

static void VerifyRteAccountingAndPartyImmediateRestore()
{
    ProjectModel exclusionsProject = WithUnknownSource(
        CreateRandomTraitExclusionProject());
    ProjectMutationService exclusionsMutation = new();
    GameplayOperationStateService exclusionsStates = new(exclusionsMutation);
    RandomTraitExclusionsService exclusionsService = new(
        exclusionsMutation,
        exclusionsStates);
    ProjectOperationService executor = new();
    string[] baselineAllowed =
    {
        "PositiveTrue", "NegativeAbsent", "PositiveAbsent"
    };
    Check(executor.Execute(
            new RandomTraitExclusionsOperation(
                exclusionsService,
                baselineAllowed),
            exclusionsProject).Succeeded,
        "RTE accounting fixture establishes captured previous values");
    Check(exclusionsStates.CanRestorePreviousValues(
            exclusionsProject,
            ProgressionType.RandomTraitExclusions),
        "ordinary-open RTE captures bounded local Restore authority");
    int preRteCount = new EffectiveChangeCountService().Calculate(exclusionsProject);
    IReadOnlyList<ChangeSummaryItemModel> baselineSummary =
        new ChangeSummaryService().BuildItems(
            exclusionsProject,
            new ModificationSnapshotService().CreateSnapshot(exclusionsProject),
            new LocalizationService());
    Check(preRteCount == 0 &&
          new ProfileEffectiveChangeCountService().Calculate(
              new ModProfileService().CreateProfile(
                  exclusionsProject,
                  "RTE captured baseline")) == 0 &&
          baselineSummary.All(item =>
              item.SettingName != "Random Trait Exclusions"),
        "RTE captured baseline contributes zero project/profile changes and no synthetic summary row");

    ProjectOperationResult threeExcluded = executor.Execute(
        new RandomTraitExclusionsOperation(
            exclusionsService,
            Array.Empty<string>()),
        exclusionsProject);
    Check(threeExcluded.Succeeded &&
          new EffectiveChangeCountService().Calculate(exclusionsProject) ==
              preRteCount + 3 &&
          new ProfileEffectiveChangeCountService().Calculate(
              new ModProfileService().CreateProfile(
                  exclusionsProject,
                  "RTE three exclusions")) == 3,
        "RTE three additional exclusions contribute exactly three project/profile changes");
    IReadOnlyList<ChangeSummaryItemModel> threeExcludedSummary =
        new ChangeSummaryService().BuildItems(
            exclusionsProject,
            new ModificationSnapshotService().CreateSnapshot(exclusionsProject),
            new LocalizationService());
    Check(threeExcludedSummary.Count(item =>
              item.CategoryName == "trait" &&
              item.PropertyName == "done") == 3 &&
          threeExcludedSummary.All(item =>
              item.SettingName != "Random Trait Exclusions"),
        "RTE three additional exclusions produce three leaf rows without synthetic double counting");
    string threeExcludedJson = Json(exclusionsProject);

    RandomTraitExclusionsDialogViewModel exclusionsViewModel = new(
        exclusionsProject,
        exclusionsService,
        new LocalizationService());
    Check(exclusionsViewModel.TryRestorePreviousValues(),
        "RTE immediate Restore resolves current captured authority");
    ProjectOperationResult exclusionsRestore = executor.Execute(
        new RandomTraitExclusionsOperation(
            exclusionsService,
            exclusionsViewModel.GetAllowedTraitIds()),
        exclusionsProject);
    Check(exclusionsRestore.Succeeded &&
          Entry(exclusionsProject, "trait", "PositiveTrue").SourceEntry!["done"]!.Value<bool>() &&
          Entry(exclusionsProject, "trait", "NegativeAbsent").SourceEntry!.Property("done") == null &&
          Entry(exclusionsProject, "trait", "PositiveAbsent").SourceEntry!.Property("done") == null &&
          Entry(exclusionsProject, "trait", "NegativeDisabled").SourceEntry!["done"]!.Value<bool>() == false &&
          new EffectiveChangeCountService().Calculate(exclusionsProject) == preRteCount &&
          new ProfileEffectiveChangeCountService().Calculate(
              new ModProfileService().CreateProfile(
                  exclusionsProject,
                  "RTE restored baseline")) == 0,
        "RTE immediate Restore returns exact true, false, and absent baseline at zero project/profile contribution");
    IReadOnlyList<ChangeSummaryItemModel> restoredSummary =
        new ChangeSummaryService().BuildItems(
            exclusionsProject,
            new ModificationSnapshotService().CreateSnapshot(exclusionsProject),
            new LocalizationService());
    Check(restoredSummary.All(item =>
              item.SettingName != "Random Trait Exclusions"),
        "RTE restored baseline has no synthetic Review Changes row");
    string restoredJson = Json(exclusionsProject);
    EditHistoryService exclusionsHistory = new();
    exclusionsHistory.Record(new ProjectOperationHistoryAction(
        "Random Trait Exclusions",
        exclusionsRestore.MutationResult,
        new ProjectOperationTransactionService()));
    Check(exclusionsHistory.Undo() &&
          Json(exclusionsProject) == threeExcludedJson &&
          new EffectiveChangeCountService().Calculate(exclusionsProject) ==
              preRteCount + 3,
        "RTE Restore Undo returns to three effective exclusions");
    Check(exclusionsHistory.Redo() &&
          Json(exclusionsProject) == restoredJson &&
          new EffectiveChangeCountService().Calculate(exclusionsProject) == preRteCount,
        "RTE Restore Redo returns to zero effective exclusions");

    VerifyPartyEconomyImmediateRestore(
        ProgressionType.VolunteerWages,
        CreateProject(
            Sheet(
                "trait",
                new JObject
                {
                    ["id"] = "Volunteer",
                    ["desc"] = "Volunteer",
                    ["props"] = new JObject { ["value"] = 25 }
                })),
        new PartyEconomySettings { VolunteerPercentage = 75 });
    VerifyPartyEconomyImmediateRestore(
        ProgressionType.ValourPoints,
        CreateValourProject(1, 2, 3),
        new PartyEconomySettings
        {
            MaximumValour = 20,
            RestoredValour = 5,
            TentTier1Valour = 2,
            TentTier2Valour = 3,
            TentTier3Valour = 4
        });
    VerifyPartyEconomyImmediateRestore(
        ProgressionType.CarryingCapacity,
        CreateCarryingProject(10, 10, 10, 5, 10),
        new PartyEconomySettings
        {
            SaddlebagCapacity = 20,
            PonyStartingCapacity = 80,
            HitchingPostTier1Base = 20,
            HitchingPostTier2Base = 40,
            HitchingPostTier3Base = 60,
            HitchingPostTier1Trait = 0,
            HitchingPostTier2Trait = 20,
            HitchingPostTier3Trait = 30
        });

    Console.WriteLine(
        "PASS RTE baseline accounting and universal Party Economy immediate restoration");
}

static void VerifyPartyEconomyImmediateRestore(
    ProgressionType type,
    ProjectModel project,
    PartyEconomySettings modifiedSettings)
{
    WithUnknownSource(project);
    string baselineJson = Json(project);
    ProjectMutationService mutation = new();
    GameplayOperationStateService states = new(mutation);
    PartyEconomyService service = new(mutation, states);
    PartyEconomyDialogViewModel viewModel = new(project, service, type);
    string missingJson = Json(project);
    Check(!viewModel.TryRestorePreviousValues() &&
          Json(project) == missingJson &&
          project.GameplayOperationStates.Count == 0,
        $"{type} missing-history Restore does not apply or create state");

    ProjectOperationResult modified = new ProjectOperationService().Execute(
        new PartyEconomyOperation(service, type, modifiedSettings),
        project);
    Check(modified.Succeeded,
        $"{type} immediate-Restore fixture applies modified settings");
    Check(states.CanRestorePreviousValues(project, type),
        $"ordinary-open {type} captures bounded local Restore authority");
    string modifiedJson = Json(project);
    viewModel.RefreshFromProject();
    Check(viewModel.TryRestorePreviousValues(),
        $"{type} Restore resolves and populates captured fields");
    ProjectOperationResult restore = new ProjectOperationService().Execute(
        new PartyEconomyOperation(service, type, viewModel.CreateSettings()),
        project);
    Check(restore.Succeeded && Json(project) == baselineJson,
        $"{type} Restore applies immediately without a second Apply action");
    VerifyUndoRedo(
        $"{type} immediate previous values",
        restore,
        project,
        modifiedJson,
        baselineJson);
}

static void VerifyRandomTraitExclusionsRestoreLifecycle()
{
    string[] modifiedSelection =
    {
        "PositiveTrue", "PositiveAbsent", "NegativeDisabled"
    };

    ProjectModel staleDialogProject = CreateRandomTraitExclusionProject();
    ProjectMutationService staleDialogMutation = new();
    GameplayOperationStateService staleDialogStates = new(staleDialogMutation);
    RandomTraitExclusionsService staleDialogService = new(
        staleDialogMutation,
        staleDialogStates);
    RandomTraitExclusionsDialogViewModel staleDialogViewModel = new(
        staleDialogProject,
        staleDialogService,
        new LocalizationService());
    ProjectOperationResult staleDialogApply = new ProjectOperationService().Execute(
        new RandomTraitExclusionsOperation(
            staleDialogService,
            modifiedSelection),
        staleDialogProject);
    Check(staleDialogApply.Succeeded &&
          staleDialogProject.GameplayOperationStates.Count == 1,
        "RTE stale-dialog fixture captures compatible operation state");
    staleDialogViewModel.RefreshFromProject(
        staleDialogProject,
        staleDialogService);
    EditHistoryService staleDialogHistory = new();
    staleDialogHistory.Record(new ProjectOperationHistoryAction(
        "Random Trait Exclusions",
        staleDialogApply.MutationResult,
        new ProjectOperationTransactionService()));
    Check(staleDialogHistory.Undo() &&
          staleDialogProject.GameplayOperationStates.Count == 0,
        "RTE external Undo removes authoritative state while the dialog remains open");
    string afterUndo = Json(staleDialogProject);
    bool staleDialogApplyRequested = false;
    if (staleDialogViewModel.TryRestorePreviousValues())
        staleDialogApplyRequested = true;
    Check(!staleDialogApplyRequested &&
          staleDialogViewModel.LastRestoreStatus ==
              RandomTraitExclusionRestoreStatus.Unavailable &&
          Json(staleDialogProject) == afterUndo &&
          staleDialogProject.GameplayOperationStates.Count == 0 &&
          !staleDialogHistory.CanUndo &&
          staleDialogHistory.CanRedo,
        "RTE stale open-dialog Restore after Undo rejects without Apply, mutation, state, or history");

    ProjectModel missingHistoryProject = CreateRandomTraitExclusionProject();
    ProjectMutationService missingHistoryMutation = new();
    RandomTraitExclusionsService missingHistoryService = new(
        missingHistoryMutation,
        new GameplayOperationStateService(missingHistoryMutation));
    string missingHistoryJson = Json(missingHistoryProject);
    CheckThrows<InvalidOperationException>(
        () => missingHistoryService.GetPreviousAllowedTraitIds(missingHistoryProject),
        "RTE direct previous-value resolution rejects missing history");
    Check(Json(missingHistoryProject) == missingHistoryJson &&
          missingHistoryProject.GameplayOperationStates.Count == 0 &&
          !missingHistoryProject.Sheets.SelectMany(sheet => sheet.Entries)
              .SelectMany(entry => entry.Properties).Any(property => property.IsModified),
        "RTE direct missing-history rejection creates no mutation or baseline state");

    ProjectModel replacementProject = CreateRandomTraitExclusionProject();
    ProjectMutationService replacementMutation = new();
    GameplayOperationStateService replacementStates = new(replacementMutation);
    RandomTraitExclusionsService replacementService = new(
        replacementMutation,
        replacementStates);
    Check(new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(
                replacementService,
                modifiedSelection),
            replacementProject).Succeeded,
        "RTE authoritative-state replacement fixture establishes baseline A");
    RandomTraitExclusionsDialogViewModel replacementViewModel = new(
        replacementProject,
        replacementService,
        new LocalizationService());
    Check(replacementViewModel.PositiveTraits.Single(item =>
              item.Id == "PositiveTrue").Candidate.BaselineDone ==
          RandomTraitDoneBaseline.True,
        "RTE open dialog initially caches baseline A presentation data");

    ProjectModel baselineBProject = CreateRandomTraitExclusionProject(
        positiveTrue: false,
        negativeAbsent: true,
        positiveAbsent: null,
        negativeDisabled: true);
    ProjectMutationService baselineBMutation = new();
    GameplayOperationStateService baselineBStates = new(baselineBMutation);
    RandomTraitExclusionsService baselineBService = new(
        baselineBMutation,
        baselineBStates);
    Check(new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(
                baselineBService,
                modifiedSelection),
            baselineBProject).Succeeded,
        "RTE authoritative-state replacement fixture establishes compatible baseline B");
    GameplayOperationStateModel baselineBState =
        baselineBProject.GameplayOperationStates.Single().DeepClone();
    ProjectMutationResult stateReplacement =
        replacementStates.RestoreSnapshotStatesWithMutations(
            replacementProject,
            new[] { baselineBState });
    Check(stateReplacement.GameplayOperationStateRollbackRecords.Count == 1 &&
          replacementProject.GameplayOperationStates.Single().BaselineArray
              .OfType<JObject>().Single(record =>
                  record.Value<string>("id") == "PositiveTrue")
              .Value<string>("doneState") == nameof(RandomTraitDoneBaseline.False),
        "RTE production snapshot/profile state path replaces baseline A with baseline B");

    Check(replacementViewModel.TryRestorePreviousValues() &&
          replacementViewModel.LastRestoreStatus ==
              RandomTraitExclusionRestoreStatus.Succeeded &&
          !replacementViewModel.PositiveTraits.Single(item =>
              item.Id == "PositiveTrue").IsAllowed &&
          replacementViewModel.NegativeTraits.Single(item =>
              item.Id == "NegativeAbsent").IsAllowed &&
          replacementViewModel.PositiveTraits.Single(item =>
              item.Id == "PositiveAbsent").IsAllowed &&
          replacementViewModel.NegativeTraits.Single(item =>
              item.Id == "NegativeDisabled").IsAllowed,
        "RTE restore resolves baseline B at click time instead of cached baseline A");
    string beforeReplacementRestore = Json(replacementProject);
    ProjectOperationResult replacementRestore = new ProjectOperationService().Execute(
        new RandomTraitExclusionsOperation(
            replacementService,
            replacementViewModel.GetAllowedTraitIds()),
        replacementProject);
    Check(replacementRestore.Succeeded &&
          Entry(replacementProject, "trait", "PositiveTrue").SourceEntry!["done"]!.Value<bool>() == false &&
          Entry(replacementProject, "trait", "NegativeAbsent").SourceEntry!["done"]!.Value<bool>() &&
          Entry(replacementProject, "trait", "PositiveAbsent").SourceEntry!.Property("done") == null &&
          Entry(replacementProject, "trait", "NegativeDisabled").SourceEntry!["done"]!.Value<bool>(),
        "RTE state-replacement restore reproduces baseline B true, false, and absent semantics");
    string afterReplacementRestore = Json(replacementProject);
    VerifyUndoRedo(
        "RTE baseline-B previous values",
        replacementRestore,
        replacementProject,
        beforeReplacementRestore,
        afterReplacementRestore);
    replacementViewModel.RefreshFromProject(
        replacementProject,
        replacementService);
    Check(replacementViewModel.PositiveTraits.Single(item =>
              item.Id == "PositiveTrue").Candidate.BaselineDone ==
          RandomTraitDoneBaseline.False,
        "RTE candidate refresh replaces stale baseline presentation metadata");

    ProjectModel normalProject = CreateRandomTraitExclusionProject();
    ProjectMutationService normalMutation = new();
    GameplayOperationStateService normalStates = new(normalMutation);
    RandomTraitExclusionsService normalService = new(
        normalMutation,
        normalStates);
    RandomTraitExclusionsDialogViewModel normalViewModel = new(
        normalProject,
        normalService,
        new LocalizationService());
    Check(new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(normalService, modifiedSelection),
            normalProject).Succeeded &&
          normalViewModel.TryRestorePreviousValues(),
        "RTE normal open-dialog Restore resolves its captured previous values");
    string beforeNormalRestore = Json(normalProject);
    ProjectOperationResult normalRestore = new ProjectOperationService().Execute(
        new RandomTraitExclusionsOperation(
            normalService,
            normalViewModel.GetAllowedTraitIds()),
        normalProject);
    Check(normalRestore.Succeeded &&
          Entry(normalProject, "trait", "PositiveTrue").SourceEntry!["done"]!.Value<bool>() &&
          Entry(normalProject, "trait", "NegativeAbsent").SourceEntry!.Property("done") == null &&
          Entry(normalProject, "trait", "PositiveAbsent").SourceEntry!.Property("done") == null &&
          Entry(normalProject, "trait", "NegativeDisabled").SourceEntry!["done"]!.Value<bool>() == false,
        "RTE normal Restore reproduces exact true, false, and absent previous values");
    string afterNormalRestore = Json(normalProject);
    VerifyUndoRedo(
        "RTE normal previous values",
        normalRestore,
        normalProject,
        beforeNormalRestore,
        afterNormalRestore);

    Console.WriteLine(
        "PASS Random Trait Exclusions execution-time restore authority and stale-dialog safety");
}

static void VerifyPartyEconomyPreviousValues()
{
    ProjectModel project = WithUnknownSource(CreateProject(
        Sheet(
            "trait",
            new JObject
            {
                ["id"] = "Volunteer",
                ["desc"] = "Volunteer",
                ["props"] = new JObject
                {
                    ["value"] = 25
                }
            })));
    ProjectMutationService mutation = new();
    GameplayOperationStateService states = new(mutation);
    PartyEconomyService service = new(mutation, states);
    PartyEconomyDialogViewModel viewModel = new(
        project,
        service,
        ProgressionType.VolunteerWages);

    Check(!viewModel.CanRestorePreviousValues,
        "Party Economy restore is unavailable before captured history exists");
    viewModel.RestorePreviousValues();
    Check(viewModel.VolunteerPercentage == 25 &&
          project.GameplayOperationStates.Count == 0,
        "Party Economy missing-history restore does not fabricate state");

    ProjectOperationResult apply = new ProjectOperationService().Execute(
        new PartyEconomyOperation(
            service,
            ProgressionType.VolunteerWages,
            new PartyEconomySettings
            {
                VolunteerPercentage = 50
            }),
        project);
    Check(apply.Succeeded,
        "Party Economy first application captures previous values");
    viewModel.RefreshFromProject();
    Check(viewModel.CanRestorePreviousValues,
        "Party Economy restore becomes available after capture");
    viewModel.RestorePreviousValues();
    Check(viewModel.VolunteerPercentage == 25,
        "Party Economy restore fills fields from captured previous values");
    ProjectOperationResult restore = new ProjectOperationService().Execute(
        new PartyEconomyOperation(
            service,
            ProgressionType.VolunteerWages,
            viewModel.CreateSettings()),
        project);
    Check(restore.Succeeded,
        "Party Economy applies captured previous values");
    CheckNumber(project, "trait", "Volunteer", "props.value", 25);
}

static void VerifySharedPresetPreviousValues()
{
    ProjectMutationService mutation = new();
    GameplayOperationStateService states = new(mutation);
    GameplayPresetService service = new(mutation, states);
    ProjectOperationService executor = new();
    ProjectModel project = WithUnknownSource(CreateProject(
        Sheet(
            "constant",
            new JObject
            {
                ["id"] = "FishingDurationControl",
                ["value"] = 7.5
            })));

    Check(!service.CanRestorePreviousValues(
            project,
            ProgressionType.FishingSpeed),
        "shared preset restore is unavailable before the tool captures history");
    string missingBaseline = Json(project);
    ProjectOperationResult missing = executor.Execute(
        new GameplayPresetOperation(
            service,
            ProgressionType.FishingSpeed,
            "Vanilla",
            true),
        project);
    Check(!missing.Succeeded &&
          Json(project) == missingBaseline &&
          project.GameplayOperationStates.Count == 0,
        "missing shared-preset history fails without blessing current values");

    Check(ApplyPreset(
            executor,
            service,
            project,
            ProgressionType.FishingSpeed,
            "Faster").Succeeded,
        "shared preset first change captures previous values");
    Check(states.CanRestorePreviousValues(
            project,
            ProgressionType.FishingSpeed),
        "ordinary-open scalar preset captures bounded local Restore authority");
    Check(ApplyPreset(
            executor,
            service,
            project,
            ProgressionType.FishingSpeed,
            "Fast").Succeeded,
        "shared preset second change preserves the first baseline");
    CheckTokenNumber(
        project.GameplayOperationStates.Single().BaselineArray[0]!["value"]!,
        7.5,
        "shared preset retained first pre-tool value");
    string beforeRestore = Json(project);
    ProjectOperationResult restore = executor.Execute(
        new GameplayPresetOperation(
            service,
            ProgressionType.FishingSpeed,
            "Vanilla",
            true),
        project);
    Check(restore.Succeeded,
        "shared preset previous values restore succeeds");
    CheckNumber(
        project,
        "constant",
        "FishingDurationControl",
        "value",
        7.5);
    VerifyUndoRedo(
        "shared preset previous values",
        restore,
        project,
        beforeRestore,
        Json(project));

    ProjectModel profileSource = CreateProject(
        Sheet(
            "constant",
            new JObject
            {
                ["id"] = "FishingDurationControl",
                ["value"] = 7.5
            }));
    ProjectMutationService profileMutation = new();
    GameplayPresetService profileService = new(
        profileMutation,
        new GameplayOperationStateService(profileMutation));
    Check(ApplyPreset(
            executor,
            profileService,
            profileSource,
            ProgressionType.FishingSpeed,
            "Fast").Succeeded,
        "profile source captures shared-preset previous values");
    ModProfileModel profile = new ModProfileService().CreateProfile(
        profileSource,
        "Previous Values profile");
    ProjectModel profileTarget = CreateProject(
        Sheet(
            "constant",
            new JObject
            {
                ["id"] = "FishingDurationControl",
                ["value"] = 7.5
            }));
    ModificationSnapshotImportResultModel profileResult =
        new ModProfileWorkflowService().ApplyProfile(profileTarget, profile);
    Check(!profileResult.HasFailures,
        "profile transports compatible shared-preset operation state");
    ProjectMutationService targetMutation = new();
    GameplayPresetService targetService = new(
        targetMutation,
        new GameplayOperationStateService(targetMutation));
    ProjectOperationResult profileRestore = executor.Execute(
        new GameplayPresetOperation(
            targetService,
            ProgressionType.FishingSpeed,
            "Vanilla",
            true),
        profileTarget);
    Check(profileRestore.Succeeded,
        "profile-transported previous values restore successfully");
    CheckNumber(
        profileTarget,
        "constant",
        "FishingDurationControl",
        "value",
        7.5);

    ProjectModel reloadTarget = CreateProject(
        Sheet(
            "constant",
            new JObject
            {
                ["id"] = "FishingDurationControl",
                ["value"] = 2.0
            }));
    GameplayOperationStatePersistenceService persistence = new();
    string cdbPath = Path.Combine(
        Path.GetTempPath(),
        $"wartales-restore-{Guid.NewGuid():N}.cdb");
    try
    {
        JsonDataService dataService = new();
        dataService.SaveProject(profileSource, cdbPath);
        reloadTarget = dataService.LoadProject(cdbPath);
        ProjectMutationService reloadMutation = new();
        GameplayPresetService reloadService = new(
            reloadMutation,
            new GameplayOperationStateService(reloadMutation));
        ProjectOperationResult reloadRestore = executor.Execute(
            new GameplayPresetOperation(
                reloadService,
                ProgressionType.FishingSpeed,
                "Vanilla",
                true),
            reloadTarget);
        Check(reloadRestore.Succeeded,
            "sidecar-loaded previous values restore successfully");
        CheckNumber(
            reloadTarget,
            "constant",
            "FishingDurationControl",
            "value",
            7.5);
    }
    finally
    {
        string sidecar = persistence.GetSidecarPath(cdbPath);
        if (File.Exists(sidecar)) File.Delete(sidecar);
        if (File.Exists(cdbPath)) File.Delete(cdbPath);
    }
}

static void VerifyMovementPreviousValues()
{
    ProjectModel project = WithUnknownSource(
        CreateMovementProject(7, 12));
    ProjectMutationService mutation = new();
    GameplayOperationStateService states = new(mutation);
    OverworldMovementSpeedService service = new(mutation, states);
    ProjectOperationService executor = new();

    Check(!service.CanRestorePreviousValues(project),
        "movement restore is unavailable before captured history exists");
    string before = Json(project);
    ProjectOperationResult missing = executor.Execute(
        new OverworldMovementSpeedOperation(
            service,
            OverworldMovementPreset.Vanilla,
            true),
        project);
    Check(!missing.Succeeded && Json(project) == before,
        "missing movement history fails without mutation");

    Check(executor.Execute(
            new OverworldMovementSpeedOperation(
                service,
                OverworldMovementPreset.Fast),
            project).Succeeded,
        "movement preset applies from non-catalog previous values");
    Check(states.CanRestorePreviousValues(
            project,
            ProgressionType.OverworldMovementSpeed),
        "ordinary-open multi-target movement captures bounded local Restore authority");
    string modified = Json(project);
    ProjectOperationResult restore = executor.Execute(
        new OverworldMovementSpeedOperation(
            service,
            OverworldMovementPreset.Vanilla,
            true),
        project);
    Check(restore.Succeeded,
        "movement previous values restore succeeds");
    CheckNumber(project, "constant", "PlayerBaseSpeed", "value", 7);
    CheckNumber(project, "constant", "PlayerRunSpeed", "value", 12);
    VerifyUndoRedo(
        "movement previous values",
        restore,
        project,
        modified,
        Json(project));
}

static void VerifyRainPreviousValues()
{
    ProjectModel project = CreateRainProject(1);
    ProjectMutationService mutation = new();
    GameplayOperationStateService states = new(mutation);
    RainFrequencyService service = new(mutation, states);
    ProjectOperationService executor = new();

    Check(!service.CanRestorePreviousValues(project),
        "rain restore is unavailable before captured history exists");
    string before = Json(project);
    ProjectOperationResult missing = executor.Execute(
        new RainFrequencyOperation(
            service,
            RainFrequencyPreset.Vanilla,
            true),
        project);
    Check(!missing.Succeeded && Json(project) == before,
        "missing rain history fails without mutation");

    Check(executor.Execute(
            new RainFrequencyOperation(
                service,
                RainFrequencyPreset.LessRain),
            project).Succeeded,
        "rain preset captures intentionally non-canonical regional values");
    string modified = Json(project);
    ProjectOperationResult restore = executor.Execute(
        new RainFrequencyOperation(
            service,
            RainFrequencyPreset.Vanilla,
            true),
        project);
    Check(restore.Succeeded,
        "rain previous values restore succeeds");
    foreach (RainRegionDefinition region in RainFrequencyService.Regions)
        CheckNumber(
            project,
            "region",
            region.EntryId,
            RainFrequencyService.PropertyPath,
            region.VanillaValue + 1);
    VerifyUndoRedo(
        "rain previous values",
        restore,
        project,
        modified,
        Json(project));
}

static void VerifyPropertyRemovalMutationPrimitive()
{
    ProjectMutationService mutation = new();
    ProjectOperationTransactionService transaction = new();

    ProjectModel project = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "RemovalFixture",
                ["first"] = 1,
                ["settings"] = new JObject
                {
                    ["keepBefore"] = 10,
                    ["removeMe"] = 20,
                    ["keepAfter"] = 30
                },
                ["last"] = 4
            }));
    EntryModel entry = Entry(project, "item", "RemovalFixture");
    PropertyModel removedProperty = entry.Properties.Single(property =>
        property.EffectivePropertyPath == "settings.removeMe");
    JProperty removedSource = removedProperty.SourceProperty!;
    PropertyModel[] originalPropertyOrder = entry.Properties.ToArray();

    _ = mutation.EnsurePropertyByPath(
        entry,
        "settings.removeMe",
        new JValue(25));
    Check(removedProperty.IsModified,
        "property-removal fixture establishes prior modified state");

    ProjectMutationResult removal = mutation.RemovePropertyByPath(
        entry,
        "settings.removeMe");
    Check(removal.WasModified &&
          removal.RemovedProperties.Count == 1 &&
          removal.RemovedPropertyRollbackRecords.Count == 1,
        "property removal is represented in its mutation result");
    Check(!entry.Properties.Contains(removedProperty) &&
          removedSource.Parent == null &&
          entry.SourceEntry!["settings"]!["keepBefore"]!.Value<int>() == 10 &&
          entry.SourceEntry["settings"]!["keepAfter"]!.Value<int>() == 30,
        "nested removal detaches only the known property");

    string removedJson = Json(project);
    EditHistoryService history = new();
    history.Record(new ProjectOperationHistoryAction(
        "Remove nested property",
        removal,
        transaction));

    for (int cycle = 0; cycle < 3; cycle++)
    {
        Check(history.Undo(),
            $"property removal Undo cycle {cycle + 1}");
        Check(ReferenceEquals(
                  entry.Properties.Single(property =>
                      property.EffectivePropertyPath == "settings.removeMe"),
                  removedProperty) &&
              ReferenceEquals(
                  ((JObject)entry.SourceEntry!["settings"]!)
                      .Property("removeMe"),
                  removedSource),
            "property removal Undo restores exact model and JSON instances");
        Check(entry.Properties.SequenceEqual(originalPropertyOrder) &&
              removedProperty.IsModified,
            "property removal Undo restores order and prior modification state");

        Check(history.Redo(),
            $"property removal Redo cycle {cycle + 1}");
        Check(Json(project) == removedJson &&
              !entry.Properties.Contains(removedProperty) &&
              removedSource.Parent == null,
            "property removal Redo deterministically removes the same instances");
    }

    string missingBefore = Json(project);
    CheckThrows<InvalidOperationException>(
        () => mutation.RemovePropertyByPath(entry, "settings.missing"),
        "missing property removal fails explicitly");
    Check(Json(project) == missingBefore,
        "missing property removal does not mutate JSON");

    ProjectModel emptyParentProject = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "EmptyParentFixture",
                ["settings"] = new JObject
                {
                    ["onlyMember"] = 1
                }
            }));
    EntryModel emptyParentEntry =
        Entry(emptyParentProject, "item", "EmptyParentFixture");
    _ = mutation.RemovePropertyByPath(
        emptyParentEntry,
        "settings.onlyMember");
    Check(emptyParentEntry.SourceEntry!["settings"] is JObject emptyObject &&
          !emptyObject.Properties().Any(),
        "property removal preserves an empty parent object");

    ProjectModel createdProject = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "CreatedRemovalFixture",
                ["settings"] = new JObject()
            }));
    EntryModel createdEntry =
        Entry(createdProject, "item", "CreatedRemovalFixture");
    string createdBaseline = Json(createdProject);
    ProjectMutationResult creation = mutation.EnsurePropertyByPath(
        createdEntry,
        "settings.temporary",
        new JValue(7));
    PropertyModel createdProperty = creation.CreatedProperties.Single();
    string createdPresent = Json(createdProject);
    ProjectMutationResult createdRemoval = mutation.RemovePropertyByPath(
        createdEntry,
        "settings.temporary");
    EditHistoryService createdHistory = new();
    createdHistory.Record(new ProjectOperationHistoryAction(
        "Remove created property",
        createdRemoval,
        transaction));
    Check(createdHistory.Undo() && Json(createdProject) == createdPresent &&
          createdEntry.Properties.Contains(createdProperty) &&
          createdProperty.IsModified,
        "created-property removal Undo restores the exact created property");
    Check(createdHistory.Redo() &&
          !createdEntry.Properties.Contains(createdProperty),
        "created-property removal Redo remains absent");
    Check(createdHistory.Undo() && Json(createdProject) == createdPresent &&
          createdEntry.Properties.Contains(createdProperty),
        "created-property removal second Undo is deterministic");
    transaction.Rollback(creation);
    Check(Json(createdProject) == createdBaseline &&
          !createdEntry.Properties.Contains(createdProperty),
        "created property retains creation/removal rollback symmetry");

    ProjectModel mixedProject = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "MixedRemovalFixture",
                ["a"] = 1,
                ["b"] = 2,
                ["c"] = 3
            }));
    EntryModel mixedEntry = Entry(mixedProject, "item", "MixedRemovalFixture");
    string mixedBaseline = Json(mixedProject);
    PropertyModel[] mixedBaselineOrder = mixedEntry.Properties.ToArray();
    ProjectMutationResult mixed = mutation.EnsurePropertyByPath(
        mixedEntry, "a", new JValue(10));
    mixed.Merge(mutation.EnsurePropertyByPath(
        mixedEntry, "d", new JValue(4)));
    mixed.Merge(mutation.RemovePropertyByPath(mixedEntry, "b"));
    string mixedFinal = Json(mixedProject);
    EditHistoryService mixedHistory = new();
    mixedHistory.Record(new ProjectOperationHistoryAction(
        "Mixed property mutation",
        mixed,
        transaction));
    Check(mixedHistory.Undo() && Json(mixedProject) == mixedBaseline &&
          mixedEntry.Properties.SequenceEqual(mixedBaselineOrder),
        "mixed mutation Undo restores exact JSON and model order");
    Check(mixedHistory.Redo() && Json(mixedProject) == mixedFinal,
        "mixed mutation Redo restores the exact final JSON");

    ProjectModel rejectedProject = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "RejectedRemovalFixture",
                ["a"] = 1,
                ["b"] = 2,
                ["c"] = 3
            }));
    string rejectedBaseline = Json(rejectedProject);
    EntryModel rejectedEntry =
        Entry(rejectedProject, "item", "RejectedRemovalFixture");
    PropertyModel[] rejectedOrder = rejectedEntry.Properties.ToArray();
    ProjectOperationResult rejected = new ProjectOperationService(
        new RejectPropertyRemovalValidatorProvider(),
        transaction).Execute(
            new MixedPropertyRemovalTestOperation(
                mutation,
                "item",
                "RejectedRemovalFixture"),
            rejectedProject);
    Check(!rejected.Succeeded && Json(rejectedProject) == rejectedBaseline &&
          rejectedEntry.Properties.SequenceEqual(rejectedOrder) &&
          rejectedEntry.Properties.All(property => !property.IsModified),
        "validator rejection rolls modify/create/remove back atomically");

    Console.WriteLine(
        "PASS property-removal mutation, rollback, and repeated history symmetry");
}

static void VerifyPropertyRemovalBoundaryRejections()
{
    ProjectMutationService mutation = new();
    ProjectModelFactory factory = new();

    ProjectModel objectProject = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "ObjectBoundaryFixture",
                ["container"] = new JObject
                {
                    ["member"] = 1
                },
                ["tail"] = 2
            }));
    EntryModel objectEntry =
        Entry(objectProject, "item", "ObjectBoundaryFixture");
    JProperty objectSource =
        objectEntry.SourceEntry!.Property("container")!;
    PropertyModel objectProperty = factory.CreatePropertyModel(
        "item",
        objectSource,
        "container",
        PropertyModelCreationMode.Existing);
    objectEntry.Properties.Insert(1, objectProperty);
    PropertyModel[] objectModelOrder = objectEntry.Properties.ToArray();
    JProperty[] objectSourceOrder =
        objectEntry.SourceEntry.Properties().ToArray();
    string objectBaseline = Json(objectProject);
    ProjectMutationResult? objectRemoval = null;

    CheckThrows<InvalidOperationException>(
        () => objectRemoval = mutation.RemovePropertyByPath(
            objectEntry,
            "container"),
        "object-valued property removal is rejected");
    Check(objectRemoval == null &&
          ReferenceEquals(
              objectEntry.SourceEntry.Property("container"),
              objectSource) &&
          ReferenceEquals(
              objectEntry.Properties.Single(property =>
                  property.EffectivePropertyPath == "container"),
              objectProperty) &&
          objectEntry.Properties.SequenceEqual(objectModelOrder) &&
          objectEntry.SourceEntry.Properties().SequenceEqual(objectSourceOrder) &&
          !objectProperty.IsModified &&
          Json(objectProject) == objectBaseline,
        "object rejection occurs before mutation or rollback recording");

    ProjectModel ambiguousProject = CreateProject(
        Sheet("item", ScalarEntry("AmbiguousRemovalFixture", 1)));
    EntryModel ambiguousEntry =
        Entry(ambiguousProject, "item", "AmbiguousRemovalFixture");
    JProperty ambiguousSource =
        ambiguousEntry.SourceEntry!.Property("value")!;
    PropertyModel ambiguousDuplicate = factory.CreatePropertyModel(
        "item",
        ambiguousSource,
        "value",
        PropertyModelCreationMode.Existing);
    ambiguousEntry.Properties.Add(ambiguousDuplicate);
    PropertyModel[] ambiguousOrder = ambiguousEntry.Properties.ToArray();
    string ambiguousBaseline = Json(ambiguousProject);

    CheckThrows<InvalidOperationException>(
        () => mutation.RemovePropertyByPath(ambiguousEntry, "value"),
        "ambiguous property removal is rejected");
    Check(ambiguousEntry.Properties.SequenceEqual(ambiguousOrder) &&
          ReferenceEquals(
              ambiguousEntry.SourceEntry.Property("value"),
              ambiguousSource) &&
          Json(ambiguousProject) == ambiguousBaseline,
        "ambiguous rejection leaves source and models unchanged");

    ProjectModel disconnectedProject = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "DisconnectedRemovalFixture",
                ["a"] = 1,
                ["b"] = 2
            }));
    EntryModel disconnectedEntry =
        Entry(disconnectedProject, "item", "DisconnectedRemovalFixture");
    PropertyModel disconnectedProperty = disconnectedEntry.Properties.Single(
        property => property.EffectivePropertyPath == "a");
    disconnectedProperty.SourceProperty =
        disconnectedEntry.SourceEntry!.Property("b");
    PropertyModel[] disconnectedOrder = disconnectedEntry.Properties.ToArray();
    JProperty[] disconnectedSourceOrder =
        disconnectedEntry.SourceEntry.Properties().ToArray();
    string disconnectedBaseline = Json(disconnectedProject);

    CheckThrows<InvalidOperationException>(
        () => mutation.RemovePropertyByPath(disconnectedEntry, "a"),
        "disconnected model/source removal is rejected");
    Check(disconnectedEntry.Properties.SequenceEqual(disconnectedOrder) &&
          disconnectedEntry.SourceEntry.Properties()
              .SequenceEqual(disconnectedSourceOrder) &&
          Json(disconnectedProject) == disconnectedBaseline,
        "disconnected rejection occurs before detachment");

    Console.WriteLine(
        "PASS property-removal object, ambiguous, and disconnected boundaries");
}

static void VerifyPropertyRemovalAdvancedTransactions()
{
    ProjectMutationService mutation = new();
    ProjectOperationTransactionService transaction = new();
    RejectPropertyRemovalValidatorProvider rejectingValidator = new();

    ProjectModel samePropertyProject = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "SamePropertyFixture",
                ["before"] = 1,
                ["target"] = 2,
                ["after"] = 3
            }));
    EntryModel samePropertyEntry =
        Entry(samePropertyProject, "item", "SamePropertyFixture");
    PropertyModel sameProperty = samePropertyEntry.Properties.Single(
        property => property.EffectivePropertyPath == "target");
    JProperty sameSource = sameProperty.SourceProperty!;
    PropertyModel[] sameModelOrder = samePropertyEntry.Properties.ToArray();
    JProperty[] sameSourceOrder =
        samePropertyEntry.SourceEntry!.Properties().ToArray();
    string sameBaseline = Json(samePropertyProject);

    ProjectOperationResult sameRejected = new ProjectOperationService(
        rejectingValidator,
        transaction).Execute(
            new TestMutationOperation(
                "Modify and remove same property",
                project =>
                {
                    EntryModel entry = Entry(
                        project,
                        "item",
                        "SamePropertyFixture");
                    ProjectMutationResult result =
                        mutation.EnsurePropertyByPath(
                            entry,
                            "target",
                            new JValue(20));
                    result.Merge(mutation.RemovePropertyByPath(
                        entry,
                        "target"));
                    return result;
                }),
            samePropertyProject);
    Check(!sameRejected.Succeeded &&
          Json(samePropertyProject) == sameBaseline &&
          samePropertyEntry.Properties.SequenceEqual(sameModelOrder) &&
          samePropertyEntry.SourceEntry.Properties().SequenceEqual(sameSourceOrder) &&
          ReferenceEquals(
              samePropertyEntry.Properties.Single(property =>
                  property.EffectivePropertyPath == "target"),
              sameProperty) &&
          ReferenceEquals(
              samePropertyEntry.SourceEntry.Property("target"),
              sameSource) &&
          sameSource.Value.Value<int>() == 2 &&
          !sameProperty.IsModified,
        "same-property modify/remove validation failure restores exact baseline");

    ProjectMutationResult sameSuccessful = mutation.EnsurePropertyByPath(
        samePropertyEntry,
        "target",
        new JValue(20));
    sameSuccessful.Merge(mutation.RemovePropertyByPath(
        samePropertyEntry,
        "target"));
    string sameRemoved = Json(samePropertyProject);
    EditHistoryService sameHistory = new();
    sameHistory.Record(new ProjectOperationHistoryAction(
        "Modify and remove same property",
        sameSuccessful,
        transaction));
    Check(sameHistory.Undo() &&
          Json(samePropertyProject) == sameBaseline &&
          ReferenceEquals(
              samePropertyEntry.SourceEntry.Property("target"),
              sameSource) &&
          !sameProperty.IsModified,
        "same-property modify/remove Undo restores exact baseline");
    Check(sameHistory.Redo() && Json(samePropertyProject) == sameRemoved &&
          sameSource.Parent == null,
        "same-property modify/remove Redo restores exact removal");

    ProjectModel createdSequenceProject = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "CreatedSequenceFixture",
                ["before"] = 1
            }));
    EntryModel createdSequenceEntry =
        Entry(createdSequenceProject, "item", "CreatedSequenceFixture");
    string createdSequenceBaseline = Json(createdSequenceProject);
    PropertyModel[] createdSequenceOrder =
        createdSequenceEntry.Properties.ToArray();

    ProjectOperationResult createdRejected = new ProjectOperationService(
        rejectingValidator,
        transaction).Execute(
            new TestMutationOperation(
                "Create modify and remove same property",
                project =>
                {
                    EntryModel entry = Entry(
                        project,
                        "item",
                        "CreatedSequenceFixture");
                    ProjectMutationResult result =
                        mutation.EnsurePropertyByPath(
                            entry,
                            "temporary",
                            new JValue(1));
                    result.Merge(mutation.EnsurePropertyByPath(
                        entry,
                        "temporary",
                        new JValue(2)));
                    result.Merge(mutation.RemovePropertyByPath(
                        entry,
                        "temporary"));
                    return result;
                }),
            createdSequenceProject);
    Check(!createdRejected.Succeeded &&
          Json(createdSequenceProject) == createdSequenceBaseline &&
          createdSequenceEntry.Properties.SequenceEqual(createdSequenceOrder) &&
          createdSequenceEntry.SourceEntry!.Property("temporary") == null &&
          createdRejected.MutationResult.RemovedPropertyRollbackRecords.Count == 0,
        "create/modify/remove validation failure returns to absent baseline");

    ProjectMutationResult createdSuccessful = mutation.EnsurePropertyByPath(
        createdSequenceEntry,
        "temporary",
        new JValue(1));
    PropertyModel transientProperty =
        createdSuccessful.CreatedProperties.Single();
    JProperty transientSource = transientProperty.SourceProperty!;
    createdSuccessful.Merge(mutation.EnsurePropertyByPath(
        createdSequenceEntry,
        "temporary",
        new JValue(2)));
    createdSuccessful.Merge(mutation.RemovePropertyByPath(
        createdSequenceEntry,
        "temporary"));
    EditHistoryService createdSequenceHistory = new();
    createdSequenceHistory.Record(new ProjectOperationHistoryAction(
        "Create modify and remove same property",
        createdSuccessful,
        transaction));
    for (int cycle = 0; cycle < 2; cycle++)
    {
        Check(createdSequenceHistory.Undo() &&
              Json(createdSequenceProject) == createdSequenceBaseline &&
              !createdSequenceEntry.Properties.Contains(transientProperty) &&
              transientSource.Parent == null,
            "create/modify/remove Undo preserves absent baseline");
        Check(createdSequenceHistory.Redo() &&
              Json(createdSequenceProject) == createdSequenceBaseline &&
              !createdSequenceEntry.Properties.Contains(transientProperty) &&
              transientSource.Parent == null,
            "create/modify/remove Redo deterministically ends absent");
    }

    VerifyMultiplePropertyRemoval(
        mutation,
        transaction,
        rejectingValidator);
    VerifyPropertyRemovalEndpointOrdering(
        mutation,
        transaction,
        "A");
    VerifyPropertyRemovalEndpointOrdering(
        mutation,
        transaction,
        "C");

    Console.WriteLine(
        "PASS advanced property-removal atomicity and ordering coverage");
}

static void VerifyMultiplePropertyRemoval(
    ProjectMutationService mutation,
    ProjectOperationTransactionService transaction,
    IOperationValidatorProvider rejectingValidator)
{
    ProjectModel project = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "MultipleRemovalFixture",
                ["A"] = 1,
                ["B"] = 2,
                ["C"] = 3,
                ["D"] = 4
            }));
    EntryModel entry = Entry(project, "item", "MultipleRemovalFixture");
    string baseline = Json(project);
    PropertyModel[] modelOrder = entry.Properties.ToArray();
    JProperty[] sourceOrder = entry.SourceEntry!.Properties().ToArray();
    PropertyModel propertyB = entry.Properties.Single(property =>
        property.EffectivePropertyPath == "B");
    PropertyModel propertyC = entry.Properties.Single(property =>
        property.EffectivePropertyPath == "C");

    ProjectMutationResult removal = mutation.RemovePropertyByPath(entry, "B");
    removal.Merge(mutation.RemovePropertyByPath(entry, "C"));
    string removed = Json(project);
    EditHistoryService history = new();
    history.Record(new ProjectOperationHistoryAction(
        "Remove multiple properties",
        removal,
        transaction));
    Check(history.UndoDescription == "Remove multiple properties",
        "multiple removals produce one history action");
    for (int cycle = 0; cycle < 3; cycle++)
    {
        Check(history.Undo() && Json(project) == baseline &&
              entry.Properties.SequenceEqual(modelOrder) &&
              entry.SourceEntry.Properties().SequenceEqual(sourceOrder) &&
              ReferenceEquals(
                  entry.Properties.Single(property =>
                      property.EffectivePropertyPath == "B"),
                  propertyB) &&
              ReferenceEquals(
                  entry.Properties.Single(property =>
                      property.EffectivePropertyPath == "C"),
                  propertyC),
            "multiple-removal Undo restores exact identities and ordering");
        Check(history.Redo() && Json(project) == removed &&
              !entry.Properties.Contains(propertyB) &&
              !entry.Properties.Contains(propertyC),
            "multiple-removal Redo handles index drift deterministically");
    }

    ProjectModel rejectedProject = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "MultipleRejectedFixture",
                ["A"] = 1,
                ["B"] = 2,
                ["C"] = 3,
                ["D"] = 4
            }));
    EntryModel rejectedEntry =
        Entry(rejectedProject, "item", "MultipleRejectedFixture");
    string rejectedBaseline = Json(rejectedProject);
    PropertyModel[] rejectedModelOrder = rejectedEntry.Properties.ToArray();
    JProperty[] rejectedSourceOrder =
        rejectedEntry.SourceEntry!.Properties().ToArray();
    ProjectOperationResult rejected = new ProjectOperationService(
        rejectingValidator,
        transaction).Execute(
            new TestMutationOperation(
                "Remove multiple properties",
                currentProject =>
                {
                    EntryModel currentEntry = Entry(
                        currentProject,
                        "item",
                        "MultipleRejectedFixture");
                    ProjectMutationResult result = mutation.RemovePropertyByPath(
                        currentEntry,
                        "B");
                    result.Merge(mutation.RemovePropertyByPath(
                        currentEntry,
                        "C"));
                    return result;
                }),
            rejectedProject);
    Check(!rejected.Succeeded &&
          Json(rejectedProject) == rejectedBaseline &&
          rejectedEntry.Properties.SequenceEqual(rejectedModelOrder) &&
          rejectedEntry.SourceEntry.Properties().SequenceEqual(rejectedSourceOrder),
        "multiple-removal validation failure restores exact baseline");
}

static void VerifyPropertyRemovalEndpointOrdering(
    ProjectMutationService mutation,
    ProjectOperationTransactionService transaction,
    string removedPath)
{
    ProjectModel project = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["A"] = 1,
                ["B"] = 2,
                ["C"] = 3
            }));
    EntryModel entry = project.Sheets.Single().Entries.Single();
    PropertyModel removedProperty = entry.Properties.Single(property =>
        property.EffectivePropertyPath == removedPath);
    JProperty removedSource = removedProperty.SourceProperty!;
    PropertyModel[] modelOrder = entry.Properties.ToArray();
    JProperty[] sourceOrder = entry.SourceEntry!.Properties().ToArray();
    ProjectMutationResult result = mutation.RemovePropertyByPath(
        entry,
        removedPath);
    EditHistoryService history = new();
    history.Record(new ProjectOperationHistoryAction(
        $"Remove endpoint {removedPath}",
        result,
        transaction));
    Check(history.Undo() &&
          entry.Properties.SequenceEqual(modelOrder) &&
          entry.SourceEntry.Properties().SequenceEqual(sourceOrder) &&
          ReferenceEquals(
              entry.Properties.Single(property =>
                  property.EffectivePropertyPath == removedPath),
              removedProperty) &&
          ReferenceEquals(
              entry.SourceEntry.Property(removedPath),
              removedSource),
        $"{removedPath} endpoint Undo restores exact model/source order");
}

static void VerifyLecternKnowledgeGain()
{
    ProjectModel project = CreateProject(
        Sheet("constant", ScalarEntry("GainOnLecternRest", 30)));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ApplyPreset(executor, service, project,
        ProgressionType.LecternKnowledgeGain, "Increased");
    CheckNumber(project, "constant", "GainOnLecternRest", "value", 60);
    ApplyPreset(executor, service, project,
        ProgressionType.LecternKnowledgeGain, "High");
    CheckNumber(project, "constant", "GainOnLecternRest", "value", 90);
    ProjectOperationResult veryHigh = ApplyPreset(executor, service, project,
        ProgressionType.LecternKnowledgeGain, "VeryHigh");
    CheckNumber(project, "constant", "GainOnLecternRest", "value", 150);
    string veryHighJson = Json(project);

    ProjectOperationResult same = ApplyPreset(executor, service, project,
        ProgressionType.LecternKnowledgeGain, "VeryHigh");
    Check(!same.MutationResult.WasModified && Json(project) == veryHighJson,
        "Lectern same-preset Apply is idempotent and does not compound");

    ModProfileModel lecternProfile = new ModProfileService().CreateProfile(
        project, "Lectern replay");
    ProjectModel lecternTarget = CreateProject(
        Sheet("constant", ScalarEntry("GainOnLecternRest", 30)));
    ModificationSnapshotImportResultModel lecternReplay =
        new ModProfileWorkflowService().ApplyProfile(lecternTarget, lecternProfile);
    Check(!lecternReplay.HasFailures,
        "Lectern profile replay succeeds");
    CheckNumber(lecternTarget, "constant", "GainOnLecternRest", "value", 150);

    ProjectOperationResult vanilla = ApplyPreset(executor, service, project,
        ProgressionType.LecternKnowledgeGain, "Vanilla");
    CheckNumber(project, "constant", "GainOnLecternRest", "value", 30);
    CheckTokenNumber(
        project.GameplayOperationStates.Single().BaselineArray[0]!["value"]!,
        30,
        "Lectern exact captured baseline retained");
    VerifyUndoRedo("Lectern Vanilla", vanilla, project, veryHighJson, Json(project));
    VerifySnapshotStateRoundTrip(project, ProgressionType.LecternKnowledgeGain, 1);
    VerifyProfileStateRoundTrip(project, ProgressionType.LecternKnowledgeGain, 1);

    foreach ((object value, string label) invalid in new[]
             {
                 ((object)"invalid", "wrong token"),
                 ((object)0, "zero"),
                 ((object)(-1), "negative"),
                 ((object)double.PositiveInfinity, "nonfinite")
             })
    {
        ProjectModel malformed = CreateProject(
            Sheet("constant", ScalarEntry("GainOnLecternRest", invalid.value)));
        Check(!ExecutePreset(
                malformed,
                ProgressionType.LecternKnowledgeGain,
                "High").Succeeded,
            $"Lectern {invalid.label} baseline rejected");
        Check(malformed.GameplayOperationStates.Count == 0,
            $"Lectern {invalid.label} failure records no state");
    }

    ProjectModel missing = CreateProject(Sheet("constant"));
    Check(!ExecutePreset(
            missing,
            ProgressionType.LecternKnowledgeGain,
            "Increased").Succeeded,
        "Lectern missing target rejected");
    _ = veryHigh;
    Console.WriteLine("PASS Lectern Knowledge Gain captured-baseline scaling and safety");
}

static void VerifyPositiveRandomTraits()
{
    ProjectModel project = CreateTraitProject(0.2, 0.3, 0.1);
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    string vanillaJson = Json(project);
    ProjectOperationResult positive = ApplyPreset(
        executor, service, project,
        ProgressionType.PositiveRandomTraits, "PositiveOnly");
    CheckTraitValues(project, 0, 1, 0);
    Check(positive.MutationResult.UpdatedProperties.Count == 3,
        "Positive Random Traits updates all three probability bands atomically");
    string positiveJson = Json(project);

    ProjectOperationResult same = ApplyPreset(
        executor, service, project,
        ProgressionType.PositiveRandomTraits, "PositiveOnly");
    Check(!same.MutationResult.WasModified && Json(project) == positiveJson,
        "Positive Random Traits reapply is idempotent");

    ModProfileModel traitProfile = new ModProfileService().CreateProfile(
        project, "Positive traits replay");
    ProjectModel traitTarget = CreateTraitProject(0.2, 0.3, 0.1);
    ModificationSnapshotImportResultModel traitReplay =
        new ModProfileWorkflowService().ApplyProfile(traitTarget, traitProfile);
    Check(!traitReplay.HasFailures,
        "Positive Random Traits profile replay succeeds");
    CheckTraitValues(traitTarget, 0, 1, 0);

    ProjectOperationResult vanilla = ApplyPreset(
        executor, service, project,
        ProgressionType.PositiveRandomTraits, "Vanilla");
    CheckTraitValues(project, 0.2, 0.3, 0.1);
    VerifyUndoRedo(
        "Positive Random Traits Vanilla", vanilla, project, positiveJson, Json(project));
    Check(Json(project) == vanillaJson || project.GameplayOperationStates.Count == 1,
        "Positive Random Traits restores the captured values");
    VerifySnapshotStateRoundTrip(project, ProgressionType.PositiveRandomTraits, 3);
    VerifyProfileStateRoundTrip(project, ProgressionType.PositiveRandomTraits, 3);

    foreach ((object mixed, object two, object one, string label) invalid in new[]
             {
                 ((object)"invalid", (object)0.3, (object)0.1, "nonnumeric"),
                 ((object)double.NaN, (object)0.3, (object)0.1, "nonfinite"),
                 ((object)(-0.1), (object)0.3, (object)0.1, "negative"),
                 ((object)0.2, (object)1.1, (object)0.1, "greater than one"),
                 ((object)0.5, (object)0.4, (object)0.2, "sum greater than one")
             })
    {
        ProjectModel malformed = CreateTraitProject(
            invalid.mixed, invalid.two, invalid.one);
        Check(!ExecutePreset(
                malformed,
                ProgressionType.PositiveRandomTraits,
                "PositiveOnly").Succeeded,
            $"Positive Random Traits {invalid.label} baseline rejected");
        Check(malformed.GameplayOperationStates.Count == 0,
            $"Positive Random Traits {invalid.label} failure is atomic");
    }

    foreach (string missing in new[]
             {
                 "RandomTrait1Positive1Negative",
                 "RandomTrait2Positive",
                 "RandomTrait1Positive"
             })
    {
        ProjectModel malformed = CreateTraitProject(0.2, 0.3, 0.1, missing);
        Check(!ExecutePreset(
                malformed,
                ProgressionType.PositiveRandomTraits,
                "PositiveOnly").Succeeded,
            $"Positive Random Traits missing {missing} rejected");
        Check(malformed.GameplayOperationStates.Count == 0,
            $"Positive Random Traits missing {missing} records no partial state");
    }

    ProjectModel duplicate = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("RandomTrait1Positive1Negative", 0.2),
            ScalarEntry("RandomTrait2Positive", 0.3),
            ScalarEntry("RandomTrait2Positive", 0.4),
            ScalarEntry("RandomTrait1Positive", 0.1)));
    Check(!ExecutePreset(
            duplicate,
            ProgressionType.PositiveRandomTraits,
            "PositiveOnly").Succeeded,
        "Positive Random Traits duplicate target rejected");
    Console.WriteLine("PASS Positive Random Traits exact bands, restoration, and safety");
}

static void VerifyRandomTraitExclusions()
{
    ProjectModel project = CreateRandomTraitExclusionProject();
    ProjectMutationService mutation = new();
    GameplayOperationStateService stateService = new(mutation);
    RandomTraitExclusionsService service = new(mutation, stateService);
    ProjectOperationService executor = new();

    IReadOnlyList<RandomTraitExclusionCandidate> discovered = service.Discover(project);
    Check(discovered.Count == 4 &&
          discovered.Count(candidate => candidate.Personality == RandomTraitPersonality.Positive) == 2 &&
          discovered.Count(candidate => candidate.Personality == RandomTraitPersonality.Negative) == 2 &&
          discovered.All(candidate => candidate.Id != "HiddenTrait" &&
                                      candidate.Id != "AcquiredTrait" &&
                                      candidate.Id != "RecruitmentWithoutPersonality"),
        "Random Trait Exclusions discovers and groups only standard candidates dynamically");
    Check(discovered.Single(candidate => candidate.Id == "PositiveAbsent").IsAllowed &&
          !discovered.Single(candidate => candidate.Id == "NegativeDisabled").IsAllowed,
        "Random Trait Exclusions reflects absent and pre-disabled baseline eligibility");
    string openingJson = Json(project);
    RandomTraitExclusionsDialogViewModel dialogViewModel = new(
        project,
        service,
        new LocalizationService());
    Check(dialogViewModel.PositiveTraits.Count == 2 &&
          dialogViewModel.NegativeTraits.Count == 2 &&
          dialogViewModel.SearchText == string.Empty &&
          dialogViewModel.PositiveTraitsView.Cast<object>().Count() == 2 &&
          dialogViewModel.NegativeTraitsView.Cast<object>().Count() == 2 &&
          !dialogViewModel.NegativeTraits.Single(item =>
              item.Id == "NegativeDisabled").IsAllowed &&
          Json(project) == openingJson &&
          project.GameplayOperationStates.Count == 0 &&
          !dialogViewModel.CanRestorePreviousValues &&
          !project.Sheets.SelectMany(sheet => sheet.Entries)
              .SelectMany(entry => entry.Properties).Any(property => property.IsModified),
        "Random Trait Exclusions full ViewModel initialization uses realistic data without mutation");

    ProjectOperationResult mixed = executor.Execute(
        new RandomTraitExclusionsOperation(
            service,
            new[] { "PositiveTrue", "PositiveAbsent", "NegativeDisabled" }),
        project);
    Check(mixed.Succeeded && mixed.MutationResult.CreatedProperties.Count == 1 &&
          mixed.MutationResult.UpdatedProperties.Count == 1 &&
          mixed.MutationResult.GameplayOperationStateRollbackRecords.Count == 1,
        "Random Trait Exclusions atomically combines creation, update, and state ownership");
    Check(project.GameplayOperationStates.Single().BaselineArray.OfType<JObject>().All(record =>
              record["group"]?.Type == JTokenType.String &&
              record["generation"] == null &&
              record.Value<string>("group") is "Starting" or "Recruitment"),
        "Random Trait Exclusions state fingerprints separator groups instead of numeric gen");
    dialogViewModel.RefreshFromProject(project, service);
    Check(dialogViewModel.CanRestorePreviousValues,
        "Random Trait Exclusions enables previous-value restoration after capture");
    Check(Entry(project, "trait", "NegativeAbsent").SourceEntry!["done"]!.Value<bool>() == false &&
          Entry(project, "trait", "NegativeDisabled").SourceEntry!["done"]!.Value<bool>(),
        "unchecked traits are disabled and checked pre-disabled traits are explicitly enabled");
    string mixedJson = Json(project);
    ProjectOperationResult same = executor.Execute(
        new RandomTraitExclusionsOperation(
            service,
            new[] { "PositiveTrue", "PositiveAbsent", "NegativeDisabled" }),
        project);
    Check(same.Succeeded && !same.MutationResult.WasModified && Json(project) == mixedJson,
        "Random Trait Exclusions same-state apply is a successful no-op");

    RandomTraitExclusionsOperationValidator exclusionsValidator = new();
    Check(exclusionsValidator.Validate(
            new RandomTraitExclusionsOperation(
                service,
                new[] { "NegativeDisabled", "PositiveAbsent", "PositiveTrue" }),
            project,
            mixed.MutationResult).IsValid,
        "validator normalizes requested allowed-trait ordering");
    Check(!exclusionsValidator.Validate(
            new RandomTraitExclusionsOperation(
                service,
                new[]
                {
                    "PositiveTrue", "PositiveAbsent", "NegativeDisabled", "NegativeAbsent"
                }),
            project,
            mixed.MutationResult).IsValid,
        "validator rejects a recorded selection missing a requested allowed trait");
    Check(!exclusionsValidator.Validate(
            new RandomTraitExclusionsOperation(
                service,
                new[] { "PositiveTrue", "PositiveAbsent" }),
            project,
            mixed.MutationResult).IsValid,
        "validator rejects an extra recorded allowed trait");
    Check(!exclusionsValidator.Validate(
            new RandomTraitExclusionsOperation(
                service,
                new[] { "PositiveTrue", "PositiveAbsent", "NegativeAbsent" }),
            project,
            mixed.MutationResult).IsValid,
        "validator rejects a wrong recorded allowed trait");
    Check(!exclusionsValidator.Validate(
            new RandomTraitExclusionsOperation(
                service,
                new[] { "PositiveTrue", "PositiveTrue", "NegativeDisabled" }),
            project,
            mixed.MutationResult).IsValid,
        "validator rejects duplicate requested allowed traits");

    ProjectModel mismatchRollback = CreateRandomTraitExclusionProject();
    string mismatchRollbackBaseline = Json(mismatchRollback);
    ProjectMutationService mismatchMutation = new();
    ProjectOperationResult mismatchRejected = new ProjectOperationService().Execute(
        new RandomTraitExclusionsOperation(
            new RandomTraitExclusionsService(
                mismatchMutation,
                new GameplayOperationStateService(mismatchMutation)),
            new ValidatorMismatchCollection(
                new[] { "PositiveTrue", "PositiveAbsent", "NegativeDisabled" },
                new[] { "PositiveTrue", "PositiveAbsent", "NegativeAbsent" })),
        mismatchRollback);
    Check(!mismatchRejected.Succeeded &&
          Json(mismatchRollback) == mismatchRollbackBaseline &&
          mismatchRollback.GameplayOperationStates.Count == 0,
        "requested/result validator mismatch triggers exact transaction rollback");

    ProjectOperationResult defaults = executor.Execute(
        new RandomTraitExclusionsOperation(
            service,
            new[] { "PositiveTrue", "PositiveAbsent", "NegativeAbsent" }),
        project);
    Check(defaults.Succeeded && defaults.MutationResult.RemovedProperties.Count == 1 &&
          Entry(project, "trait", "NegativeAbsent").SourceEntry!.Property("done") == null &&
          Entry(project, "trait", "NegativeDisabled").SourceEntry!["done"]!.Value<bool>() == false,
        "Restore Previous Values reproduces false and absent baselines exactly");
    Check(project.GameplayOperationStates.Single().OperationType ==
              ProgressionType.RandomTraitExclusions &&
          project.IsGameplayOperationStateModified,
        "removal-based restoration retains operation-state dirty truth");
    ModificationSnapshotModel restoredSnapshot =
        new ModificationSnapshotService().CreateSnapshot(project);
    Check(restoredSnapshot.GameplayOperationStates.Any(state =>
              state.OperationType == ProgressionType.RandomTraitExclusions),
        "Review/profile snapshot reporting retains removal-based operation ownership");
    IReadOnlyList<ChangeSummaryItemModel> restoredSummary =
        new ChangeSummaryService().BuildItems(
            project,
            restoredSnapshot,
            new LocalizationService());
    Check(new EffectiveChangeCountService().Calculate(project) == 0 &&
          restoredSummary.All(item =>
              item.SettingName != "Random Trait Exclusions"),
        "Review Changes omits RTE when exact captured previous values are restored");

    ProjectModel unrelatedDirty = CreateRandomTraitExclusionAndPositiveTraitProject();
    ProjectMutationService unrelatedMutation = new();
    GameplayOperationStateService unrelatedStateService = new(unrelatedMutation);
    RandomTraitExclusionsService unrelatedExclusions = new(
        unrelatedMutation,
        unrelatedStateService);
    Check(new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(
                unrelatedExclusions,
                new[] { "PositiveTrue", "PositiveAbsent", "NegativeAbsent" }),
            unrelatedDirty).Succeeded,
        "cross-operation fixture records default exclusions state");
    unrelatedStateService.AcceptCurrentStates(unrelatedDirty);
    unrelatedDirty.IsGameplayOperationStateModified = false;
    Check(ExecutePreset(
            unrelatedDirty,
            ProgressionType.PositiveRandomTraits,
            "PositiveOnly").Succeeded,
        "cross-operation fixture changes an unrelated gameplay operation");
    IReadOnlyList<ChangeSummaryItemModel> unrelatedSummary =
        new ChangeSummaryService().BuildItems(
            unrelatedDirty,
            new ModificationSnapshotService().CreateSnapshot(unrelatedDirty),
            new LocalizationService());
    Check(!unrelatedStateService.IsStateModified(
              unrelatedDirty,
              ProgressionType.RandomTraitExclusions) &&
          unrelatedSummary.All(item =>
              item.SettingName != "Random Trait Exclusions"),
        "unrelated gameplay dirty state does not create an exclusions fallback row or count");

    ProjectModel removalOnly = CreateRandomTraitExclusionProject();
    ProjectMutationService removalMutation = new();
    GameplayOperationStateService removalStateService = new(removalMutation);
    RandomTraitExclusionsService removalService = new(
        removalMutation,
        removalStateService);
    Check(new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(
                removalService,
                new[] { "PositiveTrue", "NegativeAbsent" }),
            removalOnly).Succeeded,
        "removal-only fixture creates one absent-baseline exclusion");
    AcceptProjectBaselines(removalOnly, removalStateService);
    Check(new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(
                removalService,
                new[] { "PositiveTrue", "PositiveAbsent", "NegativeAbsent" }),
            removalOnly).Succeeded,
        "removal-only fixture restores the persisted absent leaf");
    IReadOnlyList<ChangeSummaryItemModel> removalOnlySummary =
        new ChangeSummaryService().BuildItems(
            removalOnly,
            new ModificationSnapshotService().CreateSnapshot(removalOnly),
            new LocalizationService());
    Check(removalStateService.IsStateModified(
              removalOnly,
              ProgressionType.RandomTraitExclusions) &&
          new EffectiveChangeCountService().Calculate(removalOnly) == 0 &&
          removalOnlySummary.All(item =>
              item.SettingName != "Random Trait Exclusions"),
        "removal-only return to captured RTE baseline contributes no fallback row or count");

    ProjectModel attachedChange = CreateRandomTraitExclusionProject();
    ProjectMutationService attachedMutation = new();
    GameplayOperationStateService attachedStateService = new(attachedMutation);
    RandomTraitExclusionsService attachedService = new(
        attachedMutation,
        attachedStateService);
    Check(new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(
                attachedService,
                new[]
                {
                    "PositiveTrue", "PositiveAbsent", "NegativeDisabled", "NegativeAbsent"
                }),
            attachedChange).Succeeded,
        "attached exclusions fixture updates an existing done leaf");
    IReadOnlyList<ChangeSummaryItemModel> attachedSummary =
        new ChangeSummaryService().BuildItems(
            attachedChange,
            new ModificationSnapshotService().CreateSnapshot(attachedChange),
            new LocalizationService());
    Check(attachedSummary.Count(item =>
              item.CategoryName == "trait" && item.PropertyName == "done") == 1 &&
          attachedSummary.All(item =>
              item.SettingName != "Random Trait Exclusions"),
        "attached done changes suppress the synthetic exclusions row without double-counting");

    ProjectModel resetWithUnrelated = CreateRandomTraitExclusionAndPositiveTraitProject();
    ProjectMutationService resetMutation = new();
    GameplayOperationStateService resetStateService = new(resetMutation);
    RandomTraitExclusionsService resetService = new(resetMutation, resetStateService);
    Check(new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(
                resetService,
                new[] { "PositiveTrue", "PositiveAbsent", "NegativeAbsent" }),
            resetWithUnrelated).Succeeded,
        "reset cross-operation fixture records persisted defaults");
    AcceptProjectBaselines(resetWithUnrelated, resetStateService);
    Check(new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(
                resetService,
                new[]
                {
                    "PositiveTrue", "PositiveAbsent", "NegativeDisabled", "NegativeAbsent"
                }),
            resetWithUnrelated).Succeeded &&
          new ProjectOperationService().Execute(
            new RandomTraitExclusionsOperation(
                resetService,
                new[] { "PositiveTrue", "PositiveAbsent", "NegativeAbsent" }),
            resetWithUnrelated).Succeeded,
        "exclusions can change and reset to their persisted state");
    Check(ExecutePreset(
            resetWithUnrelated,
            ProgressionType.PositiveRandomTraits,
            "PositiveOnly").Succeeded,
        "unrelated operation remains dirty after exclusions reset");
    IReadOnlyList<ChangeSummaryItemModel> resetSummary =
        new ChangeSummaryService().BuildItems(
            resetWithUnrelated,
            new ModificationSnapshotService().CreateSnapshot(resetWithUnrelated),
            new LocalizationService());
    Check(!resetStateService.IsStateModified(
              resetWithUnrelated,
              ProgressionType.RandomTraitExclusions) &&
          resetWithUnrelated.IsGameplayOperationStateModified &&
          resetSummary.All(item => item.SettingName != "Random Trait Exclusions"),
        "exclusions fallback disappears after reset while unrelated state remains dirty");

    string defaultsJson = Json(project);
    VerifyUndoRedo("Random Trait Exclusions defaults", defaults, project, mixedJson, defaultsJson);

    ProjectModel profileSource = CreateRandomTraitExclusionProject();
    ProjectMutationService profileMutation = new();
    RandomTraitExclusionsService profileService = new(
        profileMutation,
        new GameplayOperationStateService(profileMutation));
    ProjectOperationResult profileApply = new ProjectOperationService().Execute(
        new RandomTraitExclusionsOperation(
            profileService,
            new[] { "PositiveTrue", "NegativeDisabled", "NegativeAbsent" }),
        profileSource);
    Check(profileApply.Succeeded &&
          Entry(profileSource, "trait", "PositiveAbsent").SourceEntry!["done"]!.Value<bool>() == false,
        "absent-baseline exclusion is created before profile capture");
    ModProfileModel profile = new ModProfileService().CreateProfile(
        profileSource,
        "Random trait exclusions");
    ProjectModel profileTarget = CreateRandomTraitExclusionProject();
    ModificationSnapshotImportResultModel replay =
        new ModProfileWorkflowService().ApplyProfile(profileTarget, profile);
    Check(!replay.HasFailures &&
          Entry(profileTarget, "trait", "PositiveAbsent").SourceEntry!["done"]!.Value<bool>() == false &&
          profileTarget.GameplayOperationStates.Single().OperationType ==
              ProgressionType.RandomTraitExclusions,
        "Random Trait Exclusions profile replay creates absent-baseline leaves through operation state");
    VerifySnapshotStateRoundTrip(
        profileSource,
        ProgressionType.RandomTraitExclusions,
        4);
    VerifyProfileStateRoundTrip(
        profileSource,
        ProgressionType.RandomTraitExclusions,
        4);
    ProjectModel stateReloadTarget = CreateProject(
        ((JArray)profileSource.RootDocument["sheets"]!).OfType<JObject>()
            .Select(sheet => (JObject)sheet.DeepClone()).ToArray());
    VerifyGameplayStateFileRoundTrip(
        profileSource,
        stateReloadTarget,
        ProgressionType.RandomTraitExclusions);

    ProjectModel updateProfileSource = CreateRandomTraitExclusionProject();
    ProjectMutationService updateProfileMutation = new();
    RandomTraitExclusionsService updateProfileService = new(
        updateProfileMutation,
        new GameplayOperationStateService(updateProfileMutation));
    Check(new ProjectOperationService().Execute(
              new RandomTraitExclusionsOperation(
                  updateProfileService,
                  new[] { "PositiveTrue", "PositiveAbsent" }),
              updateProfileSource).Succeeded,
        "Update Profile fixture applies a revised exclusion selection");
    ModProfileModel updatedProfile = new ModProfileService().CreateUpdatedProfile(
        updateProfileSource,
        profile);
    GameplayOperationStateModel updatedProfileState =
        updatedProfile.Snapshot.GameplayOperationStates.Single(state =>
            state.OperationType == ProgressionType.RandomTraitExclusions);
    Check(RandomTraitExclusionsService.ReadAllowedIds(updatedProfileState)
              .SetEquals(new[] { "PositiveTrue", "PositiveAbsent" }) &&
          updatedProfile.Metadata.Name == profile.Metadata.Name,
        "Update Profile recaptures current exclusions while preserving profile identity");

    string exclusionsLibraryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"wartales-exclusions-profile-update-{Guid.NewGuid():N}");
    Directory.CreateDirectory(exclusionsLibraryDirectory);
    try
    {
        ModProfileLibraryService exclusionsLibrary = new(
            new ModProfileLibraryPathService(exclusionsLibraryDirectory),
            new ModProfileSerializationService());
        ModProfileWorkflowService exclusionsWorkflow = new();
        ProjectModel managedSource = CreateRandomTraitExclusionProject();
        ProjectMutationService managedMutation = new();
        RandomTraitExclusionsService managedService = new(
            managedMutation,
            new GameplayOperationStateService(managedMutation));
        Check(new ProjectOperationService().Execute(
                new RandomTraitExclusionsOperation(
                    managedService,
                    new[] { "PositiveTrue", "NegativeDisabled", "NegativeAbsent" }),
                managedSource).Succeeded,
            "managed exclusions profile fixture applies its initial selection");
        ModProfileSummaryModel managedSummary = exclusionsLibrary.AddProfile(
            exclusionsWorkflow.CreateProfile(
                managedSource,
                "Managed Random Trait Exclusions"));
        ProfileManagerViewModel exclusionsManager = new(
            exclusionsLibrary,
            new WpfFileDialogService(),
            new WpfMessageDialogService(),
            _ => false)
        {
            CanApplyToCurrentProject = true
        };
        exclusionsManager.SelectedProfile = exclusionsManager.Profiles.Single(candidate =>
            candidate.FilePath == managedSummary.FilePath);

        Check(new ProjectOperationService().Execute(
                new RandomTraitExclusionsOperation(
                    managedService,
                    new[] { "PositiveTrue", "PositiveAbsent" }),
                managedSource).Succeeded,
            "managed exclusions profile fixture applies its revised selection");
        ModProfileModel managedLoaded = exclusionsLibrary.LoadProfile(
            exclusionsManager.SelectedProfile!);
        ModProfileModel managedRebuilt = exclusionsWorkflow.CreateUpdatedProfile(
            managedSource,
            managedLoaded,
            "focused-corrections");
        ModProfileSummaryModel managedUpdated = exclusionsLibrary.UpdateProfile(
            exclusionsManager.SelectedProfile!,
            managedRebuilt,
            reloaded => exclusionsWorkflow.ValidateUpdatedProfileCandidate(
                managedSource,
                managedLoaded,
                reloaded));
        ModProfileModel managedRoundTrip = exclusionsLibrary.LoadProfile(managedUpdated);
        Check(managedUpdated.FilePath == managedSummary.FilePath &&
              managedUpdated.FileName == managedSummary.FileName &&
              exclusionsLibrary.GetProfiles().Count == 1 &&
              RandomTraitExclusionsService.ReadAllowedIds(
                      managedRoundTrip.Snapshot.GameplayOperationStates.Single(state =>
                          state.OperationType == ProgressionType.RandomTraitExclusions))
                  .SetEquals(new[] { "PositiveTrue", "PositiveAbsent" }),
            "Update Profile replaces the selected exclusions profile at the same path without duplication");

        ProjectModel managedReplayTarget = CreateRandomTraitExclusionProject();
        ModificationSnapshotImportResultModel managedReplay =
            exclusionsWorkflow.ApplyProfile(managedReplayTarget, managedRoundTrip);
        GameplayOperationStateModel managedReplayState =
            managedReplayTarget.GameplayOperationStates.Single(state =>
                state.OperationType == ProgressionType.RandomTraitExclusions);
        Check(!managedReplay.HasFailures &&
              RandomTraitExclusionsService.ReadAllowedIds(managedReplayState)
                  .SetEquals(new[] { "PositiveTrue", "PositiveAbsent" }) &&
              Entry(managedReplayTarget, "trait", "PositiveAbsent")
                  .SourceEntry!.Property("done") == null &&
              Entry(managedReplayTarget, "trait", "NegativeAbsent")
                  .SourceEntry!["done"]!.Value<bool>() == false,
            "updated managed exclusions profile deterministically replays its absent-baseline selection");
    }
    finally
    {
        if (Directory.Exists(exclusionsLibraryDirectory))
            Directory.Delete(exclusionsLibraryDirectory, recursive: true);
    }

    ProjectModel rollbackProject = CreateRandomTraitExclusionProject();
    ProjectMutationService rollbackMutation = new();
    RandomTraitExclusionsService rollbackService = new(
        rollbackMutation,
        new GameplayOperationStateService(rollbackMutation));
    Check(new ProjectOperationService().Execute(
        new RandomTraitExclusionsOperation(
            rollbackService,
            new[] { "PositiveAbsent", "NegativeDisabled", "NegativeAbsent" }),
        rollbackProject).Succeeded,
        "mixed rollback fixture establishes operation state");
    string rollbackBaseline = Json(rollbackProject);
    ProjectOperationService rejecting = new(
        new RejectPropertyRemovalValidatorProvider(),
        new ProjectOperationTransactionService());
    ProjectOperationResult rejected = rejecting.Execute(
        new RandomTraitExclusionsOperation(
            rollbackService,
            new[] { "PositiveTrue", "NegativeDisabled" }),
        rollbackProject);
    Check(!rejected.Succeeded && Json(rollbackProject) == rollbackBaseline,
        "forced validation failure rolls back mixed trait mutations exactly");

    ProjectModel duplicate = CreateProject(RandomTraitSheetWithStartingCandidates(
        RandomTraitEntry("Duplicate", 0, null),
        RandomTraitEntry("Duplicate", 1, true)));
    CheckThrows<InvalidOperationException>(() => service.Discover(duplicate),
        "duplicate random trait IDs are rejected");
    ProjectModel wrongDone = CreateProject(RandomTraitSheetWithStartingCandidates(
        RandomTraitEntry("WrongDone", 0, "false")));
    CheckThrows<InvalidOperationException>(() => service.Discover(wrongDone),
        "non-Boolean random trait done values are rejected");
    Check(service.Discover(CreateRandomTraitExclusionProject()).Count == 4,
        "malformed candidate-only data on unsupported and personality-less traits is ignored");

    VerifyRandomTraitSeparatorFailure(
        sheet => RemoveRequiredSeparator(sheet, "Starting"),
        "missing Starting separator is rejected");
    VerifyRandomTraitSeparatorFailure(
        sheet => RemoveRequiredSeparator(sheet, "Hidden"),
        "missing Hidden separator is rejected");
    VerifyRandomTraitSeparatorFailure(
        sheet => RemoveRequiredSeparator(sheet, "Recruitment"),
        "missing Recruitment separator is rejected");
    VerifyRandomTraitSeparatorFailure(
        sheet => RemoveRequiredSeparator(sheet, "Acquired"),
        "missing Acquired separator is rejected");
    VerifyRandomTraitSeparatorFailure(
        sheet => ((JArray)sheet["separators"]!).Add(
            RequiredSeparator(sheet, "Starting").DeepClone()),
        "duplicate required separator is rejected");
    VerifyRandomTraitSeparatorFailure(
        sheet => RequiredSeparator(sheet, "Starting").Property("id")!.Remove(),
        "separator missing its anchor ID is rejected");
    VerifyRandomTraitSeparatorFailure(
        sheet => RequiredSeparator(sheet, "Starting")["id"] = " ",
        "blank separator anchor ID is rejected");
    VerifyRandomTraitSeparatorFailure(
        sheet => RequiredSeparator(sheet, "Starting")["id"] = "NotPresent",
        "separator anchor not found is rejected");
    VerifyRandomTraitSeparatorFailure(
        sheet => RequiredSeparator(sheet, "Hidden")["id"] =
            RequiredSeparator(sheet, "Starting")["id"]!.DeepClone(),
        "duplicate separator anchor target is rejected");
    VerifyRandomTraitSeparatorFailure(
        sheet => ((JArray)sheet["lines"]!).Add(
            ((JObject)((JArray)sheet["lines"]!)[0]!).DeepClone()),
        "ambiguous duplicate source anchor is rejected");
    VerifyRandomTraitSeparatorFailure(
        sheet =>
        {
            JToken startingId = RequiredSeparator(sheet, "Starting")["id"]!.DeepClone();
            RequiredSeparator(sheet, "Starting")["id"] =
                RequiredSeparator(sheet, "Hidden")["id"]!.DeepClone();
            RequiredSeparator(sheet, "Hidden")["id"] = startingId;
        },
        "out-of-order separator anchors are rejected");

    ProjectModel disconnected = CreateRandomTraitExclusionProject();
    EntryModel disconnectedEntry = Entry(disconnected, "trait", "PositiveAbsent");
    disconnectedEntry.Properties.Add(new PropertyModel
    {
        SheetName = "trait",
        Name = "done",
        PropertyPath = "done",
        SourceProperty = null
    });
    string disconnectedBaseline = Json(disconnected);
    EditHistoryService disconnectedHistory = new();
    ProjectMutationService disconnectedMutation = new();
    ProjectOperationResult disconnectedResult = new ProjectOperationService().Execute(
        new RandomTraitExclusionsOperation(
            new RandomTraitExclusionsService(
                disconnectedMutation,
                new GameplayOperationStateService(disconnectedMutation)),
            new[] { "PositiveTrue", "PositiveAbsent", "NegativeDisabled" }),
        disconnected);
    Check(!disconnectedResult.Succeeded &&
          Json(disconnected) == disconnectedBaseline &&
          disconnected.GameplayOperationStates.Count == 0 &&
          disconnectedEntry.SourceEntry!.Property("done") == null &&
          disconnectedEntry.Properties.Count(property =>
              property.EffectivePropertyPath == "done") == 1 &&
          !disconnectedHistory.CanUndo,
        "disconnected missing-source done model fails full preflight without partial mutation or history");

    JObject missingIdEntry = RandomTraitEntry("MissingId", 0, null);
    missingIdEntry.Property("id")!.Remove();
    ProjectModel missingSourceId = CreateProject(
        RandomTraitSheetWithStartingCandidates(missingIdEntry));
    CheckThrows<InvalidOperationException>(
        () => new RandomTraitExclusionsService(
            new ProjectMutationService(),
            new GameplayOperationStateService()).Discover(missingSourceId),
        "factory fallback trait ID is rejected when source id is missing");
    Check(missingSourceId.GameplayOperationStates.Count == 0,
        "missing source trait ID does not create ownership state");

    ProjectModel blankSourceId = CreateProject(RandomTraitSheetWithStartingCandidates(
        RandomTraitEntry(" ", 0, null)));
    CheckThrows<InvalidOperationException>(
        () => service.Discover(blankSourceId),
        "blank source trait ID is rejected");

    ProjectModel mismatchedId = CreateProject(RandomTraitSheetWithStartingCandidates(
        RandomTraitEntry("TraitA", 0, null)));
    Entry(mismatchedId, "trait", "TraitA").Id = "TraitB";
    CheckThrows<InvalidOperationException>(
        () => service.Discover(mismatchedId),
        "source and model trait ID mismatch is rejected");
    Check(service.Discover(CreateRandomTraitExclusionProject()).Count == 4,
        "valid explicit source trait IDs remain discoverable");

    ProjectModel clearAllProject = CreateRandomTraitExclusionProject();
    ProjectMutationService clearMutation = new();
    RandomTraitExclusionsService clearService = new(
        clearMutation,
        new GameplayOperationStateService(clearMutation));
    Check(new ProjectOperationService().Execute(
              new RandomTraitExclusionsOperation(clearService, Array.Empty<string>()),
              clearAllProject).Succeeded &&
          clearService.Discover(clearAllProject).All(candidate => !candidate.IsAllowed),
        "Clear All excludes every dynamically discovered candidate");
    string[] everyId = clearService.Discover(clearAllProject)
        .Select(candidate => candidate.Id).ToArray();
    Check(new ProjectOperationService().Execute(
              new RandomTraitExclusionsOperation(clearService, everyId),
              clearAllProject).Succeeded &&
          clearService.Discover(clearAllProject).All(candidate => candidate.IsAllowed) &&
          Entry(clearAllProject, "trait", "PositiveAbsent").SourceEntry!.Property("done") == null,
        "Select All allows every candidate while preserving absent eligible baselines");

    GameplayOperationStateModel savedState = profileSource.GameplayOperationStates.Single().DeepClone();
    ProjectModel missingOwned = CreateRandomTraitExclusionProject();
    EntryModel removedOwned = Entry(missingOwned, "trait", "PositiveAbsent");
    missingOwned.Sheets.Single(sheet => sheet.Name == "trait").Entries.Remove(removedOwned);
    removedOwned.SourceEntry!.Remove();
    missingOwned.GameplayOperationStates.Add(savedState.DeepClone());
    RandomTraitExclusionsService compatibilityService = new(
        new ProjectMutationService(),
        new GameplayOperationStateService(new ProjectMutationService()));
    CheckThrows<InvalidOperationException>(() => compatibilityService.Discover(missingOwned),
        "missing owned random trait fails compatibility");

    ProjectModel changedPersonality = CreateRandomTraitExclusionProject();
    changedPersonality.GameplayOperationStates.Add(savedState.DeepClone());
    Entry(changedPersonality, "trait", "PositiveAbsent").SourceEntry!
        .SelectToken("props.personality")!.Replace(1);
    CheckThrows<InvalidOperationException>(() => compatibilityService.Discover(changedPersonality),
        "changed owned personality fails compatibility");

    ProjectModel changedGroup = CreateRandomTraitExclusionProject();
    changedGroup.GameplayOperationStates.Add(savedState.DeepClone());
    JObject movedTrait = Entry(changedGroup, "trait", "PositiveAbsent").SourceEntry!;
    JArray changedGroupLines = (JArray)changedGroup.Sheets.Single(
        sheet => sheet.Name == "trait").SourceSheet!["lines"]!;
    movedTrait.Remove();
    int hiddenIndex = changedGroupLines.OfType<JObject>().ToList().FindIndex(
        source => source.Value<string>("id") == "HiddenTrait");
    changedGroupLines.Insert(hiddenIndex, movedTrait);
    CheckThrows<InvalidOperationException>(() => compatibilityService.Discover(changedGroup),
        "changed owned generation group fails compatibility");

    ProjectModel unsupportedGroup = CreateRandomTraitExclusionProject();
    unsupportedGroup.GameplayOperationStates.Add(savedState.DeepClone());
    JObject unsupportedMoved = Entry(unsupportedGroup, "trait", "PositiveAbsent").SourceEntry!;
    JArray unsupportedLines = (JArray)unsupportedGroup.Sheets.Single(
        sheet => sheet.Name == "trait").SourceSheet!["lines"]!;
    unsupportedMoved.Remove();
    int recruitmentIndex = unsupportedLines.OfType<JObject>().ToList().FindIndex(
        source => source.Value<string>("id") == "RecruitmentWithoutPersonality");
    unsupportedLines.Insert(recruitmentIndex, unsupportedMoved);
    CheckThrows<InvalidOperationException>(() => compatibilityService.Discover(unsupportedGroup),
        "owned trait moved to an unsupported group fails compatibility");

    ProjectModel updatedGame = CreateRandomTraitExclusionProject();
    _ = compatibilityService.RestoreState(updatedGame, savedState);
    SheetModel updatedTraitSheet = updatedGame.Sheets.Single(sheet => sheet.Name == "trait");
    JObject addedSource = RandomTraitEntry("NewUpdateTrait", 0, null);
    JArray updatedLines = (JArray)updatedTraitSheet.SourceSheet!["lines"]!;
    int acquiredIndex = updatedLines.OfType<JObject>().ToList().FindIndex(
        source => source.Value<string>("id") == "AcquiredTrait");
    updatedLines.Insert(acquiredIndex, addedSource);
    updatedTraitSheet.Entries.Add(
        new ProjectModelFactory().CreateEntryModel("trait", addedSource, updatedTraitSheet.Entries.Count + 1));
    IReadOnlyList<RandomTraitExclusionCandidate> updatedCandidates =
        compatibilityService.Discover(updatedGame);
    Check(updatedCandidates.Any(candidate => candidate.Id == "NewUpdateTrait") &&
          savedState.ElementCount + 1 == updatedCandidates.Count,
        "new compatible traits retain their update baseline without invalidating old ownership");
    Check(new ProjectOperationService().Execute(
              new RandomTraitExclusionsOperation(
                  compatibilityService,
                  updatedCandidates.Where(candidate => candidate.IsAllowed)
                      .Select(candidate => candidate.Id).ToArray()),
              updatedGame).Succeeded &&
          updatedGame.GameplayOperationStates.Single().ElementCount == updatedCandidates.Count,
        "explicit reapply expands dynamic state ownership to newly discovered traits");

    ProjectModel independent = CreateProject(
        Sheet("constant",
            ScalarEntry("RandomTrait1Positive1Negative", 0.2),
            ScalarEntry("RandomTrait2Positive", 0.3),
            ScalarEntry("RandomTrait1Positive", 0.1)),
        RandomTraitSheetWithStartingCandidates(
            RandomTraitEntry("IndependentPositive", 0, null)));
    Check(ExecutePreset(independent, ProgressionType.PositiveRandomTraits, "PositiveOnly").Succeeded,
        "Positive Random Traits applies alongside exclusions");
    ProjectMutationService independentMutation = new();
    RandomTraitExclusionsService independentService = new(
        independentMutation,
        new GameplayOperationStateService(independentMutation));
    Check(new ProjectOperationService().Execute(
              new RandomTraitExclusionsOperation(independentService, Array.Empty<string>()),
              independent).Succeeded &&
          independent.GameplayOperationStates.Select(state => state.OperationType)
              .OrderBy(type => type).SequenceEqual(new[]
              {
                  ProgressionType.PositiveRandomTraits,
                  ProgressionType.RandomTraitExclusions
              }.OrderBy(type => type)),
        "Positive Random Traits and Random Trait Exclusions keep independent state and targets");

    Check((int)ProgressionType.PositiveRandomTraits == 24 &&
          (int)ProgressionType.RandomTraitExclusions == 25,
        "Random Trait Exclusions appends operation identity without renumbering existing states");
    Console.WriteLine("PASS Random Trait Exclusions discovery, exact baselines, history, rollback, and profiles");
}

static void VerifyProfileUpdate()
{
    string libraryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"wartales-profile-update-{Guid.NewGuid():N}");
    Directory.CreateDirectory(libraryDirectory);

    try
    {
        ModProfileLibraryService library = new(
            new ModProfileLibraryPathService(libraryDirectory),
            new ModProfileSerializationService());
        ModProfileWorkflowService workflow = new();
        ProjectModel originalProject = CreateTraitProject(0.25, 0.25, 0.25);
        ApplyPreset(new ProjectOperationService(), CreatePresetService(), originalProject,
            ProgressionType.PositiveRandomTraits, "PositiveOnly");

        ModProfileModel captured = workflow.CreateProfile(
            originalProject, "Managed Profile", "Description", "Author", "7.2", "old-editor");
        DateTimeOffset createdAt = new(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        ModProfileModel originalProfile = new()
        {
            Metadata = new ModProfileMetadataModel
            {
                Name = captured.Metadata.Name,
                Description = captured.Metadata.Description,
                Author = captured.Metadata.Author,
                ProfileVersion = captured.Metadata.ProfileVersion,
                CreatedAtUtc = createdAt,
                ModifiedAtUtc = createdAt,
                Tags = new() { "party", "quality-of-life" }
            },
            Snapshot = captured.Snapshot,
            OperationRequests = new()
            {
                new ProfileOperationRequestModel
                {
                    OperationId = ProfileOperationIds.UpgradeAllEquipment
                }
            }
        };
        ModProfileSummaryModel summary = library.AddProfile(originalProfile);
        string originalPath = summary.FilePath;

        ProfileManagerViewModel manager = new(
            library,
            new WpfFileDialogService(),
            new WpfMessageDialogService(),
            _ => false)
        {
            CanApplyToCurrentProject = true
        };
        Check(manager.SelectedProfile == null &&
              !manager.CanUpdate &&
              !manager.UpdateCommand.CanExecute(null),
            "profile update is disabled until a managed profile is explicitly selected");
        manager.SelectedProfile = manager.Profiles.Single(candidate =>
            candidate.FilePath == originalPath);
        Check(manager.CanUpdate && manager.UpdateCommand.CanExecute(null),
            "explicit managed profile selection enables update");

        ProjectModel currentProject = CreateTraitProject(0.25, 0.25, 0.25);
        ApplyPreset(new ProjectOperationService(), CreatePresetService(), currentProject,
            ProgressionType.PositiveRandomTraits, "PositiveOnly");
        ModProfileModel loaded = library.LoadProfile(summary);
        ModProfileModel rebuilt = workflow.CreateUpdatedProfile(
            currentProject, loaded, "new-editor");
        ModProfileSummaryModel updatedSummary = library.UpdateProfile(
            summary,
            rebuilt,
            reloaded => workflow.ValidateUpdatedProfileCandidate(
                currentProject,
                loaded,
                reloaded));
        ModProfileModel updated = library.LoadProfile(updatedSummary);

        Check(updatedSummary.FilePath == originalPath &&
              updatedSummary.FileName == summary.FileName,
            "profile update preserves managed path and filename");
        Check(updated.Metadata.Name == "Managed Profile" &&
              updated.Metadata.Description == "Description" &&
              updated.Metadata.Author == "Author" &&
              updated.Metadata.ProfileVersion == "7.2" &&
              updated.Metadata.Tags.SequenceEqual(new[] { "party", "quality-of-life" }),
            "profile update preserves identity metadata");
        Check(updated.Metadata.CreatedAtUtc == createdAt &&
              updated.Metadata.ModifiedAtUtc > createdAt,
            "profile update preserves creation time and refreshes modification time");
        Check(updated.FormatVersion == ModProfileFormat.CurrentVersion &&
              updated.Snapshot.EditorVersion == "new-editor" &&
              updated.Snapshot.GameplayOperationStates.Single().OperationType ==
                  ProgressionType.PositiveRandomTraits,
            "profile update refreshes current format, snapshot metadata, and gameplay state");
        Check(updated.OperationRequests.Count == 0,
            "profile update refreshes additive requests instead of retaining old requests");
        Check(updated.Snapshot.Categories.Single()
                  .Settings.SelectMany(setting => setting.Properties).Count() == 3,
            "profile update rebuilds ordinary property changes from the current project");

        string bytesBeforeUnsafeOverload = File.ReadAllText(originalPath);
        CheckThrows<InvalidOperationException>(
            () => library.UpdateProfile(updatedSummary, rebuilt),
            "public managed replacement overload rejects missing semantic validation");
        Check(File.ReadAllText(originalPath) == bytesBeforeUnsafeOverload,
            "validation-bypass rejection preserves the managed profile bytes");

        ModProfileModel legacy = new()
        {
            FormatVersion = ModProfileFormat.LegacyVersion,
            Metadata = originalProfile.Metadata,
            Snapshot = originalProfile.Snapshot
        };
        ModProfileModel upgradedLegacy = workflow.CreateUpdatedProfile(
            currentProject, legacy, "new-editor");
        Check(upgradedLegacy.FormatVersion == ModProfileFormat.CurrentVersion,
            "supported older profile update rewrites the current format");

        manager.ReportProfileUpdated(originalPath);
        Check(manager.SelectedProfile?.FilePath == originalPath &&
              manager.Status == "Profile updated successfully",
            "profile update refreshes and reselects the same managed path");

        string beforeFailure = File.ReadAllText(originalPath);
        ModProfileModel invalid = new()
        {
            Metadata = rebuilt.Metadata,
            Snapshot = null!,
            OperationRequests = rebuilt.OperationRequests
        };
        bool failed = false;
        try
        {
            library.UpdateProfile(
                updatedSummary,
                invalid,
                reloaded => workflow.ValidateUpdatedProfileCandidate(
                    currentProject,
                    updated,
                    reloaded));
        }
        catch (ModProfileSerializationException)
        {
            failed = true;
        }
        Check(failed && File.ReadAllText(originalPath) == beforeFailure,
            "profile serialization failure leaves the original file intact");

        ModProfileSummaryModel outside = new()
        {
            Name = updatedSummary.Name,
            FilePath = Path.Combine(Path.GetTempPath(), "outside.wtprofile")
        };
        CheckThrows<InvalidOperationException>(
            () => library.UpdateProfile(
                outside,
                rebuilt,
                reloaded => workflow.ValidateUpdatedProfileCandidate(
                    currentProject,
                    loaded,
                    reloaded)),
            "profile update rejects paths outside the managed library");

        string newerPath = Path.Combine(libraryDirectory, "newer.wtprofile");
        string newerJson = "{\"FormatVersion\":999}";
        File.WriteAllText(newerPath, newerJson);
        ModProfileSummaryModel newerSummary = new()
        {
            Name = "Newer",
            FilePath = newerPath
        };
        CheckThrows<ModProfileSerializationException>(
            () => library.LoadProfile(newerSummary),
            "unsupported newer profile is rejected before update");
        Check(File.ReadAllText(newerPath) == newerJson,
            "unsupported newer profile remains untouched");
    }
    finally
    {
        if (Directory.Exists(libraryDirectory))
            Directory.Delete(libraryDirectory, recursive: true);
    }

    Console.WriteLine("PASS explicit managed profile authoritative atomic update");
}

static void VerifyProfileUpdateIntegrityAndAccounting()
{
    string workDirectory = Path.Combine(
        Path.GetTempPath(),
        $"wartales-profile-integrity-{Guid.NewGuid():N}");
    Directory.CreateDirectory(workDirectory);

    try
    {
        ModProfileLibraryService library = new(
            new ModProfileLibraryPathService(workDirectory),
            new ModProfileSerializationService());
        ModProfileWorkflowService workflow = new();
        ProjectOperationService operations = new();
        ProjectMutationService mutation = new();
        GameplayOperationStateService stateService = new(mutation);
        RandomTraitExclusionsService exclusions = new(mutation, stateService);

        ProjectModel authoring = CreateProfileIntegrityProject();
        _ = mutation.EnsurePropertyByPath(
            Entry(authoring, "constant", "IntegrityScalar"),
            "value",
            new JValue(2));
        _ = mutation.EnsurePropertyByPath(
            Entry(authoring, "item", "Anvil"),
            "custom.height",
            new JValue(11));
        ApplyPreset(
            operations,
            CreatePresetService(),
            authoring,
            ProgressionType.CampfireExpansion,
            "Expanded");
        ApplyPreset(
            operations,
            CreatePresetService(),
            authoring,
            ProgressionType.FishingSpeed,
            "Fast");
        ContentCreationService contentCreation = new(mutation);
        ProjectOperationResult campFacilities = operations.Execute(
            new AddCampFacilitiesOperation(contentCreation),
            authoring);
        ProjectOperationResult equipment = operations.Execute(
            new UpgradeAllEquipmentOperation(contentCreation),
            authoring);
        Check(campFacilities.Succeeded && equipment.Succeeded,
            "mixed profile fixture applies both additive operations");

        string[] candidateIds =
            RandomTraitExclusionsService.ResolveCandidateIds(authoring)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
        string createdId = "CreatedAbsent";
        string[] initialAllowed = candidateIds
            .Where(id => id != createdId)
            .ToArray();
        ProjectOperationResult initialExclusions = operations.Execute(
            new RandomTraitExclusionsOperation(exclusions, initialAllowed),
            authoring);
        Check(initialExclusions.Succeeded &&
              Entry(authoring, "trait", createdId).SourceEntry!.Property("done") != null,
            "mixed profile fixture includes a created absent-baseline property");

        ModProfileModel initial = workflow.CreateProfile(
            authoring,
            "Integrity Profile",
            "Preserved description",
            "Preserved author",
            "4.2",
            "before-update");
        initial.Metadata.Tags.Add("integrity");
        int initialEffectiveCount =
            new ProfileEffectiveChangeCountService().Calculate(initial);
        Check(initial.Snapshot.Categories
                .Single(category => category.Name == "item")
                .Settings.Single(setting => setting.Id == "Anvil")
                .Properties.Any(property =>
                    property.PropertyPath == "custom.height") &&
              !initial.Snapshot.Categories
                .Single(category => category.Name == "item")
                .Settings.Single(setting => setting.Id == "Anvil")
                .Properties.Any(property =>
                    property.PropertyPath == "tool.height"),
            "additive filtering retains an unrelated same-leaf path and filters only builder-owned output");
        ModProfileSummaryModel summary = library.AddProfile(initial);
        string managedPath = summary.FilePath;

        ProjectModel appliedProject = CreateProfileIntegrityProject();
        ModificationSnapshotImportResultModel initialApply =
            workflow.ApplyProfile(appliedProject, initial);
        Check(!initialApply.HasFailures &&
              JToken.DeepEquals(
                  appliedProject.RootDocument,
                  authoring.RootDocument),
            "production profile workflow applies the original mixed profile to clean project A");

        string savedCdb = Path.Combine(workDirectory, "accepted.cdb");
        JsonDataService jsonDataService = new();
        jsonDataService.SaveProject(appliedProject, savedCdb);
        ProjectModel source = jsonDataService.LoadProject(savedCdb);
        Check(Entry(source, "constant", "IntegrityScalar")
                  .SourceEntry!["value"]!.Value<int>() == 2,
            "reopened CDB loads the previously applied profile state as its current baseline");
        Check(new ProfileEffectiveChangeCountService().Calculate(source) == 0 &&
              source.Sheets.SelectMany(sheet => sheet.Entries)
                  .SelectMany(entry => entry.Properties)
                  .All(property => !property.IsModified),
            "production save/reload accepts profile-applied leaves as the current baseline");

        string[] fiveNewExclusions = candidateIds
            .Where(id => id != createdId)
            .Take(5)
            .ToArray();
        string[] revisedAllowed = initialAllowed
            .Except(fiveNewExclusions, StringComparer.Ordinal)
            .ToArray();
        ProjectOperationResult revised = operations.Execute(
            new RandomTraitExclusionsOperation(exclusions, revisedAllowed),
            source);
        Check(revised.Succeeded &&
              revised.MutationResult.UpdatedProperties.Count == 5 &&
              revised.MutationResult.CreatedProperties.Count == 0 &&
              revised.MutationResult.RemovedProperties.Count == 0,
            "mixed profile fixture applies exactly five new exclusions after baseline acceptance");
        _ = mutation.EnsurePropertyByPath(
            Entry(source, "constant", "IntegrityScalar"),
            "value",
            new JValue(3));

        ModProfileModel loadedInitial = library.LoadProfile(summary);
        int initialPropertyCount = loadedInitial.Snapshot.Categories
            .SelectMany(category => category.Settings)
            .Sum(setting => setting.Properties.Count);
        ModProfileModel candidate = workflow.CreateUpdatedProfile(
            source,
            loadedInitial,
            "after-update");
        int candidatePropertyCount = candidate.Snapshot.Categories
            .SelectMany(category => category.Settings)
            .Sum(setting => setting.Properties.Count);
        Check(candidatePropertyCount == initialPropertyCount + 5,
            "Update Profile preserves baseline-accepted records and adds five effective paths");
        ModificationSnapshotPropertyModel replacedScalar = candidate.Snapshot.Categories
            .Single(category => category.Name == "constant")
            .Settings.Single(setting => setting.Id == "IntegrityScalar")
            .Properties.Single(property => property.PropertyPath == "value");
        Check(replacedScalar.OriginalValue.Value<int>() == 1 &&
              replacedScalar.CurrentValue.Value<int>() == 3 &&
              new ProfileEffectiveChangeCountService().Calculate(candidate) ==
                  initialEffectiveCount + 5,
            "same-target replacement remains one canonical record while exactly five new changes increase the profile count");

        ModProfileSerializationService profileSerialization = new();
        ModProfileModel incompleteCandidate = profileSerialization.Deserialize(
            profileSerialization.Serialize(candidate));
        ModificationSnapshotSettingModel incompleteScalar =
            incompleteCandidate.Snapshot.Categories
                .Single(category => category.Name == "constant")
                .Settings.Single(setting => setting.Id == "IntegrityScalar");
        incompleteScalar.Properties.Clear();
        incompleteCandidate.Snapshot.Categories
            .Single(category => category.Name == "constant")
            .Settings.Remove(incompleteScalar);
        CheckThrows<InvalidOperationException>(
            () => workflow.ValidateUpdatedProfileCandidate(
                source,
                loadedInitial,
                incompleteCandidate),
            "reopened-project validation rejects a candidate missing a prior required record");

        ModProfileSummaryModel updatedSummary = library.UpdateProfile(
            summary,
            candidate,
            reloaded => workflow.ValidateUpdatedProfileCandidate(
                source,
                loadedInitial,
                reloaded));
        Check(updatedSummary.FilePath == managedPath &&
              library.GetProfiles().Count == 1,
            "validated profile update replaces the same managed profile without duplication");

        ModProfileModel updated = library.LoadProfile(updatedSummary);
        Check(updated.Metadata.Name == "Integrity Profile" &&
              updated.Metadata.Description == "Preserved description" &&
              updated.Metadata.Author == "Preserved author" &&
              updated.Metadata.ProfileVersion == "4.2" &&
              updated.Metadata.Tags.SequenceEqual(new[] { "integrity" }),
            "validated profile update preserves profile identity metadata");

        ProjectModel replayTarget = CreateProfileIntegrityProject();
        ModificationSnapshotImportResultModel replay = workflow.ApplyProfile(
            replayTarget,
            updated);
        IReadOnlyList<ChangeSummaryItemModel> review =
            new ChangeSummaryService().BuildItems(
                replayTarget,
                new ModificationSnapshotService().CreateSnapshot(replayTarget),
                new LocalizationService());
        int projectCount =
            new ProfileEffectiveChangeCountService().Calculate(replayTarget);
        Check(JToken.DeepEquals(replayTarget.RootDocument, source.RootDocument) &&
              replay.UnappliedEffectiveChangeCount == 0,
            "updated mixed profile reloads and reproduces the intended CDB state");
        Check(projectCount == review.Count &&
              projectCount == updatedSummary.EffectiveChangeCount &&
              projectCount == replay.AppliedEffectiveChangeCount,
            "project, Review Changes, profile, and apply feedback share effective-leaf counts");

        foreach (string entryId in new[] { "Firecamp", "FirecampT2", "FirecampT3" })
        {
            Check(review.Any(item =>
                      item.Setting.Id == entryId &&
                      item.Property.EffectivePropertyPath == "tool.height") &&
                  review.Any(item =>
                      item.Setting.Id == entryId &&
                      item.Property.EffectivePropertyPath == "tool.width"),
                $"Review Changes retains duplicate nested height/width paths for {entryId}");
        }

        string bytesBeforeFailedValidation = File.ReadAllText(managedPath);
        CheckThrows<InvalidOperationException>(
            () => library.UpdateProfile(
                updatedSummary,
                updated,
                _ => throw new InvalidOperationException("forced validation failure")),
            "candidate validation failure is surfaced before profile replacement");
        Check(File.ReadAllText(managedPath) == bytesBeforeFailedValidation &&
              !Directory.EnumerateFiles(workDirectory)
                  .Any(path => path.Contains(".update-", StringComparison.Ordinal)),
            "failed candidate validation preserves prior bytes and cleans temporary artifacts");

        new JsonDataService().SaveProject(source, savedCdb);
        ModProfileModel noChangeCandidate = workflow.CreateUpdatedProfile(
            source,
            updated,
            "no-change-update");
        Check(new ProfileEffectiveChangeCountService().Calculate(noChangeCandidate) ==
              updatedSummary.EffectiveChangeCount,
            "no-change update after baseline acceptance preserves effective profile content");
        ModProfileSummaryModel noChangeSummary = library.UpdateProfile(
            updatedSummary,
            noChangeCandidate,
            reloaded => workflow.ValidateUpdatedProfileCandidate(
                source,
                updated,
                reloaded));
        Check(noChangeSummary.FilePath == managedPath &&
              library.GetProfiles().Count == 1 &&
              library.LoadProfile(noChangeSummary).Snapshot.Categories
                  .SelectMany(category => category.Settings)
                  .Sum(setting => setting.Properties.Count) == candidatePropertyCount,
            "no-change baseline-accepted update preserves the same managed content");

        ModProfileModel noChangeProfile = library.LoadProfile(noChangeSummary);
        Check(JToken.DeepEquals(
                  JToken.FromObject(updated.Snapshot.Categories),
                  JToken.FromObject(noChangeProfile.Snapshot.Categories)) &&
              JToken.DeepEquals(
                  JToken.FromObject(updated.Snapshot.GameplayOperationStates),
                  JToken.FromObject(noChangeProfile.Snapshot.GameplayOperationStates)) &&
              JToken.DeepEquals(
                  JToken.FromObject(updated.OperationRequests),
                  JToken.FromObject(noChangeProfile.OperationRequests)),
            "no-new-change update remains semantically equivalent to the prior profile");

        _ = mutation.EnsurePropertyByPath(
            Entry(source, "constant", "IntegrityScalar"),
            "value",
            new JValue(1));
        ModProfileModel restoredScalarCandidate = workflow.CreateUpdatedProfile(
            source,
            noChangeProfile,
            "restored-scalar");
        Check(!restoredScalarCandidate.Snapshot.Categories
                .SelectMany(category => category.Settings)
                .Where(setting => setting.Id == "IntegrityScalar")
                .SelectMany(setting => setting.Properties)
                .Any(),
            "intentional restoration to the clean scalar baseline removes the obsolete record");
        Check(new ProfileEffectiveChangeCountService().Calculate(
                  restoredScalarCandidate) ==
              new ProfileEffectiveChangeCountService().Calculate(
                  noChangeProfile) - 1,
            "intentional scalar reversion decreases effective profile count by exactly one");
        workflow.ValidateUpdatedProfileCandidate(
            source,
            noChangeProfile,
            restoredScalarCandidate);
        ModProfileSummaryModel restoredScalarSummary = library.UpdateProfile(
            noChangeSummary,
            restoredScalarCandidate,
            reloaded => workflow.ValidateUpdatedProfileCandidate(
                source,
                noChangeProfile,
                reloaded));
        ModProfileModel restoredScalarProfile =
            library.LoadProfile(restoredScalarSummary);
        ProjectModel restoredScalarReplay = CreateProfileIntegrityProject();
        _ = workflow.ApplyProfile(restoredScalarReplay, restoredScalarProfile);
        Check(restoredScalarSummary.FilePath == managedPath &&
              library.GetProfiles().Count == 1 &&
              Entry(restoredScalarReplay, "constant", "IntegrityScalar")
                  .SourceEntry!["value"]!.Value<int>() == 1 &&
              JToken.DeepEquals(
                  restoredScalarReplay.RootDocument,
                  source.RootDocument),
            "reloaded decreased profile no longer applies the reverted scalar target");

        ProjectOperationResult restoredAbsent = operations.Execute(
            new RandomTraitExclusionsOperation(
                exclusions,
                revisedAllowed.Append(createdId).ToArray()),
            source);
        Check(restoredAbsent.Succeeded &&
              restoredAbsent.MutationResult.RemovedProperties.Count == 1 &&
              new ProfileEffectiveChangeCountService()
                  .Calculate(restoredAbsent.MutationResult) == 1,
            "removed absent-baseline property contributes one applied effective change");
        ModProfileModel restoredAbsentCandidate = workflow.CreateUpdatedProfile(
            source,
            restoredScalarProfile,
            "restored-absent");
        Check(!restoredAbsentCandidate.Snapshot.Categories
                .SelectMany(category => category.Settings)
                .Where(setting => setting.Id == createdId)
                .SelectMany(setting => setting.Properties)
                .Any(),
            "profile reconciliation removes a restored absent-baseline property record");
        workflow.ValidateUpdatedProfileCandidate(
            source,
            restoredScalarProfile,
            restoredAbsentCandidate);

        ModProfileModel additiveOnly = new()
        {
            Snapshot = new ModificationSnapshotModel(),
            OperationRequests = new()
            {
                new ProfileOperationRequestModel
                {
                    OperationId = ProfileOperationIds.AddCampFacilities
                },
                new ProfileOperationRequestModel
                {
                    OperationId = ProfileOperationIds.UpgradeAllEquipment
                }
            }
        };
        ModProfileModel additiveOverlap = new()
        {
            Snapshot = new ModificationSnapshotModel
            {
                Categories =
                {
                    new ModificationSnapshotCategoryModel
                    {
                        Name = "item",
                        Settings =
                        {
                            new ModificationSnapshotSettingModel
                            {
                                Id = "Anvil",
                                Properties =
                                {
                                    new ModificationSnapshotPropertyModel
                                    {
                                        Name = "model",
                                        PropertyPath = "props.model",
                                        OriginalValue = JValue.CreateNull(),
                                        CurrentValue = new CampFacilityJsonBuilder()
                                            .BuildAnvilProps(new JObject())["model"]!
                                            .DeepClone()
                                    }
                                }
                            },
                            new ModificationSnapshotSettingModel
                            {
                                Id = "SwordStart",
                                Properties =
                                {
                                    new ModificationSnapshotPropertyModel
                                    {
                                        Name = "flags",
                                        PropertyPath = "props.flags",
                                        OriginalValue = new JValue(0),
                                        CurrentValue = new JValue(128)
                                    }
                                }
                            }
                        }
                    }
                }
            },
            OperationRequests = additiveOnly.OperationRequests
        };
        ProfileEffectiveChangeCountService accounting = new();
        Check(accounting.Calculate(additiveOverlap) ==
              accounting.Calculate(additiveOnly),
            "additive deterministic output deduplicates overlapping effective paths");

        ModProfileModel upgradeOnly = new()
        {
            Snapshot = new ModificationSnapshotModel(),
            OperationRequests = new()
            {
                new ProfileOperationRequestModel
                {
                    OperationId = ProfileOperationIds.UpgradeAllEquipment
                }
            }
        };
        ModProfileModel pathlessUpgradeCollision = new()
        {
            Snapshot = new ModificationSnapshotModel
            {
                Categories =
                {
                    new ModificationSnapshotCategoryModel
                    {
                        Name = "item",
                        Settings =
                        {
                            new ModificationSnapshotSettingModel
                            {
                                Id = "SwordStart",
                                Properties =
                                {
                                    new ModificationSnapshotPropertyModel
                                    {
                                        Name = "flags",
                                        PropertyPath = string.Empty,
                                        OriginalValue = new JValue(0),
                                        CurrentValue = new JValue(128)
                                    }
                                }
                            }
                        }
                    }
                }
            },
            OperationRequests = upgradeOnly.OperationRequests
        };
        Check(accounting.Calculate(pathlessUpgradeCollision) ==
              accounting.Calculate(upgradeOnly) + 1,
            "pathless legacy flags are not guessed to overlap canonical props.flags output");

        Console.WriteLine(
            "PASS mixed profile update integrity, safe replacement, paths, removals, and unified counts");
    }
    finally
    {
        if (Directory.Exists(workDirectory))
        {
            Directory.Delete(workDirectory, recursive: true);
        }
    }
}

static void VerifyUpdateProfileFinalBlockerCorrections()
{
    ModProfileWorkflowService workflow = new();
    ProjectMutationService mutation = new();
    ModProfileSerializationService serialization = new();

    ProjectModel absentProject = CreateProject(
        Sheet("constant", ScalarEntry("AbsentFixture", 1)));
    EntryModel absentEntry = Entry(absentProject, "constant", "AbsentFixture");
    _ = mutation.EnsurePropertyByPath(
        absentEntry,
        "createdValue",
        new JValue(5));
    ModProfileModel absentProfile = workflow.CreateProfile(
        absentProject,
        "Historical absence");
    ModificationSnapshotPropertyModel absentRecord = absentProfile.Snapshot
        .Categories.Single().Settings.Single().Properties
        .Single(property => property.PropertyPath == "createdValue");
    Check(absentRecord.OriginalPropertyExisted == false,
        "new snapshot records encode authoritative historical structural absence");

    ModProfileModel serializedAbsent = serialization.Deserialize(
        serialization.Serialize(absentProfile));
    Check(serializedAbsent.Snapshot.Categories.Single().Settings.Single()
              .Properties.Single(property => property.PropertyPath == "createdValue")
              .OriginalPropertyExisted == false,
        "historical structural absence survives profile serialization");

    absentEntry.Properties.Single(property =>
            property.EffectivePropertyPath == "createdValue")
        .AcceptCurrentValue();
    ModProfileModel retainedAbsent = workflow.CreateUpdatedProfile(
        absentProject,
        serializedAbsent,
        "absence-retained");
    workflow.ValidateUpdatedProfileCandidate(
        absentProject,
        serializedAbsent,
        retainedAbsent);
    Check(retainedAbsent.Snapshot.Categories.Single().Settings.Single()
              .Properties.Single(property => property.PropertyPath == "createdValue")
              .OriginalPropertyExisted == false,
        "historically absent property remains explicitly absent-backed while present");

    int retainedAbsentCount =
        new ProfileEffectiveChangeCountService().Calculate(retainedAbsent);
    _ = mutation.RemovePropertyByPath(absentEntry, "createdValue");
    ModProfileModel restoredAbsent = workflow.CreateUpdatedProfile(
        absentProject,
        retainedAbsent,
        "absence-restored");
    workflow.ValidateUpdatedProfileCandidate(
        absentProject,
        retainedAbsent,
        restoredAbsent);
    Check(!restoredAbsent.Snapshot.Categories
              .SelectMany(category => category.Settings)
              .SelectMany(setting => setting.Properties)
              .Any(property => property.PropertyPath == "createdValue") &&
          new ProfileEffectiveChangeCountService().Calculate(restoredAbsent) ==
              retainedAbsentCount - 1,
        "proven absent-to-created-to-absent restoration removes one profile change");

    ProjectModel presentNullProject = CreateProject(
        Sheet(
            "constant",
            new JObject
            {
                ["id"] = "PresentNullFixture",
                ["nullableValue"] = JValue.CreateNull()
            }));
    EntryModel presentNullEntry =
        Entry(presentNullProject, "constant", "PresentNullFixture");
    _ = mutation.EnsurePropertyByPath(
        presentNullEntry,
        "nullableValue",
        new JValue("configured"));
    ModProfileModel presentNullProfile = workflow.CreateProfile(
        presentNullProject,
        "Present null");
    ModificationSnapshotPropertyModel presentNullRecord =
        presentNullProfile.Snapshot.Categories.Single().Settings.Single()
            .Properties.Single();
    Check(presentNullRecord.OriginalPropertyExisted == true &&
          presentNullRecord.OriginalValue.Type == JTokenType.Null,
        "snapshot distinguishes historically present JSON null from absence");

    ProjectModel valueReversionProject = CreateProject(
        Sheet(
            "constant",
            new JObject
            {
                ["id"] = "PresentNullFixture",
                ["nullableValue"] = JValue.CreateNull()
            }));
    EntryModel valueReversionEntry =
        Entry(valueReversionProject, "constant", "PresentNullFixture");
    _ = mutation.EnsurePropertyByPath(
        valueReversionEntry,
        "nullableValue",
        new JValue("configured"));
    ModProfileModel valueReversionProfile = workflow.CreateProfile(
        valueReversionProject,
        "Present null value reversion");
    _ = mutation.EnsurePropertyByPath(
        valueReversionEntry,
        "nullableValue",
        JValue.CreateNull());
    ModProfileModel valueReverted = workflow.CreateUpdatedProfile(
        valueReversionProject,
        valueReversionProfile,
        "present-null-value-restored");
    workflow.ValidateUpdatedProfileCandidate(
        valueReversionProject,
        valueReversionProfile,
        valueReverted);
    Check(valueReversionEntry.SourceEntry!.Property("nullableValue") != null &&
          !valueReverted.Snapshot.Categories.Any(),
        "restoring an existing property's value to JSON null removes its profile record without deleting the property");

    string nullLibraryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"wartales-null-presence-{Guid.NewGuid():N}");
    Directory.CreateDirectory(nullLibraryDirectory);
    try
    {
        ModProfileLibraryService library = new(
            new ModProfileLibraryPathService(nullLibraryDirectory),
            serialization);
        ModProfileSummaryModel managed = library.AddProfile(presentNullProfile);
        string originalBytes = File.ReadAllText(managed.FilePath);
        _ = mutation.RemovePropertyByPath(presentNullEntry, "nullableValue");
        CheckThrows<InvalidOperationException>(
            () => workflow.CreateUpdatedProfile(
                presentNullProject,
                library.LoadProfile(managed),
                "present-null-deleted"),
            "structural deletion of a historically existing-null property is rejected");
        Check(File.ReadAllText(managed.FilePath) == originalBytes,
            "rejected existing-null deletion preserves prior managed profile bytes");

        ModProfileModel legacyNull = serialization.Deserialize(
            serialization.Serialize(presentNullProfile));
        ModificationSnapshotPropertyModel legacySource =
            legacyNull.Snapshot.Categories.Single().Settings.Single()
                .Properties.Single();
        legacyNull.Snapshot.Categories.Single().Settings.Single().Properties[0] =
            new ModificationSnapshotPropertyModel
            {
                Name = legacySource.Name,
                PropertyPath = legacySource.PropertyPath,
                OriginalPropertyExisted = null,
                OriginalValue = legacySource.OriginalValue.DeepClone(),
                CurrentValue = legacySource.CurrentValue.DeepClone()
            };
        CheckThrows<InvalidOperationException>(
            () => workflow.CreateUpdatedProfile(
                presentNullProject,
                legacyNull,
                "legacy-null-deleted"),
            "legacy null record without structural-presence evidence fails safely when its target is missing");
    }
    finally
    {
        if (Directory.Exists(nullLibraryDirectory))
            Directory.Delete(nullLibraryDirectory, recursive: true);
    }

    ProjectModel staleStateProject = CreateProject(
        Sheet("constant", ScalarEntry("FishingDurationControl", 6)));
    ApplyPreset(
        new ProjectOperationService(),
        CreatePresetService(),
        staleStateProject,
        ProgressionType.FishingSpeed,
        "Fast");
    Check(staleStateProject.GameplayOperationStates.Single().IsCompatible,
        "state-backed preset begins compatible before direct target editing");
    ModProfileModel stateProfile = workflow.CreateProfile(
        staleStateProject,
        "Stale state fixture");
    _ = mutation.EnsurePropertyByPath(
        Entry(staleStateProject, "constant", "FishingDurationControl"),
        "value",
        new JValue(99));
    Check(staleStateProject.GameplayOperationStates.Single().IsCompatible,
        "direct target edit leaves cached compatibility unchanged before Update Profile");

    ModProfileModel staleCorrected = workflow.CreateUpdatedProfile(
        staleStateProject,
        stateProfile,
        "stale-state-corrected");
    Check(!staleStateProject.GameplayOperationStates.Single().IsCompatible &&
          !staleCorrected.Snapshot.GameplayOperationStates.Any() &&
          staleCorrected.Snapshot.Categories.Single().Settings.Single()
              .Properties.Single(property => property.PropertyPath == "value")
              .CurrentValue.Value<int>() == 99,
        "Update Profile observationally refreshes stale compatibility and retains the live ordinary target change");
    workflow.ValidateUpdatedProfileCandidate(
        staleStateProject,
        stateProfile,
        staleCorrected);

    ModProfileModel injectedStale = serialization.Deserialize(
        serialization.Serialize(staleCorrected));
    injectedStale.Snapshot.GameplayOperationStates.Add(
        stateProfile.Snapshot.GameplayOperationStates.Single().DeepClone());
    CheckThrows<InvalidOperationException>(
        () => workflow.ValidateUpdatedProfileCandidate(
            staleStateProject,
            stateProfile,
            injectedStale),
        "independent validation rejects deliberately injected stale gameplay state");

    string staleStateLibraryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"wartales-stale-state-{Guid.NewGuid():N}");
    Directory.CreateDirectory(staleStateLibraryDirectory);
    try
    {
        ModProfileLibraryService library = new(
            new ModProfileLibraryPathService(staleStateLibraryDirectory),
            serialization);
        ModProfileSummaryModel managed = library.AddProfile(stateProfile);
        string originalBytes = File.ReadAllText(managed.FilePath);
        CheckThrows<InvalidOperationException>(
            () => library.UpdateProfile(
                managed,
                injectedStale,
                reloaded => workflow.ValidateUpdatedProfileCandidate(
                    staleStateProject,
                    stateProfile,
                    reloaded)),
            "stale gameplay state fails before managed profile replacement");
        Check(File.ReadAllText(managed.FilePath) == originalBytes &&
              !Directory.EnumerateFiles(staleStateLibraryDirectory)
                  .Any(path => path.Contains(".update-", StringComparison.Ordinal)),
            "stale-state validation failure preserves managed bytes and cleans staging artifacts");
    }
    finally
    {
        if (Directory.Exists(staleStateLibraryDirectory))
            Directory.Delete(staleStateLibraryDirectory, recursive: true);
    }

    ProjectModel compatibleStateProject = CreateProject(
        Sheet("constant", ScalarEntry("FishingDurationControl", 6)));
    ApplyPreset(
        new ProjectOperationService(),
        CreatePresetService(),
        compatibleStateProject,
        ProgressionType.FishingSpeed,
        "Fast");
    ModProfileModel compatibleStateProfile = workflow.CreateProfile(
        compatibleStateProject,
        "Compatible state fixture");
    ModProfileModel compatibleStateUpdated = workflow.CreateUpdatedProfile(
        compatibleStateProject,
        compatibleStateProfile,
        "compatible-state-updated");
    workflow.ValidateUpdatedProfileCandidate(
        compatibleStateProject,
        compatibleStateProfile,
        compatibleStateUpdated);
    Check(compatibleStateUpdated.Snapshot.GameplayOperationStates.Single()
              .OperationType == ProgressionType.FishingSpeed,
        "observational refresh preserves still-compatible gameplay state");

    ModProfileModel duplicateCandidate = serialization.Deserialize(
        serialization.Serialize(compatibleStateUpdated));
    ModificationSnapshotSettingModel duplicateSetting =
        duplicateCandidate.Snapshot.Categories.Single().Settings.Single();
    duplicateSetting.Properties.Add(duplicateSetting.Properties.Single());
    CheckThrows<InvalidOperationException>(
        () => workflow.ValidateUpdatedProfileCandidate(
            compatibleStateProject,
            compatibleStateProfile,
            duplicateCandidate),
        "independent validation rejects a deliberately duplicated canonical property record");

    Console.WriteLine(
        "PASS Update Profile independent validation, historical presence, and state refresh corrections");
}

static void VerifyScalarVanillaRestoration()
{
    ProjectModel project = CreateProject(
        Sheet("constant", ScalarEntry("FishingDurationControl", 7.5)));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ProjectOperationResult fast = executor.Execute(
        new GameplayPresetOperation(
            service,
            ProgressionType.FishingSpeed,
            "Fast"),
        project);
    Check(fast.Succeeded, "differing scalar baseline applies");
    CheckNumber(project, "constant", "FishingDurationControl", "value", 2);

    GameplayOperationStateModel state = project.GameplayOperationStates.Single();
    CheckTokenNumber(state.BaselineArray[0]!["value"]!, 7.5,
        "differing scalar baseline captured");

    string fastJson = Json(project);
    ProjectOperationResult vanilla = executor.Execute(
        new GameplayPresetOperation(
            service,
            ProgressionType.FishingSpeed,
            "Vanilla"),
        project);
    Check(vanilla.Succeeded, "scalar Vanilla applies");
    CheckNumber(project, "constant", "FishingDurationControl", "value", 7.5);
    CheckTokenNumber(project.GameplayOperationStates.Single().BaselineArray[0]!["value"]!,
        7.5, "scalar baseline remains unchanged");
    CheckPresetState(project, ProgressionType.FishingSpeed, "Vanilla");

    string vanillaJson = Json(project);
    VerifyUndoRedo(
        "scalar Vanilla",
        vanilla,
        project,
        fastJson,
        vanillaJson);
    Console.WriteLine("PASS differing-baseline Vanilla restoration");
}

static void VerifyMiningBaselineScaling()
{
    ProjectModel project = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("MiningSpeedCircleMin", 2.0),
            ScalarEntry("MiningSpeedCircleMax", 3.5)));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ApplyPreset(executor, service, project,
        ProgressionType.MiningWoodcuttingTiming, "Easier");
    CheckMining(project, 1.6, 2.8, "80 percent");

    ApplyPreset(executor, service, project,
        ProgressionType.MiningWoodcuttingTiming, "Easy");
    CheckMining(project, 1.2, 2.1, "60 percent");

    ProjectOperationResult veryEasy = ApplyPreset(
        executor,
        service,
        project,
        ProgressionType.MiningWoodcuttingTiming,
        "VeryEasy");
    CheckMining(project, 0.8, 1.4, "40 percent");
    string scaledJson = Json(project);

    ProjectOperationResult vanilla = ApplyPreset(
        executor,
        service,
        project,
        ProgressionType.MiningWoodcuttingTiming,
        "Vanilla");
    CheckMining(project, 2.0, 3.5, "Vanilla");
    CheckTokenNumber(
        project.GameplayOperationStates.Single().BaselineArray[0]!["value"]!,
        2.0,
        "Mining minimum baseline remains unchanged");
    CheckTokenNumber(
        project.GameplayOperationStates.Single().BaselineArray[1]!["value"]!,
        3.5,
        "Mining maximum baseline remains unchanged");
    CheckPresetState(
        project,
        ProgressionType.MiningWoodcuttingTiming,
        "Vanilla");
    VerifyUndoRedo(
        "Mining Vanilla",
        vanilla,
        project,
        scaledJson,
        Json(project));

    ProjectModel unsupported = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("MiningSpeedCircleMin", 4.0),
            ScalarEntry("MiningSpeedCircleMax", 3.0)));
    string before = Json(unsupported);
    ProjectOperationResult rejected = ApplyPreset(
        executor,
        CreatePresetService(),
        unsupported,
        ProgressionType.MiningWoodcuttingTiming,
        "Easy",
        expectSuccess: false);
    Check(!rejected.Succeeded, "unsupported Mining baseline rejected");
    Check(Json(unsupported) == before && unsupported.GameplayOperationStates.Count == 0,
        "unsupported Mining baseline fails before mutation");
    _ = veryEasy;
    Console.WriteLine("PASS Mining baseline-relative scaling");
}

static void VerifyVendorBaselineScaling()
{
    ProjectModel project = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("MerchantRefillPerDaySlow", 0.4),
            ScalarEntry("MerchantRefillPerDayNormal", 1.2),
            ScalarEntry("MerchantRefillPerDayFast", 4.0),
            ScalarEntry("MerchantFullRefillDays", 17)));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ApplyPreset(executor, service, project, ProgressionType.VendorRefresh, "Faster");
    CheckNumber(project, "constant", "MerchantRefillPerDaySlow", "value", 0.8);
    CheckNumber(project, "constant", "MerchantRefillPerDayNormal", "value", 2.4);
    CheckNumber(project, "constant", "MerchantRefillPerDayFast", "value", 8.0);
    CheckNumber(project, "constant", "MerchantFullRefillDays", "value", 10);

    ApplyPreset(executor, service, project, ProgressionType.VendorRefresh, "Vanilla");
    CheckNumber(project, "constant", "MerchantRefillPerDaySlow", "value", 0.4);
    CheckNumber(project, "constant", "MerchantRefillPerDayNormal", "value", 1.2);
    CheckNumber(project, "constant", "MerchantRefillPerDayFast", "value", 4.0);
    CheckNumber(project, "constant", "MerchantFullRefillDays", "value", 17);
    Console.WriteLine("PASS Vendor baseline-relative rates");
}

static void VerifyResourceReplenishment()
{
    ProjectModel project = CreateResourceProject(0.2, 0.5, 1.4, 1.75, 19);
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ProjectOperationResult faster = ApplyPreset(
        executor, service, project,
        ProgressionType.ResourceReplenishment, "Faster");
    CheckResourceValues(project, 0.4, 1.0, 2.8);
    CheckNumber(project, "constant", "GatherRefillFactorExtreme", "value", 1.75);
    CheckNumber(project, "constant", "UnrelatedFixture", "value", 19);
    Check(faster.MutationResult.UpdatedProperties.Count == 3,
        "Resource Faster changes only three refill targets");

    string fasterJson = Json(project);
    ProjectOperationResult same = ApplyPreset(
        executor, service, project,
        ProgressionType.ResourceReplenishment, "Faster");
    Check(!same.MutationResult.WasModified && Json(project) == fasterJson,
        "Resource same-preset Apply is idempotent");

    ApplyPreset(executor, service, project,
        ProgressionType.ResourceReplenishment, "Fast");
    CheckResourceValues(project, 0.6, 1.5, 4.2);
    ApplyPreset(executor, service, project,
        ProgressionType.ResourceReplenishment, "VeryFast");
    CheckResourceValues(project, 1.0, 2.5, 7.0);
    string veryFastJson = Json(project);

    var profile = new ModProfileService().CreateProfile(
        project, "Resource Replenishment smoke");
    ProjectModel profileTarget =
        CreateResourceProject(0.2, 0.5, 1.4, 1.75, 19);
    ModificationSnapshotImportResultModel profileResult =
        new ModProfileWorkflowService().ApplyProfile(profileTarget, profile);
    Check(!profileResult.HasFailures,
        "Resource profile replay succeeds");
    CheckResourceValues(profileTarget, 1.0, 2.5, 7.0);
    CheckNumber(profileTarget, "constant", "GatherRefillFactorExtreme", "value", 1.75);
    VerifyGameplayStateFileRoundTrip(
        project,
        profileTarget,
        ProgressionType.ResourceReplenishment);

    ProjectOperationResult vanilla = ApplyPreset(
        executor, service, project,
        ProgressionType.ResourceReplenishment, "Vanilla");
    CheckResourceValues(project, 0.2, 0.5, 1.4);
    CheckNumber(project, "constant", "GatherRefillFactorExtreme", "value", 1.75);
    VerifyUndoRedo(
        "Resource Vanilla", vanilla, project, veryFastJson, Json(project));

    GameplayOperationStateModel state = project.GameplayOperationStates.Single();
    Check(state.BaselineArray.Count == 3,
        "Resource baseline contains all three categories");
    CheckTokenNumber(state.BaselineArray[0]!["value"]!, 0.2,
        "Resource differing Slow baseline preserved");
    VerifySnapshotStateRoundTrip(project, ProgressionType.ResourceReplenishment, 3);
    VerifyProfileStateRoundTrip(project, ProgressionType.ResourceReplenishment, 3);

    foreach (string missing in new[]
             {
                 "GatherRefillSlow", "GatherRefillNormal", "GatherRefillFast"
             })
    {
        ProjectModel malformed = CreateResourceProject(0.2, 0.5, 1.4, 1.75, 19,
            missing);
        string before = Json(malformed);
        Check(!ExecutePreset(
                malformed,
                ProgressionType.ResourceReplenishment,
                "Faster").Succeeded,
            $"Resource missing {missing} fails safely");
        Check(Json(malformed) == before,
            $"Resource missing {missing} rolls back without mutation");
    }

    foreach ((object slow, object normal, object fast, string label) invalid in new[]
             {
                 ((object)"invalid", (object)0.5, (object)1.4, "wrong type"),
                 ((object)0.0, (object)0.5, (object)1.4, "zero"),
                 ((object)(-0.1), (object)0.5, (object)1.4, "negative"),
                 ((object)0.5, (object)0.5, (object)1.4, "equal"),
                 ((object)0.7, (object)0.5, (object)1.4, "unordered")
             })
    {
        ProjectModel malformed = CreateResourceProject(
            invalid.slow, invalid.normal, invalid.fast, 1.75, 19);
        string before = Json(malformed);
        Check(!ExecutePreset(
                malformed,
                ProgressionType.ResourceReplenishment,
                "VeryFast").Succeeded,
            $"Resource {invalid.label} baseline rejected");
        Check(Json(malformed) == before,
            $"Resource {invalid.label} failure is atomic");
    }

    Console.WriteLine("PASS Resource Replenishment baseline scaling and safety");
}

static void VerifyCampfireExpansion()
{
    ProjectModel project = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2"),
            CampfireEntry("FirecampT3")));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ProjectOperationResult expanded = ApplyPreset(
        executor, service, project,
        ProgressionType.CampfireExpansion, "Expanded");
    CheckCampfire(project, "Firecamp", 6, 4);
    CheckCampfire(project, "FirecampT2", 6, 8);
    CheckCampfire(project, "FirecampT3", 6, 12);
    Check(expanded.MutationResult.UpdatedProperties.Count == 16,
        "Campfire Expanded records only values that changed");
    string expandedJson = Json(project);

    ProjectOperationResult same = ApplyPreset(
        executor, service, project,
        ProgressionType.CampfireExpansion, "Expanded");
    Check(!same.MutationResult.WasModified && Json(project) == expandedJson,
        "Campfire reapply is idempotent");

    ProjectOperationResult vanilla = ApplyPreset(
        executor, service, project,
        ProgressionType.CampfireExpansion, "Vanilla");
    CheckCampfire(project, "Firecamp", 4, 4);
    CheckCampfire(project, "FirecampT2", 4, 4);
    CheckCampfire(project, "FirecampT3", 4, 4);
    CheckPresetState(project, ProgressionType.CampfireExpansion, "Vanilla");
    VerifyUndoRedo(
        "Campfire Vanilla", vanilla, project, expandedJson, Json(project));
    VerifyProfileStateRoundTrip(project, ProgressionType.CampfireExpansion, 18);

    ProjectModel missingCapacity = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2", omitCapacity: true),
            CampfireEntry("FirecampT3")));
    Check(!ExecutePreset(
            missingCapacity,
            ProgressionType.CampfireExpansion,
            "Expanded").Succeeded,
        "Campfire missing runtime capacity fails safely");

    ProjectModel wrongType = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2", capacity: "invalid"),
            CampfireEntry("FirecampT3")));
    Check(!ExecutePreset(
            wrongType,
            ProgressionType.CampfireExpansion,
            "Expanded").Succeeded,
        "Campfire wrong runtime capacity type fails safely");

    ProjectModel missingToolCapacity = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2", omitToolCapacity: true),
            CampfireEntry("FirecampT3")));
    string missingToolCapacityBefore = Json(missingToolCapacity);
    Check(!ExecutePreset(
            missingToolCapacity,
            ProgressionType.CampfireExpansion,
            "Expanded").Succeeded,
        "Campfire missing source tool capacity fails safely");
    Check(Json(missingToolCapacity) == missingToolCapacityBefore,
        "Campfire missing source tool capacity fails before accepted mutation");

    ProjectModel wrongToolCapacityType = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2", toolCapacity: "invalid"),
            CampfireEntry("FirecampT3")));
    string wrongToolCapacityBefore = Json(wrongToolCapacityType);
    Check(!ExecutePreset(
            wrongToolCapacityType,
            ProgressionType.CampfireExpansion,
            "Expanded").Succeeded,
        "Campfire wrong source tool capacity type fails safely");
    Check(Json(wrongToolCapacityType) == wrongToolCapacityBefore,
        "Campfire wrong source tool capacity type fails before accepted mutation");

    ProjectModel partialTier = CreateProject(
        Sheet(
            "item",
            CampfireEntry("Firecamp"),
            CampfireEntry("FirecampT2")));
    Check(!ExecutePreset(
            partialTier,
            ProgressionType.CampfireExpansion,
            "Expanded").Succeeded,
        "Campfire partial tier fails safely");

    Console.WriteLine("PASS Campfire coordinated dimensions and assignment capacity");
}

static void VerifyBattleCameraBaselineDrift()
{
    ProjectModel project = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("CameraMinDistance", 35),
            ScalarEntry("CameraMaxDistance", 45)));
    GameplayPresetService service = CreatePresetService();
    ProjectOperationService executor = new();

    ProjectOperationResult far = ApplyPreset(
        executor,
        service,
        project,
        ProgressionType.BattleCameraZoom,
        "Far");
    CheckNumber(project, "constant", "CameraMinDistance", "value", 35);
    CheckNumber(project, "constant", "CameraMaxDistance", "value", 48);
    Check(
        far.MutationResult.UpdatedProperties.All(
            property => property.EffectivePropertyPath != "value" ||
                        property.SourceProperty?.Parent is not JObject owner ||
                        owner.Value<string>("id") != "CameraMinDistance"),
        "Battle Camera non-Vanilla does not mutate minimum");

    ApplyPreset(executor, service, project,
        ProgressionType.BattleCameraZoom, "Vanilla");
    CheckNumber(project, "constant", "CameraMinDistance", "value", 35);
    CheckNumber(project, "constant", "CameraMaxDistance", "value", 45);
    Console.WriteLine("PASS Battle Camera baseline drift");
}

static void VerifyLegacyValour()
{
    VerifyLegacyValourSupported();
    VerifyLegacyValourCustom();
    Console.WriteLine("PASS legacy Valour compatibility and upgrade");
}

static void VerifyLegacyValourSupported()
{
    ProjectModel project = CreateValourProject(2, 3, 4);
    AddLegacyPartyState(project, ProgressionType.ValourPoints);
    PartyEconomyService service = CreatePartyService();
    PartyEconomyDialogViewModel viewModel = new(
        project,
        service,
        ProgressionType.ValourPoints);

    Check(viewModel.SelectedTentPreset == "Increased",
        "legacy Valour detects supported current Tent preset");
    Check(viewModel.CanApply, "legacy Valour supported preset can upgrade");
    string before = Json(project);
    ProjectOperationResult upgrade = ExecuteParty(
        project,
        service,
        ProgressionType.ValourPoints,
        viewModel.CreateSettings());
    Check(upgrade.Succeeded, "legacy Valour supported upgrade applies");
    GameplayOperationStateModel expanded = project.GameplayOperationStates.Single();
    Check(expanded.ElementCount == 5 && expanded.BaselineArray.Count == 5,
        "legacy Valour expands to five targets");
    CheckPartyBaseline(expanded, 2, 3, 4);

    string after = Json(project);
    VerifyUndoRedo("legacy Valour upgrade", upgrade, project, before, after);
    Check(project.GameplayOperationStates.Single().ElementCount == 5,
        "legacy Valour redo restores expanded state");
    VerifyProfileStateRoundTrip(project, ProgressionType.ValourPoints, 5);
}

static void VerifyLegacyValourCustom()
{
    ProjectModel project = CreateValourProject(3, 5, 8);
    AddLegacyPartyState(project, ProgressionType.ValourPoints);
    PartyEconomyService service = CreatePartyService();
    PartyEconomyDialogViewModel viewModel = new(
        project,
        service,
        ProgressionType.ValourPoints);

    Check(viewModel.SelectedTentPreset == null && !viewModel.CanApply,
        "legacy Valour custom current Tent values block implicit Apply");
    Check(viewModel.PreviewText.Contains("3 / 5 / 8", StringComparison.Ordinal),
        "legacy Valour reports actual custom Tent values");
    Check(viewModel.ValidationMessage.Contains("custom", StringComparison.OrdinalIgnoreCase),
        "legacy Valour reports custom state clearly");
    CheckPartyValues(project, ProgressionType.ValourPoints, 3, 5, 8);

    viewModel.SelectedTentPreset = "Increased";
    Check(viewModel.CanApply, "legacy Valour explicit preset enables Apply");
    string customJson = Json(project);
    ProjectOperationResult upgrade = ExecuteParty(
        project,
        service,
        ProgressionType.ValourPoints,
        viewModel.CreateSettings());
    Check(upgrade.Succeeded, "legacy Valour custom upgrade applies explicitly");
    GameplayOperationStateModel state = project.GameplayOperationStates.Single();
    CheckPartyBaseline(state, 3, 5, 8);
    CheckPartyValues(project, ProgressionType.ValourPoints, 2, 3, 4);
    string increasedJson = Json(project);
    VerifyUndoRedo(
        "legacy Valour custom upgrade",
        upgrade,
        project,
        customJson,
        increasedJson);

    viewModel.RefreshFromProject();
    viewModel.ResetToGameDefaults();
    Check(viewModel.CanApply, "legacy Valour baseline reset explicitly confirmed");
    ProjectOperationResult restore = ExecuteParty(
        project,
        service,
        ProgressionType.ValourPoints,
        viewModel.CreateSettings());
    Check(restore.Succeeded, "legacy Valour baseline restoration applies");
    CheckPartyValues(project, ProgressionType.ValourPoints, 3, 5, 8);
}

static void VerifyLegacyCarrying()
{
    VerifyLegacyCarryingSupported();
    VerifyLegacyCarryingCustom();
    Console.WriteLine("PASS legacy Carrying compatibility and upgrade");
}

static void VerifyLegacyCarryingSupported()
{
    ProjectModel project = CreateCarryingProject(20, 40, 60, 20, 30);
    AddLegacyPartyState(project, ProgressionType.CarryingCapacity);
    PartyEconomyService service = CreatePartyService();
    PartyEconomyDialogViewModel viewModel = new(
        project,
        service,
        ProgressionType.CarryingCapacity);

    Check(viewModel.SelectedHitchingPostPreset == "Increased",
        "legacy Carrying detects supported current Hitching Post preset");
    Check(viewModel.CanApply, "legacy Carrying supported preset can upgrade");
    ProjectOperationResult upgrade = ExecuteParty(
        project,
        service,
        ProgressionType.CarryingCapacity,
        viewModel.CreateSettings());
    Check(upgrade.Succeeded, "legacy Carrying supported upgrade applies");
    GameplayOperationStateModel expanded = project.GameplayOperationStates.Single();
    Check(expanded.ElementCount == 7 && expanded.BaselineArray.Count == 7,
        "legacy Carrying expands to seven targets");
    CheckCarryingBaseline(expanded, 20, 40, 60, 20, 30);
    VerifyProfileStateRoundTrip(project, ProgressionType.CarryingCapacity, 7);
}

static void VerifyLegacyCarryingCustom()
{
    ProjectModel project = CreateCarryingProject(11, 22, 33, 7, 14);
    AddLegacyPartyState(project, ProgressionType.CarryingCapacity);
    PartyEconomyService service = CreatePartyService();
    PartyEconomyDialogViewModel viewModel = new(
        project,
        service,
        ProgressionType.CarryingCapacity);

    Check(viewModel.SelectedHitchingPostPreset == null && !viewModel.CanApply,
        "legacy Carrying custom current values block implicit Apply");
    Check(viewModel.PreviewText.Contains("11 / 22 / 33", StringComparison.Ordinal) &&
          viewModel.PreviewText.Contains("0 / 7 / 14", StringComparison.Ordinal),
        "legacy Carrying reports actual custom values");
    Check(viewModel.ValidationMessage.Contains("custom", StringComparison.OrdinalIgnoreCase),
        "legacy Carrying reports custom state clearly");
    CheckCarryingValues(project, 11, 22, 33, 7, 14);

    viewModel.SelectedHitchingPostPreset = "Increased";
    Check(viewModel.CanApply, "legacy Carrying explicit preset enables Apply");
    string customJson = Json(project);
    ProjectOperationResult upgrade = ExecuteParty(
        project,
        service,
        ProgressionType.CarryingCapacity,
        viewModel.CreateSettings());
    Check(upgrade.Succeeded, "legacy Carrying custom upgrade applies explicitly");
    GameplayOperationStateModel state = project.GameplayOperationStates.Single();
    CheckCarryingBaseline(state, 11, 22, 33, 7, 14);
    CheckCarryingValues(project, 20, 40, 60, 20, 30);
    string increasedJson = Json(project);
    VerifyUndoRedo(
        "legacy Carrying custom upgrade",
        upgrade,
        project,
        customJson,
        increasedJson);

    viewModel.RefreshFromProject();
    viewModel.ResetToGameDefaults();
    Check(viewModel.CanApply, "legacy Carrying baseline reset explicitly confirmed");
    ProjectOperationResult restore = ExecuteParty(
        project,
        service,
        ProgressionType.CarryingCapacity,
        viewModel.CreateSettings());
    Check(restore.Succeeded, "legacy Carrying baseline restoration applies");
    CheckCarryingValues(project, 11, 22, 33, 7, 14);
}

static void VerifySnapshotPathCompatibility()
{
    ProjectModel project = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "PathFixture",
                ["tool"] = new JObject { ["width"] = 4 },
                ["icon"] = new JObject { ["width"] = 32 },
                ["height"] = 9
            }));
    ModificationSnapshotMatcher matcher = new();

    ModificationMatchResultModel pathMatch = matcher.Match(
        project,
        SnapshotProperty("width", "tool.width"));
    Check(pathMatch.MatchedCount == 1 &&
          pathMatch.Items[0].TargetProperty?.EffectivePropertyPath == "tool.width",
        "new snapshot full path selects exact duplicate leaf");

    ModificationMatchResultModel legacyUnique = matcher.Match(
        project,
        SnapshotProperty("height", string.Empty));
    Check(legacyUnique.MatchedCount == 1 &&
          legacyUnique.Items[0].TargetProperty?.EffectivePropertyPath == "height",
        "legacy unique leaf fallback remains compatible");

    ModificationMatchResultModel legacyAmbiguous = matcher.Match(
        project,
        SnapshotProperty("width", string.Empty));
    Check(legacyAmbiguous.Items.Single().Status ==
          ModificationMatchStatus.PropertyAmbiguous,
        "legacy duplicate leaf fails as ambiguous");
    Check(legacyAmbiguous.Items.Single().TargetProperty == null,
        "legacy duplicate leaf never selects an arbitrary property");

    ChangeSummaryService summary = new();
    IReadOnlyList<ChangeSummaryItemModel> uniqueRows = summary.BuildItems(
        project,
        SnapshotProperty("height", string.Empty),
        new LocalizationService());
    Check(uniqueRows.Count == 1 &&
          uniqueRows[0].Property.EffectivePropertyPath == "height" &&
          uniqueRows[0].CanNavigate,
        "Review Changes displays a unique pathless legacy property");

    IReadOnlyList<ChangeSummaryItemModel> ambiguousRows = summary.BuildItems(
        project,
        SnapshotProperty("width", string.Empty),
        new LocalizationService());
    Check(ambiguousRows.Count == 1 &&
          !ambiguousRows[0].CanNavigate &&
          ambiguousRows[0].CurrentValue.Contains(
              "Ambiguous legacy property",
              StringComparison.Ordinal) &&
          new ProfileEffectiveChangeCountService().Calculate(
              new ModProfileModel
              {
                  Snapshot = SnapshotProperty("width", string.Empty)
              }) == ambiguousRows.Count,
        "Review Changes surfaces ambiguous legacy identity without a count mismatch");
    Console.WriteLine("PASS snapshot path compatibility");
}

static void VerifyLegacyProfileReconciliation()
{
    ProjectMutationService mutation = new();
    ProfileSnapshotReconciliationService reconciliation = new();

    ProjectModel scalar = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "PathFixture",
                ["height"] = 9
            }));
    _ = mutation.EnsurePropertyByPath(
        Entry(scalar, "item", "PathFixture"),
        "height",
        new JValue(10));
    ModificationSnapshotModel scalarLegacy =
        SnapshotProperty("height", string.Empty, 9, 10);
    ModificationSnapshotModel scalarCurrent =
        new ModificationSnapshotService().CreateSnapshot(scalar);
    reconciliation.Reconcile(scalar, scalarLegacy, scalarCurrent);
    Check(scalarCurrent.Categories.Single().Settings.Single()
              .Properties.Single().PropertyPath == "height",
        "unique pathless scalar reconciles to its canonical path");

    ProjectModel nested = CreateProject(
        Sheet(
            "item",
            new JObject
            {
                ["id"] = "PathFixture",
                ["tool"] = new JObject { ["height"] = 4 }
            }));
    _ = mutation.EnsurePropertyByPath(
        Entry(nested, "item", "PathFixture"),
        "tool.height",
        new JValue(5));
    ModificationSnapshotModel nestedLegacy =
        SnapshotProperty("height", string.Empty, 4, 5);
    ModificationSnapshotModel nestedCurrent =
        new ModificationSnapshotService().CreateSnapshot(nested);
    reconciliation.Reconcile(nested, nestedLegacy, nestedCurrent);
    Check(nestedCurrent.Categories.Single().Settings.Single()
              .Properties.Single().PropertyPath == "tool.height",
        "unique nested pathless property reconciles to its canonical path");

    string libraryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"wartales-legacy-ambiguity-{Guid.NewGuid():N}");
    Directory.CreateDirectory(libraryDirectory);
    try
    {
        ProjectModel ambiguous = CreateProject(
            Sheet(
                "item",
                new JObject
                {
                    ["id"] = "PathFixture",
                    ["tool"] = new JObject { ["width"] = 4 },
                    ["icon"] = new JObject { ["width"] = 32 }
                }));
        _ = mutation.EnsurePropertyByPath(
            Entry(ambiguous, "item", "PathFixture"),
            "tool.width",
            new JValue(5));
        ModificationSnapshotModel ambiguousLegacy =
            SnapshotProperty("width", string.Empty, 4, 5);
        ModProfileModel legacyProfile = new()
        {
            Metadata = new ModProfileMetadataModel
            {
                Name = "Ambiguous Legacy"
            },
            Snapshot = ambiguousLegacy
        };
        ModProfileLibraryService library = new(
            new ModProfileLibraryPathService(libraryDirectory),
            new ModProfileSerializationService());
        ModProfileSummaryModel managed = library.AddProfile(legacyProfile);
        string originalBytes = File.ReadAllText(managed.FilePath);

        CheckThrows<InvalidOperationException>(
            () => new ModProfileWorkflowService().CreateUpdatedProfile(
                ambiguous,
                library.LoadProfile(managed),
                "legacy-test"),
            "ambiguous pathless reconciliation fails before replacement");
        Check(File.ReadAllText(managed.FilePath) == originalBytes &&
              library.LoadProfile(managed).Snapshot.Categories.Single()
                  .Settings.Single().Properties.Count == 1,
            "ambiguous pathless reconciliation preserves the prior managed profile and record");
    }
    finally
    {
        if (Directory.Exists(libraryDirectory))
            Directory.Delete(libraryDirectory, recursive: true);
    }

    Console.WriteLine("PASS legacy profile reconciliation and ambiguity safety");
}

static void VerifyApplyFeedbackState()
{
    GameplayApplyFeedbackViewModel feedback = new();
    Check(!feedback.IsVisible,
        "Apply feedback begins hidden");
    feedback.ShowApplied("Settings were updated.");
    Check(feedback.IsVisible &&
          feedback.Heading == "Applied successfully" &&
          feedback.Message == "Settings were updated.",
        "Apply feedback exposes success state");
    feedback.ShowAlreadyApplied();
    Check(feedback.IsVisible &&
          feedback.Heading == "Already applied" &&
          feedback.Message.Contains("already matches", StringComparison.Ordinal),
        "Apply feedback exposes no-op state");
    feedback.Clear();
    Check(!feedback.IsVisible &&
          feedback.Heading.Length == 0 &&
          feedback.Message.Length == 0,
        "Apply feedback clears when settings change");

    StartingResourcesDialogViewModel startingResources = new(
        CreateProject(),
        new GameplayOperationStateService(new ProjectMutationService()));
    startingResources.ApplyFeedback.ShowApplied("Starting resources were updated.");
    startingResources.SetInputBindingValid(false);
    Check(!startingResources.ApplyFeedback.IsVisible,
        "Starting Resources clears stale feedback for invalid input");

    PartyEconomyDialogViewModel partyEconomy = new(
        CreateValourProject(1, 2, 3),
        CreatePartyService(),
        ProgressionType.ValourPoints);
    partyEconomy.ApplyFeedback.ShowAlreadyApplied();
    partyEconomy.SetInputBindingValid(false);
    Check(!partyEconomy.ApplyFeedback.IsVisible,
        "Party Economy clears stale feedback for invalid input");

    LocalizationService localization = new();
    ReferenceDataService references = ReferenceDataService.Instance;
    MainViewModel main = CreateMainViewModel(localization, references);
    ProjectModel reportProject = CreateProject(
        Sheet("constant", ScalarEntry("FishingDurationControl", "invalid")));
    reportProject.SetUpdateCompatibilityReport(
        new UpdateCompatibilityReportService().Create(
            reportProject,
            SourceGenerationTransition.ChangedSourceGeneration));
    main.Project = reportProject;
    Check(main.CheckCompatibilityCommand.CanExecute(null) &&
          main.ReviewUpdateCompatibilityCommand.CanExecute(null),
        "loaded project enables explicit compatibility check command");
    UpdateCompatibilityReport firstCheck =
        main.CheckCurrentProjectCompatibility();
    Check(firstCheck.ProblematicGameplayTools.Any(item =>
              item.ToolName == "Fishing Speed" &&
              item.Status == GameplayCompatibilityStatus.TypeChanged) &&
          !main.IsUpdateCompatibilityWindowOpen,
        "explicit compatibility check assesses current project without implicitly opening a window");
    new ProjectMutationService().EnsurePropertyByPath(
        reportProject.Sheets.Single().Entries.Single(),
        "value",
        new JValue(10));
    UpdateCompatibilityReport secondCheck =
        main.CheckCurrentProjectCompatibility();
    Check(!ReferenceEquals(firstCheck, secondCheck) &&
          ReferenceEquals(reportProject.UpdateCompatibilityReport, secondCheck) &&
          secondCheck.GameplayTools.Any(item =>
              item.ToolName == "Fishing Speed" &&
              item.Status == GameplayCompatibilityStatus.Compatible) &&
          secondCheck.ProblematicGameplayTools.All(item =>
              item.ToolName != "Fishing Speed"),
        "repeated compatibility check replaces stale results using current in-memory content");

    ProjectModel automaticallyReportedProject = CreateProject(
        Sheet("constant", ScalarEntry("ReplacementFixture", 2)));
    automaticallyReportedProject.SetUpdateCompatibilityReport(
        new UpdateCompatibilityReportService().Create(
            automaticallyReportedProject,
            SourceGenerationTransition.ChangedSourceGeneration));
    main.PromoteLoadedProject(
        automaticallyReportedProject,
        "automatic-publication-test.cdb");
    Check(!main.IsUpdateCompatibilityWindowOpen &&
          automaticallyReportedProject.UpdateCompatibilityReport != null,
        "ordinary and QuickBMS shared changed-source publication retains background report data without auto-opening the full window");

    ProjectModel unknownProvenanceProject = CreateProject(
        Sheet("constant", ScalarEntry("UnknownFixture", 3)));
    unknownProvenanceProject.SetUpdateCompatibilityReport(
        new UpdateCompatibilityReportService().Create(
            unknownProvenanceProject,
            SourceGenerationTransition.CurrentSourceGenerationUnknown));
    main.PromoteLoadedProject(
        unknownProvenanceProject,
        "unknown-publication-test.cdb");
    Check(!main.IsUpdateCompatibilityWindowOpen &&
          unknownProvenanceProject.UpdateCompatibilityReport?.Transition ==
              SourceGenerationTransition.CurrentSourceGenerationUnknown,
        "unknown-provenance publication retains background evidence without auto-opening the full window");

    ProjectModel replacementProject = CreateProject(
        Sheet("constant", ScalarEntry("ReplacementFixture", 2)));
    main.Project = replacementProject;
    Check(main.CheckCompatibilityCommand.CanExecute(null) &&
          replacementProject.UpdateCompatibilityReport == null &&
          !main.IsUpdateCompatibilityWindowOpen,
        "project switch clears stale compatibility report and window state while keeping explicit check available");

    MainViewModel emptyMain = CreateMainViewModel(localization, references);
    Check(!emptyMain.CheckCompatibilityCommand.CanExecute(null),
        "compatibility check command is disabled without a loaded project");

    DirectoryInfo? sourceDirectory = new(AppContext.BaseDirectory);
    string? compatibilityWindowPath = null;
    string? mainWindowPath = null;
    string? mainViewModelPath = null;
    while (sourceDirectory != null &&
           (compatibilityWindowPath == null ||
            mainWindowPath == null ||
            mainViewModelPath == null))
    {
        string compatibilityCandidate = Path.Combine(
            sourceDirectory.FullName,
            "Views",
            "UpdateCompatibilityWindow.xaml");
        string mainCandidate = Path.Combine(
            sourceDirectory.FullName,
            "MainWindow.xaml");
        string mainViewModelCandidate = Path.Combine(
            sourceDirectory.FullName,
            "ViewModels",
            "MainViewModel.cs");
        if (File.Exists(compatibilityCandidate))
            compatibilityWindowPath = compatibilityCandidate;
        if (File.Exists(mainCandidate))
            mainWindowPath = mainCandidate;
        if (File.Exists(mainViewModelCandidate))
            mainViewModelPath = mainViewModelCandidate;
        sourceDirectory = sourceDirectory.Parent;
    }
    Check(compatibilityWindowPath != null &&
          mainWindowPath != null &&
          mainViewModelPath != null,
        "compatibility workflow XAML is available for structural verification");
    string compatibilityXaml = File.ReadAllText(compatibilityWindowPath!);
    string mainXaml = File.ReadAllText(mainWindowPath!);
    string mainViewModelSource = File.ReadAllText(mainViewModelPath!);
    Check(compatibilityXaml.Contains("ShowInTaskbar=\"True\"", StringComparison.Ordinal) &&
          compatibilityXaml.Contains("WindowStartupLocation=\"CenterOwner\"", StringComparison.Ordinal) &&
          mainXaml.Contains("Header=\"_Check Compatibility\"", StringComparison.Ordinal) &&
          mainXaml.Contains("Command=\"{Binding CheckCompatibilityCommand}\"", StringComparison.Ordinal),
        "compatibility window and command use the established modeless utility-window configuration");
    Check(mainXaml.Contains(
              "Header=\"_Wartales Location...\"",
              StringComparison.Ordinal) &&
          mainXaml.Contains(
              "Command=\"{Binding SetWartalesLocationCommand}\"",
              StringComparison.Ordinal),
        "Tools exposes Wartales Location through the shared resolver command");
    Check(mainViewModelSource.Contains(
              "Owner = GetMainWindowOwner()",
              StringComparison.Ordinal) &&
          mainViewModelSource.Contains(
              "updateCompatibilityWindow.DataContext = report;",
              StringComparison.Ordinal) &&
          mainViewModelSource.Contains(
              "RestoreAndActivateWindow(updateCompatibilityWindow);",
              StringComparison.Ordinal),
        "compatibility window reuses one owned instance and refreshes it before focus");
    Console.WriteLine("PASS shared Apply feedback state");
}

static void VerifyCatalogCoverage()
{
    Check((int)ProgressionType.RubySapphireValue == 20 &&
          (int)ProgressionType.TimeBetweenRests == 21 &&
          (int)ProgressionType.ResourceReplenishment == 22 &&
          (int)ProgressionType.LecternKnowledgeGain == 23 &&
          (int)ProgressionType.PositiveRandomTraits == 24,
        "new operation type preserves persisted enum identities");
    ProgressionType[] supported = Enum.GetValues<ProgressionType>()
        .Where(GameplayPresetCatalog.IsSupported)
        .ToArray();
    Check(supported.Length == 17, "catalog contains seventeen shared preset tools");

    foreach (ProgressionType type in supported)
    {
        GameplayPresetDefinition definition = GameplayPresetCatalog.Get(type);
        Check(definition.Targets.Count > 0, $"{type} has targets");
        Check(definition.Presets.Count > 0, $"{type} has presets");
        Check(definition.Presets.Count(preset => preset.Key == "Vanilla") == 1,
            $"{type} has exactly one Vanilla preset");
        Check(definition.Presets.Select(preset => preset.Key).Distinct().Count() ==
              definition.Presets.Count,
            $"{type} preset keys are unique");

        foreach (GameplayTargetDefinition target in definition.Targets)
        {
            Check(!string.IsNullOrWhiteSpace(target.Sheet) &&
                  !string.IsNullOrWhiteSpace(target.Entry) &&
                  !string.IsNullOrWhiteSpace(target.Path),
                $"{type} target identity is complete");
            Check((target.Discriminator == null) == (target.Identity == null),
                $"{type} target type is internally consistent");
        }

        foreach (GameplayPresetOption preset in definition.Presets)
        {
            Check(preset.Values.Count == definition.Targets.Count,
                $"{type}/{preset.Key} resolved value count");
            GameplayPresetService.ValidatePreset(definition, preset);
        }
    }
    Console.WriteLine("PASS complete preset catalog validation coverage");
}

static void VerifyMalformedTargets()
{
    ProjectModel missingTier = CreateProject(
        Sheet(
            "item",
            BonusEntry("CookingPotT2", "props", "bonuses", "bonus", "PerfectRecipe", 15)));
    Check(!ExecutePreset(
            missingTier,
            ProgressionType.DeliciousMealChance,
            "High").Succeeded,
        "missing tier fails safely");

    JObject duplicateT2 = BonusEntry(
        "CookingPotT2", "props", "bonuses", "bonus", "PerfectRecipe", 15);
    ((JArray)duplicateT2.SelectToken("props.bonuses")!).Add(
        new JObject { ["bonus"] = "PerfectRecipe", ["value"] = 16 });
    ProjectModel duplicate = CreateProject(
        Sheet(
            "item",
            duplicateT2,
            BonusEntry("CookingPotT3", "props", "bonuses", "bonus", "PerfectRecipe", 30)));
    Check(!ExecutePreset(
            duplicate,
            ProgressionType.DeliciousMealChance,
            "High").Succeeded,
        "duplicate discriminator fails safely");

    ProjectModel missingDiscriminator = CreateProject(
        Sheet(
            "item",
            BonusEntry("Workshop", "tool", "bonusesIfAssigned", "bonus", "Other", 2),
            BonusEntry("WorkshopT2", "tool", "bonusesIfAssigned", "bonus", "RawMaterialOnRest", 2),
            BonusEntry("WorkshopT3", "tool", "bonusesIfAssigned", "bonus", "RawMaterialOnRest", 2)));
    Check(!ExecutePreset(
            missingDiscriminator,
            ProgressionType.WorkshopMaterials,
            "High").Succeeded,
        "missing discriminator fails safely");

    ProjectModel wrongType = CreateProject(
        Sheet("constant", ScalarEntry("FishingDurationControl", "invalid")));
    Check(!ExecutePreset(
            wrongType,
            ProgressionType.FishingSpeed,
            "Fast").Succeeded,
        "wrong token type fails safely");

    ProjectModel partialState = CreateProject(
        Sheet(
            "constant",
            ScalarEntry("ForgeDurationPerfectHeatMin", 0.25),
            ScalarEntry("ForgeDurationPerfectHeatMax", 0.25)));
    Check(ExecutePreset(
            partialState,
            ProgressionType.ForgingAssistance,
            "Easy").Succeeded,
        "multi-target state fixture applies");
    partialState.GameplayOperationStates.Single().BaselineArray.RemoveAt(1);
    string before = Json(partialState);
    Check(!ExecutePreset(
            partialState,
            ProgressionType.ForgingAssistance,
            "Easier").Succeeded,
        "partial multi-target state fails safely");
    Check(Json(partialState) == before,
        "partial multi-target state failure does not mutate project");
    Console.WriteLine("PASS representative malformed-target coverage");
}

static GameplayPresetService CreatePresetService()
{
    ProjectMutationService mutation = new();
    return new GameplayPresetService(
        mutation,
        new GameplayOperationStateService(mutation));
}

static PartyEconomyService CreatePartyService()
{
    ProjectMutationService mutation = new();
    return new PartyEconomyService(
        mutation,
        new GameplayOperationStateService(mutation));
}

static ProjectOperationResult ApplyPreset(
    ProjectOperationService executor,
    GameplayPresetService service,
    ProjectModel project,
    ProgressionType type,
    string preset,
    bool expectSuccess = true)
{
    ProjectOperationResult result = executor.Execute(
        new GameplayPresetOperation(service, type, preset),
        project);
    Check(result.Succeeded == expectSuccess,
        $"{type}/{preset} expected success={expectSuccess}: {result.Message}");
    return result;
}

static ProjectOperationResult ExecutePreset(
    ProjectModel project,
    ProgressionType type,
    string preset)
{
    GameplayPresetService service = CreatePresetService();
    return new ProjectOperationService().Execute(
        new GameplayPresetOperation(service, type, preset),
        project);
}

static ProjectOperationResult ExecuteParty(
    ProjectModel project,
    PartyEconomyService service,
    ProgressionType type,
    PartyEconomySettings settings) =>
    new ProjectOperationService().Execute(
        new PartyEconomyOperation(service, type, settings),
        project);

static void AddLegacyPartyState(
    ProjectModel project,
    ProgressionType type)
{
    JArray current = PartyEconomyService.CaptureTargets(project, type);
    JArray baseline = new(current.Take(2).Select(record => record!.DeepClone()));
    JObject settings = type == ProgressionType.ValourPoints
        ? new JObject
        {
            ["maximumValour"] = baseline[0]!["value"]!.Value<int>(),
            ["restoredValour"] = baseline[1]!["value"]!.Value<int>()
        }
        : new JObject
        {
            ["saddlebagCapacity"] = baseline[0]!["value"]!.Value<int>(),
            ["ponyStartingCapacity"] = baseline[1]!["value"]!.Value<int>()
        };
    project.GameplayOperationStates.Add(new GameplayOperationStateModel
    {
        OperationType = type,
        TargetSheet = Join(baseline, "sheet", ","),
        TargetEntry = Join(baseline, "entry", ","),
        TargetPath = Join(baseline, "path", "|"),
        BaselineArray = baseline,
        GameplaySettings = settings,
        BaselineFingerprint =
            GameplayOperationFingerprintService.CreateContentFingerprint(baseline),
        ExpectedCurrentFingerprint =
            GameplayOperationFingerprintService.CreateContentFingerprint(baseline),
        ElementCount = 2,
        ElementShapeFingerprint =
            GameplayOperationFingerprintService.CreateShapeFingerprint(baseline),
        IsCompatible = true
    });
}

static string Join(JArray array, string property, string separator) =>
    string.Join(
        separator,
        array.OfType<JObject>().Select(record => record.Value<string>(property)));

static void VerifyUndoRedo(
    string name,
    ProjectOperationResult result,
    ProjectModel project,
    string before,
    string after)
{
    EditHistoryService history = new();
    history.Record(new ProjectOperationHistoryAction(
        name,
        result.MutationResult,
        new ProjectOperationTransactionService()));
    Check(history.Undo(), $"{name} Undo command");
    Check(Json(project) == before, $"{name} exact Undo");
    Check(history.Redo(), $"{name} Redo command");
    Check(Json(project) == after, $"{name} exact Redo");
}

static void CheckPresetState(
    ProjectModel project,
    ProgressionType type,
    string expectedPreset)
{
    GameplayOperationStateModel state = project.GameplayOperationStates.Single(
        candidate => candidate.OperationType == type);
    Check(
        string.Equals(
            state.GameplaySettings?.Value<string>("preset"),
            expectedPreset,
            StringComparison.Ordinal),
        $"{type} reset records the {expectedPreset} operation state");
}

static void VerifyProfileStateRoundTrip(
    ProjectModel project,
    ProgressionType type,
    int expectedCount)
{
    ModProfileSerializationService serializer = new();
    var profile = new ModProfileService().CreateProfile(
        project,
        $"{type} compatibility profile");
    var roundTrip = serializer.Deserialize(serializer.Serialize(profile));
    GameplayOperationStateModel state =
        roundTrip.Snapshot.GameplayOperationStates.Single(candidate =>
            candidate.OperationType == type);
    Check(state.ElementCount == expectedCount &&
          state.BaselineArray.Count == expectedCount,
        $"{type} expanded state profile round trip");
}

static void VerifySnapshotStateRoundTrip(
    ProjectModel project,
    ProgressionType type,
    int expectedCount)
{
    ModificationSnapshotModel snapshot =
        new ModificationSnapshotService().CreateSnapshot(project, "smoke");
    ModificationSnapshotSerializationService serializer = new();
    ModificationSnapshotModel roundTrip =
        serializer.Deserialize(serializer.Serialize(snapshot));
    GameplayOperationStateModel state =
        roundTrip.GameplayOperationStates.Single(candidate =>
            candidate.OperationType == type);
    Check(state.ElementCount == expectedCount &&
          state.BaselineArray.Count == expectedCount,
        $"{type} snapshot state round trip");
}

static void VerifyGameplayStateFileRoundTrip(
    ProjectModel source,
    ProjectModel target,
    ProgressionType type)
{
    string cdbPath = Path.Combine(
        Path.GetTempPath(),
        $"wartales-editor-{Guid.NewGuid():N}.cdb");
    GameplayOperationStatePersistenceService persistence = new();
    try
    {
        byte[] cdbBytes = new System.Text.UTF8Encoding(false).GetBytes(
            source.RootDocument.ToString(Newtonsoft.Json.Formatting.Indented));
        File.WriteAllBytes(cdbPath, cdbBytes);
        string identity = new CdbGenerationIdentityService().Calculate(cdbBytes);
        source.EstablishPersistedIdentity(
            identity, identity, SourceProvenanceStatus.Verified);
        target.EstablishPersistedIdentity(
            identity, identity, SourceProvenanceStatus.Verified);
        foreach (GameplayOperationStateModel operationState in
                 source.GameplayOperationStates)
        {
            operationState.ProjectCompatibilityIdentity = identity;
        }
        persistence.Save(source, cdbPath);
        target.GameplayOperationStates.Clear();
        target.GameplayOperationStateWarnings.Clear();
        target.IsGameplayOperationStateModified = false;
        persistence.LoadIntoProject(target, cdbPath);
        GameplayOperationStateModel state =
            target.GameplayOperationStates.Single(candidate =>
                candidate.OperationType == type);
        Check(state.IsCompatible,
            $"{type} operation state save/reload remains compatible");
    }
    finally
    {
        string sidecar = persistence.GetSidecarPath(cdbPath);
        if (File.Exists(sidecar))
            File.Delete(sidecar);
        if (File.Exists(cdbPath))
            File.Delete(cdbPath);
    }
}

static void CheckMining(
    ProjectModel project,
    double minimum,
    double maximum,
    string label)
{
    CheckNumber(project, "constant", "MiningSpeedCircleMin", "value", minimum);
    CheckNumber(project, "constant", "MiningSpeedCircleMax", "value", maximum);
    Check(minimum < maximum, $"Mining {label} relationship preserved");
}

static void CheckPartyValues(
    ProjectModel project,
    ProgressionType type,
    params int[] expected)
{
    JArray current = PartyEconomyService.CaptureTargets(project, type);
    int[] actual = current.Skip(2)
        .Select(record => record!["value"]!.Value<int>())
        .ToArray();
    Check(actual.SequenceEqual(expected),
        $"{type} current expanded values: expected [{string.Join(",", expected)}], actual [{string.Join(",", actual)}]");
}

static void CheckPartyBaseline(
    GameplayOperationStateModel state,
    params int[] expected)
{
    int[] actual = state.BaselineArray.Skip(2)
        .Select(record => record!["value"]!.Value<int>())
        .ToArray();
    Check(actual.SequenceEqual(expected), $"{state.OperationType} expanded baseline");
}

static void CheckCarryingValues(
    ProjectModel project,
    int tier1Base,
    int tier2Base,
    int tier3Base,
    int tier2Trait,
    int tier3Trait) =>
    CheckPartyValues(
        project,
        ProgressionType.CarryingCapacity,
        tier1Base,
        tier2Base,
        tier3Base,
        tier2Trait,
        tier3Trait);

static void CheckCarryingBaseline(
    GameplayOperationStateModel state,
    int tier1Base,
    int tier2Base,
    int tier3Base,
    int tier2Trait,
    int tier3Trait) =>
    CheckPartyBaseline(
        state,
        tier1Base,
        tier2Base,
        tier3Base,
        tier2Trait,
        tier3Trait);

static ProjectModel CreateValourProject(
    int tier1,
    int tier2,
    int tier3) =>
    CreateProject(
        Sheet(
            "constant",
            ScalarEntry("ActionPointBaseMax", 14),
            ScalarEntry("ActionPointGainPerSleep", 2)),
        Sheet(
            "item",
            BonusEntry("Tent", "props", "bonuses", "bonus", "ActionPoint", tier1),
            BonusEntry("TentT2", "props", "bonuses", "bonus", "ActionPoint", tier2),
            BonusEntry("TentT3", "props", "bonuses", "bonus", "ActionPoint", tier3)));

static ProjectModel CreateCarryingProject(
    int tier1Base,
    int tier2Base,
    int tier3Base,
    int tier2Trait,
    int tier3Trait) =>
    CreateProject(
        Sheet(
            "item",
            ArrayEntry(
                "AnimAccCarriage",
                "baseBonus",
                new JObject { ["attribute"] = "Transport", ["value"] = 10 }),
            PersonalBonusEntry("PonyAuge", tier1Base, null),
            PersonalBonusEntry("PonyAugeT2", tier2Base, tier2Trait),
            PersonalBonusEntry("PonyAugeT3", tier3Base, tier3Trait)),
        Sheet(
            "unitClass",
            ArrayEntry(
                "Pony",
                "stats",
                new JObject { ["attribute"] = "Transport", ["value"] = 55 })));

static JObject PersonalBonusEntry(
    string id,
    int baseValue,
    int? traitValue)
{
    JArray bonuses = new()
    {
        new JObject
        {
            ["bonus"] = "PonyAugeTransport",
            ["value"] = baseValue
        }
    };
    if (traitValue.HasValue)
        bonuses.Add(new JObject
        {
            ["bonus"] = "PonyAugeTransportTrait",
            ["value"] = traitValue.Value
        });
    return new JObject
    {
        ["id"] = id,
        ["tool"] = new JObject { ["personalBonuses"] = bonuses }
    };
}

static JObject BonusEntry(
    string id,
    string container,
    string arrayName,
    string discriminator,
    string identity,
    int value) =>
    new()
    {
        ["id"] = id,
        [container] = new JObject
        {
            [arrayName] = new JArray
            {
                new JObject
                {
                    [discriminator] = identity,
                    ["value"] = value
                }
            }
        }
    };

static JObject ArrayEntry(
    string id,
    string arrayName,
    params JObject[] values) =>
    new()
    {
        ["id"] = id,
        [arrayName] = new JArray(values)
    };

static JObject ScalarEntry(string id, object value) =>
    new()
    {
        ["id"] = id,
        ["value"] = JToken.FromObject(value)
    };

static ProjectModel CreateResourceProject(
    object slow,
    object normal,
    object fast,
    object extreme,
    object unrelated,
    string? omitted = null)
{
    List<JObject> entries = new();
    foreach ((string id, object value) in new[]
             {
                 ("GatherRefillSlow", slow),
                 ("GatherRefillNormal", normal),
                 ("GatherRefillFast", fast),
                 ("GatherRefillFactorExtreme", extreme),
                 ("UnrelatedFixture", unrelated)
             })
    {
        if (!string.Equals(id, omitted, StringComparison.Ordinal))
            entries.Add(ScalarEntry(id, value));
    }
    return CreateProject(Sheet("constant", entries.ToArray()));
}

static ProjectModel CreateTraitProject(
    object mixed,
    object twoPositive,
    object onePositive,
    string? omittedEntry = null)
{
    JObject[] entries =
    {
        ScalarEntry("RandomTrait1Positive1Negative", mixed),
        ScalarEntry("RandomTrait2Positive", twoPositive),
        ScalarEntry("RandomTrait1Positive", onePositive)
    };
    return CreateProject(
        Sheet(
            "constant",
            entries.Where(entry =>
                !string.Equals(
                    entry.Value<string>("id"),
                    omittedEntry,
                    StringComparison.Ordinal)).ToArray()));
}

static ProjectModel CreateRandomTraitExclusionProject(
    bool? positiveTrue = true,
    bool? negativeAbsent = null,
    bool? positiveAbsent = null,
    bool? negativeDisabled = false) => CreateProject(
    RandomTraitSheet(
        new[]
        {
            RandomTraitEntry("PositiveTrue", 0, positiveTrue),
            RandomTraitEntry("NegativeAbsent", 1, negativeAbsent)
        },
        new[]
        {
            RandomTraitEntry("HiddenTrait", 0, "unsupported")
        },
        new[]
        {
            RandomTraitEntry("RecruitmentWithoutPersonality", null, "unsupported"),
            RandomTraitEntry("PositiveAbsent", 0, positiveAbsent, 2),
            RandomTraitEntry("NegativeDisabled", 1, negativeDisabled)
        },
        new[]
        {
            RandomTraitEntry("AcquiredTrait", 0, "unsupported")
        }));

static ProjectModel CreateRandomTraitExclusionAndPositiveTraitProject() => CreateProject(
    Sheet(
        "constant",
        ScalarEntry("RandomTrait1Positive1Negative", 0.2),
        ScalarEntry("RandomTrait2Positive", 0.3),
        ScalarEntry("RandomTrait1Positive", 0.1)),
    RandomTraitSheet(
        new[]
        {
            RandomTraitEntry("PositiveTrue", 0, true),
            RandomTraitEntry("NegativeAbsent", 1, null)
        },
        new[] { RandomTraitEntry("HiddenTrait", 0, "unsupported") },
        new[]
        {
            RandomTraitEntry("RecruitmentWithoutPersonality", null, "unsupported"),
            RandomTraitEntry("PositiveAbsent", 0, null, 2),
            RandomTraitEntry("NegativeDisabled", 1, false)
        },
    new[] { RandomTraitEntry("AcquiredTrait", 0, "unsupported") }));

static ProjectModel CreateProfileIntegrityProject()
{
    Type catalogType = typeof(ContentCreationService).Assembly.GetType(
        "WartalesEditor.Services.UpgradeAllEquipmentTargetCatalog")!;
    var targetField = catalogType.GetField(
        "TargetEntryIds",
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Static)!;
    IEnumerable<string> equipmentIds =
        (IEnumerable<string>)targetField.GetValue(null)!;

    List<JObject> items = new()
    {
        CampfireEntry("Firecamp"),
        CampfireEntry("FirecampT2"),
        CampfireEntry("FirecampT3"),
        new JObject
        {
            ["id"] = "Anvil",
            ["props"] = new JObject
            {
                ["activity"] = "Forge",
                ["hideInCheatMenu"] = true
            },
            ["custom"] = new JObject { ["height"] = 7 }
        },
        new JObject
        {
            ["id"] = "ApothecaryTable",
            ["props"] = new JObject
            {
                ["activity"] = "Alchemy",
                ["hideInCheatMenu"] = true
            }
        }
    };
    items.AddRange(equipmentIds.Select(id => new JObject
    {
        ["id"] = id,
        ["props"] = new JObject { ["flags"] = 0 }
    }));

    return CreateProject(
        Sheet(
            "constant",
            ScalarEntry("IntegrityScalar", 1),
            ScalarEntry("FishingDurationControl", 6)),
        Sheet("item", items.ToArray()),
        Sheet("craft"),
        RandomTraitSheet(
            new[]
            {
                RandomTraitEntry("CreatedAbsent", 0, null),
                RandomTraitEntry("IntegrityA", 0, true),
                RandomTraitEntry("IntegrityB", 1, true),
                RandomTraitEntry("IntegrityC", 0, true),
                RandomTraitEntry("IntegrityD", 1, true),
                RandomTraitEntry("IntegrityE", 0, true)
            },
            new[] { RandomTraitEntry("HiddenIntegrity", 0, "unsupported") },
            new[]
            {
                RandomTraitEntry("IntegrityF", 1, true),
                RandomTraitEntry("IntegrityG", 0, true),
                RandomTraitEntry("IntegrityH", 1, true)
            },
            new[] { RandomTraitEntry("AcquiredIntegrity", 0, "unsupported") }));
}

static void AcceptProjectBaselines(
    ProjectModel project,
    GameplayOperationStateService stateService)
{
    foreach (PropertyModel property in project.Sheets
                 .SelectMany(sheet => sheet.Entries)
                 .SelectMany(entry => entry.Properties))
        property.AcceptCurrentValue();
    stateService.AcceptCurrentStates(project);
    project.IsGameplayOperationStateModified = false;
}

static JObject RandomTraitEntry(
    string id,
    int? personality,
    object? done,
    int? generationEligibility = null)
{
    JObject entry = new()
    {
        ["id"] = id,
        ["props"] = personality.HasValue
            ? new JObject { ["personality"] = personality.Value }
            : new JObject()
    };
    if (generationEligibility.HasValue)
        entry["gen"] = generationEligibility.Value;
    if (done != null)
        entry["done"] = JToken.FromObject(done);
    return entry;
}

static JObject RandomTraitSheet(
    IReadOnlyList<JObject> starting,
    IReadOnlyList<JObject> hidden,
    IReadOnlyList<JObject> recruitment,
    IReadOnlyList<JObject> acquired)
{
    if (starting.Count == 0 || hidden.Count == 0 ||
        recruitment.Count == 0 || acquired.Count == 0)
        throw new ArgumentException("Random trait fixture groups require anchor entries.");

    return new JObject
    {
        ["name"] = "trait",
        ["columns"] = new JArray
        {
            new JObject { ["typeStr"] = "0", ["name"] = "id" },
            new JObject
            {
                ["typeStr"] =
                    "10:Animal,NotAnimal,TwoHand,OneHand,Shield,Bow,FistWeapon",
                ["name"] = "gen",
                ["opt"] = true
            },
            new JObject { ["typeStr"] = "17", ["name"] = "props" },
            new JObject
            {
                ["typeStr"] = "2",
                ["name"] = "done",
                ["opt"] = true
            }
        },
        ["lines"] = new JArray(starting.Concat(hidden).Concat(recruitment).Concat(acquired)),
        ["separators"] = new JArray
        {
            new JObject { ["title"] = "Starting", ["id"] = starting[0]["id"]!.DeepClone() },
            new JObject { ["title"] = "Hidden", ["id"] = hidden[0]["id"]!.DeepClone() },
            new JObject { ["title"] = "Recruitment", ["id"] = recruitment[0]["id"]!.DeepClone() },
            new JObject { ["title"] = "Acquired", ["id"] = acquired[0]["id"]!.DeepClone() }
        }
    };
}

static JObject RandomTraitSheetWithStartingCandidates(params JObject[] candidates) =>
    RandomTraitSheet(
        new[] { RandomTraitEntry("StartingAnchor", null, "unsupported") }
            .Concat(candidates).ToArray(),
        new[] { RandomTraitEntry("HiddenAnchor", null, "unsupported") },
        new[] { RandomTraitEntry("RecruitmentAnchor", null, "unsupported") },
        new[] { RandomTraitEntry("AcquiredAnchor", null, "unsupported") });

static void VerifyRandomTraitSeparatorFailure(
    Action<JObject> corrupt,
    string message)
{
    ProjectModel project = CreateRandomTraitExclusionProject();
    JObject sourceSheet = project.Sheets.Single(sheet => sheet.Name == "trait").SourceSheet!;
    corrupt(sourceSheet);
    string baseline = Json(project);
    bool failed = false;
    try
    {
        ProjectMutationService mutation = new();
        _ = new RandomTraitExclusionsService(
            mutation,
            new GameplayOperationStateService(mutation)).Discover(project);
    }
    catch (InvalidOperationException)
    {
        failed = true;
    }

    Check(failed &&
          Json(project) == baseline &&
          project.GameplayOperationStates.Count == 0 &&
          !project.Sheets.SelectMany(sheet => sheet.Entries)
              .SelectMany(entry => entry.Properties).Any(property => property.IsModified),
        message);
}

static JObject RequiredSeparator(JObject sheet, string title) =>
    ((JArray)sheet["separators"]!).OfType<JObject>().Single(separator =>
        separator.Value<string>("title") == title);

static void RemoveRequiredSeparator(JObject sheet, string title) =>
    RequiredSeparator(sheet, title).Remove();

static void CheckTraitValues(
    ProjectModel project,
    double mixed,
    double twoPositive,
    double onePositive)
{
    CheckNumber(project, "constant", "RandomTrait1Positive1Negative", "value", mixed);
    CheckNumber(project, "constant", "RandomTrait2Positive", "value", twoPositive);
    CheckNumber(project, "constant", "RandomTrait1Positive", "value", onePositive);
}

static void CheckResourceValues(
    ProjectModel project,
    double slow,
    double normal,
    double fast)
{
    CheckNumber(project, "constant", "GatherRefillSlow", "value", slow);
    CheckNumber(project, "constant", "GatherRefillNormal", "value", normal);
    CheckNumber(project, "constant", "GatherRefillFast", "value", fast);
}

static JObject CampfireEntry(
    string id,
    object? capacity = null,
    bool omitCapacity = false,
    object? toolCapacity = null,
    bool omitToolCapacity = false)
{
    JObject tool = new()
    {
        ["campWidth"] = 4,
        ["width"] = 4,
        ["campHeight"] = 4,
        ["height"] = 4
    };
    if (!omitToolCapacity)
        tool["toolCapacity"] = JToken.FromObject(toolCapacity ?? 4);
    if (!omitCapacity)
        tool["capacity"] = JToken.FromObject(capacity ?? 4);
    return new JObject
    {
        ["id"] = id,
        ["tool"] = tool
    };
}

static void CheckCampfire(
    ProjectModel project,
    string id,
    int dimension,
    int capacity)
{
    foreach (string path in new[]
             {
                 "tool.campWidth", "tool.width",
                 "tool.campHeight", "tool.height"
             })
        CheckNumber(project, "item", id, path, dimension);
    CheckNumber(project, "item", id, "tool.toolCapacity", capacity);
    CheckNumber(project, "item", id, "tool.capacity", capacity);
}

static JObject Sheet(string name, params JObject[] entries) =>
    new()
    {
        ["name"] = name,
        ["lines"] = new JArray(entries)
    };

static ProjectModel CreateMovementProject(int walk, int run) =>
    CreateProject(
        Sheet(
            "constant",
            new JObject
            {
                ["id"] = OverworldMovementSpeedService.WalkEntryId,
                ["value"] = walk
            },
            new JObject
            {
                ["id"] = OverworldMovementSpeedService.RunEntryId,
                ["value"] = run
            }));

static ProjectModel CreateRainProject(int offset) =>
    CreateProject(
        Sheet(
            "region",
            RainFrequencyService.Regions
                .Select(region => new JObject
                {
                    ["id"] = region.EntryId,
                    ["props"] = new JObject
                    {
                        ["meteo"] = new JObject
                        {
                            ["rainDaysPerMonth"] =
                                region.VanillaValue + offset
                        }
                    }
                })
                .ToArray()));

static ProjectModel CreateProject(params JObject[] sheets)
{
    JObject root = new() { ["sheets"] = new JArray(sheets) };
    ProjectModel project = new()
    {
        FileName = "compatibility-fixture.cdb",
        OriginalJson = root.ToString(),
        RootDocument = root
    };
    ProjectModelFactory factory = new();
    foreach (JObject sheet in sheets)
        project.Sheets.Add(factory.CreateSheetModel(sheet));
    string identity = new CdbGenerationIdentityService().Calculate(
        new System.Text.UTF8Encoding(false).GetBytes(project.OriginalJson));
    project.EstablishPersistedIdentity(
        identity,
        identity,
        SourceProvenanceStatus.Verified);
    return project;
}

static ProjectModel WithUnknownSource(ProjectModel project)
{
    project.EstablishPersistedIdentity(
        project.CurrentCdbContentIdentity,
        null,
        SourceProvenanceStatus.Unknown);
    return project;
}

static EntryModel Entry(
    ProjectModel project,
    string sheet,
    string id) =>
    project.Sheets.Single(candidate => candidate.Name == sheet)
        .Entries.Single(candidate => candidate.Id == id);

static void CheckNumber(
    ProjectModel project,
    string sheet,
    string id,
    string path,
    double expected)
{
    JToken value = Entry(project, sheet, id).SourceEntry!.SelectToken(path)!;
    CheckTokenNumber(value, expected, $"{sheet}/{id}/{path}");
}

static void CheckTokenNumber(JToken actual, double expected, string name) =>
    Check(
        Math.Abs(actual.Value<double>() - expected) < 0.000001,
        $"{name}: expected {expected}, actual {actual}");

static ModificationSnapshotModel SnapshotProperty(
    string name,
    string path,
    int originalValue = 0,
    int currentValue = 1) =>
    new()
    {
        Categories =
        {
            new ModificationSnapshotCategoryModel
            {
                Name = "item",
                Settings =
                {
                    new ModificationSnapshotSettingModel
                    {
                        Id = "PathFixture",
                        Name = "PathFixture",
                        DisplayName = "PathFixture",
                        Properties =
                        {
                            new ModificationSnapshotPropertyModel
                            {
                                Name = name,
                                PropertyPath = path,
                                OriginalValue = originalValue,
                                CurrentValue = currentValue
                            }
                        }
                    }
                }
            }
        }
    };

static string Json(ProjectModel project) =>
    project.RootDocument.ToString(Newtonsoft.Json.Formatting.None);

static void VerifyWartalesInstallationResolution(string parentRoot)
{
    string root = Path.Combine(parentRoot, "location-resolution");
    string locationFile = Path.Combine(root, "settings", "location.json");
    string primarySteam = Path.Combine(root, "primary-steam");
    string secondarySteam = Path.Combine(root, "secondary-steam");
    string primaryInstall = CreateSteamWartalesInstallation(primarySteam);

    WartalesInstallationService primaryService =
        new(locationFile, () => new[] { primarySteam });
    WartalesInstallationResolution primary =
        primaryService.Resolve();
    Check(primary.Installation?.InstallationDirectory ==
              Path.GetFullPath(primaryInstall),
        "primary Steam appmanifest resolves Wartales");

    Check(WartalesInstallationService
              .TryComposeManifestInstallationDirectory(
                  primarySteam,
                  "Wartales",
                  out string? simpleContained)
          && simpleContained == Path.GetFullPath(primaryInstall),
        "ordinary manifest install directory remains beneath Steam common");
    Check(WartalesInstallationService
              .TryComposeManifestInstallationDirectory(
                  primarySteam,
                  Path.Combine("Publisher", "Wartales"),
                  out string? nestedContained)
          && nestedContained == Path.GetFullPath(
              Path.Combine(
                  primarySteam,
                  "steamapps",
                  "common",
                  "Publisher",
                  "Wartales")),
        "ordinary relative manifest subpath remains supported");
    Check(!WartalesInstallationService
              .TryComposeManifestInstallationDirectory(
                  primarySteam,
                  Path.Combine("..", "..", "EscapedWartales"),
                  out _),
        "manifest traversal outside Steam common is rejected");
    Check(!WartalesInstallationService
              .TryComposeManifestInstallationDirectory(
                  primarySteam,
                  Path.Combine(root, "RootedWartales"),
                  out _),
        "rooted manifest installation path is rejected");
    Check(!WartalesInstallationService
              .TryComposeManifestInstallationDirectory(
                  primarySteam,
                  @"\\server\share\Wartales",
                  out _),
        "UNC manifest installation path is rejected");
    Check(!WartalesInstallationService
              .TryComposeManifestInstallationDirectory(
                  primarySteam,
                  Path.Combine("..", "common", "Wartales"),
                  out _),
        "manifest path that exits and re-enters Steam common is rejected");
    Check(!WartalesInstallationService
              .TryComposeManifestInstallationDirectory(
                  primarySteam,
                  Path.Combine("..", "common-other", "Wartales"),
                  out _),
        "Steam common prefix collision is rejected");
    Check(!WartalesInstallationService
              .TryComposeManifestInstallationDirectory(
                  primarySteam,
                  "Invalid|Wartales",
                  out _),
        "manifest installation path with invalid filename characters is rejected");

    string escapedSteam = Path.Combine(root, "escaped-steam");
    string escapedInstallation =
        Path.Combine(escapedSteam, "EscapedWartales");
    Directory.CreateDirectory(escapedInstallation);
    File.WriteAllText(
        Path.Combine(escapedInstallation, "Wartales.exe"),
        "fixture executable");
    File.WriteAllBytes(
        Path.Combine(escapedInstallation, "res.pak"),
        new byte[] { (byte)'P', (byte)'A', (byte)'K', 0, 1 });
    Directory.CreateDirectory(Path.Combine(escapedSteam, "steamapps"));
    File.WriteAllText(
        Path.Combine(
            escapedSteam,
            "steamapps",
            "appmanifest_1527950.acf"),
        "\"AppState\" { \"appid\" \"1527950\" \"installdir\" \"..\\\\..\\\\EscapedWartales\" }");
    WartalesInstallationService escapedService =
        new(
            Path.Combine(root, "escaped", "location.json"),
            () => new[] { escapedSteam });
    Check(escapedService.Resolve().RequiresSelection,
        "automatic discovery never accepts an escaped manifest candidate");

    Directory.Delete(primaryInstall, recursive: true);
    string secondaryInstall =
        CreateSteamWartalesInstallation(secondarySteam);
    Directory.CreateDirectory(Path.Combine(primarySteam, "steamapps"));
    File.WriteAllText(
        Path.Combine(primarySteam, "steamapps", "libraryfolders.vdf"),
        "\"libraryfolders\"\n{\n  \"1\"\n  {\n    \"path\"    \"" +
        secondarySteam.Replace("\\", "\\\\") +
        "\"\n  }\n}");

    WartalesInstallationResolution secondary =
        primaryService.Resolve();
    Check(secondary.Installation?.InstallationDirectory ==
              Path.GetFullPath(secondaryInstall),
        "secondary Steam library resolves through libraryfolders.vdf");

    File.WriteAllText(
        Path.Combine(primarySteam, "steamapps", "libraryfolders.vdf"),
        "malformed metadata");
    Check(primaryService.Resolve().RequiresSelection,
        "malformed libraryfolders metadata falls through safely");

    File.Delete(
        Path.Combine(primarySteam, "steamapps", "libraryfolders.vdf"));
    Check(primaryService.Resolve().RequiresSelection,
        "missing libraryfolders metadata falls through safely");

    WartalesInstallationService inaccessibleMetadata =
        new(
            Path.Combine(root, "inaccessible", "location.json"),
            () => throw new UnauthorizedAccessException("fixture"));
    Check(inaccessibleMetadata.Resolve().RequiresSelection,
        "inaccessible Steam metadata falls through safely");

    File.WriteAllText(
        Path.Combine(primarySteam, "steamapps", "appmanifest_1527950.acf"),
        "\"AppState\" { \"appid\" \"1527950\" \"installdir\" \"Missing\" }");
    Check(primaryService.Resolve().RequiresSelection,
        "stale appmanifest falls through safely");

    string otherSteam = Path.Combine(root, "other-steam");
    string otherInstall = CreateSteamWartalesInstallation(otherSteam);
    WartalesInstallationService multipleService =
        new(
            Path.Combine(root, "multiple", "location.json"),
            () => new[] { secondarySteam, otherSteam });
    Check(multipleService.Resolve().RequiresSelection,
        "multiple valid Wartales installations require explicit selection");

    WartalesInstallationService duplicateService =
        new(
            Path.Combine(root, "duplicate", "location.json"),
            () => new[]
            {
                secondarySteam,
                secondarySteam + Path.DirectorySeparatorChar
            });
    Check(duplicateService.Resolve().Installation?.InstallationDirectory ==
              Path.GetFullPath(secondaryInstall),
        "normalized duplicate candidates resolve as one installation");

    WartalesPackageInfo remembered =
        primaryService.ValidateAndRemember(secondaryInstall);
    Check(File.Exists(locationFile)
          && primaryService.Resolve().Installation?.InstallationDirectory ==
             remembered.InstallationDirectory,
        "valid manual selection persists and is reused");

    string invalidSelection = Path.Combine(root, "invalid-selection");
    Directory.CreateDirectory(invalidSelection);
    CheckImportFailure(
        () => primaryService.ValidateAndRemember(invalidSelection),
        QuickBmsImportFailureKind.WartalesInstallationInvalid,
        "manual selection without Wartales.exe is rejected");
    Check(primaryService.Resolve().Installation?.InstallationDirectory ==
              remembered.InstallationDirectory,
        "invalid manual selection does not replace a saved valid location");

    Directory.Delete(secondaryInstall, recursive: true);
    string rediscoveredInstall =
        CreateSteamWartalesInstallation(otherSteam);
    WartalesInstallationService staleSavedService =
        new(locationFile, () => new[] { otherSteam });
    Check(staleSavedService.Resolve().Installation?.InstallationDirectory ==
              Path.GetFullPath(rediscoveredInstall),
        "stale saved location falls through to Steam discovery");

    string missingExe = Path.Combine(root, "missing-exe");
    Directory.CreateDirectory(missingExe);
    File.WriteAllBytes(
        Path.Combine(missingExe, "res.pak"),
        new byte[] { (byte)'P', (byte)'A', (byte)'K', 0, 1 });
    CheckImportFailure(
        () => primaryService.Validate(missingExe),
        QuickBmsImportFailureKind.WartalesInstallationInvalid,
        "installation without Wartales.exe is not recognized");

    string uiLocationFile =
        Path.Combine(root, "ui", "location.json");
    WartalesInstallationService uiService =
        new(uiLocationFile, () => Array.Empty<string>());
    TestFileDialogService uiDialogs =
        new(folderName: rediscoveredInstall);
    TestMessageDialogService uiMessages = new();
    MainViewModel uiMain = CreateMainViewModel(
        new LocalizationService(),
        ReferenceDataService.Instance,
        uiMessages,
        uiDialogs,
        wartalesInstallationService: uiService,
        quickBmsImportOptions: new QuickBmsImportOptions
        {
            WartalesInstallationDirectory =
                Path.Combine(root, "not-installed"),
            QuickBmsExecutablePath = "quickbms.exe",
            ShiroScriptPath = "script.bms",
            StagingRootDirectory = Path.Combine(root, "staging")
        });
    Check(uiMain.SetWartalesLocationCommand.CanExecute(null),
        "Tools Wartales Location command is enabled while idle");
    uiMain.SetWartalesLocationCommand.Execute(null);
    Check(uiDialogs.FolderCallCount == 1
          && uiService.Resolve().Installation?.InstallationDirectory ==
             Path.GetFullPath(rediscoveredInstall)
          && uiMessages.LastInformation?.Contains(
                 rediscoveredInstall,
                 StringComparison.OrdinalIgnoreCase) == true,
        "Tools Wartales Location validates, persists, and reports a valid selection");

    TestMessageDialogService locationFailureMessages = new();
    MainViewModel locationFailureMain = CreateMainViewModel(
        new LocalizationService(),
        ReferenceDataService.Instance,
        locationFailureMessages,
        new TestFileDialogService(folderName: invalidSelection),
        wartalesInstallationService: uiService,
        quickBmsImportOptions: new QuickBmsImportOptions
        {
            WartalesInstallationDirectory = rediscoveredInstall
        });
    locationFailureMain.SetWartalesLocationCommand.Execute(null);
    Check(locationFailureMessages.ErrorCount == 1
          && locationFailureMain.Status ==
             "The Wartales installation location could not be resolved."
          && uiService.Resolve().Installation?.InstallationDirectory ==
             Path.GetFullPath(rediscoveredInstall),
        "Tools Wartales Location presents one validation error and preserves the prior valid location");

    TestMessageDialogService locationCancelMessages = new();
    MainViewModel locationCancelMain = CreateMainViewModel(
        new LocalizationService(),
        ReferenceDataService.Instance,
        locationCancelMessages,
        new TestFileDialogService(folderName: null),
        wartalesInstallationService: uiService,
        quickBmsImportOptions: new QuickBmsImportOptions
        {
            WartalesInstallationDirectory = rediscoveredInstall
        });
    locationCancelMain.SetWartalesLocationCommand.Execute(null);
    Check(locationCancelMessages.ErrorCount == 0
          && locationCancelMain.Status ==
             "Wartales location was not changed."
          && uiService.Resolve().Installation?.InstallationDirectory ==
             Path.GetFullPath(rediscoveredInstall),
        "Tools Wartales Location cancellation is silent and preserves the prior valid location");

    TestFileDialogService cancelDialogs = new(folderName: null);
    TestMessageDialogService cancelMessages = new();
    MainViewModel cancelMain = CreateMainViewModel(
        new LocalizationService(),
        ReferenceDataService.Instance,
        cancelMessages,
        cancelDialogs,
        wartalesInstallationService: new WartalesInstallationService(
            Path.Combine(root, "cancel", "location.json"),
            () => Array.Empty<string>()),
        quickBmsImportOptions: new QuickBmsImportOptions
        {
            WartalesInstallationDirectory = Path.Combine(root, "absent")
        });
    cancelMain.ImportFromWartalesCommand.Execute(null);
    Check(cancelDialogs.FolderCallCount == 1
          && cancelMain.Project == null
          && cancelMain.Status == "Wartales import cancelled."
          && cancelMessages.ErrorCount == 0,
        "operation-time folder selection cancel is clean and does not publish a project");

    TestMessageDialogService failureMessages = new();
    MainViewModel failureMain = CreateMainViewModel(
        new LocalizationService(),
        ReferenceDataService.Instance,
        failureMessages,
        new TestFileDialogService(folderName: invalidSelection),
        wartalesInstallationService: new WartalesInstallationService(
            Path.Combine(root, "failure", "location.json"),
            () => Array.Empty<string>()),
        quickBmsImportOptions: new QuickBmsImportOptions
        {
            WartalesInstallationDirectory = Path.Combine(root, "absent-failure")
        });
    failureMain.ImportFromWartalesCommand.Execute(null);
    Check(failureMessages.ErrorCount == 1
          && failureMessages.LastError != null
          && failureMain.Status ==
             "The Wartales installation location could not be resolved."
          && failureMain.Project == null,
        "operation-time validation failure remains distinct from cancellation");

    _ = otherInstall;
    Console.WriteLine(
        "PASS shared Wartales installation resolution and persistence");
}

static string CreateSteamWartalesInstallation(string steamRoot)
{
    string steamApps = Path.Combine(steamRoot, "steamapps");
    string installation =
        Path.Combine(steamApps, "common", "Wartales");
    Directory.CreateDirectory(installation);
    File.WriteAllText(
        Path.Combine(installation, "Wartales.exe"),
        "fixture executable");
    File.WriteAllBytes(
        Path.Combine(installation, "res.pak"),
        new byte[] { (byte)'P', (byte)'A', (byte)'K', 0, 1 });
    File.WriteAllText(
        Path.Combine(steamApps, "appmanifest_1527950.acf"),
        "\"AppState\"\n{\n  \"appid\" \"1527950\"\n  \"installdir\" \"Wartales\"\n}");
    return installation;
}

static async Task VerifyQuickBmsImport()
{
    string root = Path.Combine(
        Path.GetTempPath(),
        "WartalesEditorQuickBmsTests",
        Guid.NewGuid().ToString("N"));
    string installation = Path.Combine(root, "Wartales");
    string package = Path.Combine(installation, "res.pak");
    string tools = Path.Combine(root, "quickbms");
    string executable = Path.Combine(tools, "quickbms.exe");
    string script = Path.Combine(tools, "Shiro_Games_PAK_script.bms");
    string stagingRoot = Path.Combine(root, "staging");
    string validCdb =
        "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"Fixture\",\"value\":1}]}]}";

    Directory.CreateDirectory(installation);
    Directory.CreateDirectory(tools);
    File.WriteAllText(
        Path.Combine(installation, "Wartales.exe"),
        "fixture executable");
    File.WriteAllBytes(
        package,
        new byte[]
        {
            (byte)'P', (byte)'A', (byte)'K', 0,
            1, 2, 3, 4
        });
    File.WriteAllText(executable, "fixture executable");
    File.WriteAllText(script, "fixture script");

    QuickBmsImportOptions options = new()
    {
        WartalesInstallationDirectory = installation,
        QuickBmsExecutablePath = executable,
        ShiroScriptPath = script,
        StagingRootDirectory = stagingRoot,
        ProcessTimeout = TimeSpan.FromSeconds(10)
    };

    try
    {
        VerifyWartalesInstallationResolution(root);
        string manualInstallation =
            Path.Combine(root, "manual-import", "Wartales");
        Directory.CreateDirectory(manualInstallation);
        File.WriteAllText(
            Path.Combine(manualInstallation, "Wartales.exe"),
            "fixture executable");
        File.WriteAllBytes(
            Path.Combine(manualInstallation, "res.pak"),
            new byte[]
            {
                (byte)'P', (byte)'A', (byte)'K', 0,
                1, 2, 3, 4
            });
        FakeExternalProcessRunner manualRunner = new(request =>
        {
            string extracted =
                Path.Combine(request.Arguments[2], "content", "data.cdb");
            Directory.CreateDirectory(Path.GetDirectoryName(extracted)!);
            File.WriteAllText(extracted, validCdb);
            return new ExternalProcessResult
            {
                Started = true,
                ExitCode = 0
            };
        });
        TestFileDialogService manualDialogs =
            new(folderName: manualInstallation);
        JsonDataService manualJson = new();
        MainViewModel manualMain = CreateMainViewModel(
            new LocalizationService(),
            ReferenceDataService.Instance,
            new TestMessageDialogService(),
            manualDialogs,
            wartalesInstallationService:
                new WartalesInstallationService(
                    Path.Combine(
                        root,
                        "manual-import-location",
                        "location.json"),
                    () => Array.Empty<string>()),
            quickBmsImportOptions:
                new QuickBmsImportOptions
                {
                    WartalesInstallationDirectory =
                        Path.Combine(root, "missing-manual-import"),
                    QuickBmsExecutablePath = executable,
                    ShiroScriptPath = script,
                    StagingRootDirectory =
                        Path.Combine(root, "manual-import-staging"),
                    ProcessTimeout = TimeSpan.FromSeconds(10)
                });
        manualMain.UseQuickBmsImportServiceForTesting(
            new QuickBmsImportService(
                manualJson,
                new WartalesInstallationService(),
                new QuickBmsToolchainService(),
                manualRunner,
                new ExtractionWorkspaceService(),
                new FileFingerprintService()));
        manualMain.ImportFromWartalesCommand.Execute(null);
        Check(manualDialogs.FolderCallCount == 1
              && manualRunner.Requests.Count == 1
              && manualRunner.Requests[0].Arguments[1] ==
                 Path.Combine(manualInstallation, "res.pak")
              && manualMain.Project != null,
            "valid manual selection continues the same Import action with the exact selected root");

        await VerifyExternalProcessTermination();
        VerifyStagingReparseSafety(root);

        WartalesPackageInfo packageInfo =
            new WartalesInstallationService().Validate(installation);
        Check(packageInfo.PackagePath == Path.GetFullPath(package),
            "Wartales validation resolves the expected package");

        QuickBmsToolchainInfo toolchainInfo =
            new QuickBmsToolchainService().Validate(executable, script);
        Check(toolchainInfo.ExecutablePath == Path.GetFullPath(executable) &&
              toolchainInfo.ScriptPath == Path.GetFullPath(script),
            "toolchain validation resolves the supplied executable and script");

        CheckImportFailure(
            () => new WartalesInstallationService().Validate(
                Path.Combine(root, "missing-install")),
            QuickBmsImportFailureKind.WartalesInstallationInvalid,
            "missing Wartales installation is rejected");

        string missingPackageInstall = Path.Combine(root, "missing-package");
        Directory.CreateDirectory(missingPackageInstall);
        File.WriteAllText(
            Path.Combine(missingPackageInstall, "Wartales.exe"),
            "fixture executable");
        CheckImportFailure(
            () => new WartalesInstallationService().Validate(missingPackageInstall),
            QuickBmsImportFailureKind.PackageMissing,
            "missing res.pak is rejected");

        string invalidInstall = Path.Combine(root, "invalid-package");
        Directory.CreateDirectory(invalidInstall);
        File.WriteAllText(
            Path.Combine(invalidInstall, "Wartales.exe"),
            "fixture executable");
        File.WriteAllText(Path.Combine(invalidInstall, "res.pak"), "invalid");
        CheckImportFailure(
            () => new WartalesInstallationService().Validate(invalidInstall),
            QuickBmsImportFailureKind.PackageInvalid,
            "invalid package signature is rejected");

        CheckImportFailure(
            () => new QuickBmsToolchainService().Validate(
                Path.Combine(root, "missing.exe"), script),
            QuickBmsImportFailureKind.QuickBmsExecutableMissing,
            "missing QuickBMS executable is rejected");
        try
        {
            _ = new QuickBmsToolchainService().Validate(
                Path.Combine(root, "missing.exe"),
                script);
            Check(false,
                "missing QuickBMS message points to the User Guide");
        }
        catch (QuickBmsImportException exception)
        {
            Check(exception.Message.Contains(
                      "For detailed QuickBMS setup instructions, see the User Guide.",
                      StringComparison.Ordinal),
                "missing QuickBMS message points to the User Guide");
        }
        CheckImportFailure(
            () => new QuickBmsToolchainService().Validate(
                executable, Path.Combine(root, "missing.bms")),
            QuickBmsImportFailureKind.ShiroScriptMissing,
            "missing Shiro script is rejected");

        FileFingerprintService fingerprints = new();
        FileFingerprint firstFingerprint = fingerprints.Calculate(package);
        FileFingerprint secondFingerprint = fingerprints.Calculate(package);
        Check(firstFingerprint == secondFingerprint,
            "identical package bytes produce a deterministic fingerprint");
        File.AppendAllText(package, "changed");
        FileFingerprint changedFingerprint = fingerprints.Calculate(package);
        Check(firstFingerprint != changedFingerprint,
            "changed package bytes produce a changed fingerprint");
        File.WriteAllBytes(
            package,
            new byte[]
            {
                (byte)'P', (byte)'A', (byte)'K', 0,
                1, 2, 3, 4
            });

        QuickBmsImportOptions unsafeStagingOptions = new()
        {
            WartalesInstallationDirectory = installation,
            QuickBmsExecutablePath = executable,
            ShiroScriptPath = script,
            StagingRootDirectory = Path.Combine(installation, "Extracted"),
            ProcessTimeout = TimeSpan.FromSeconds(10)
        };
        FakeExternalProcessRunner unsafeStagingRunner = new(_ =>
            new ExternalProcessResult { Started = true, ExitCode = 0 });
        await CheckImportFailureAsync(
            unsafeStagingOptions,
            unsafeStagingRunner,
            QuickBmsImportFailureKind.StagingFailed,
            "staging inside the live installation is rejected before process execution");
        Check(unsafeStagingRunner.Requests.Count == 0,
            "unsafe live-install staging never reaches process execution");

        await CheckImportFailureAsync(
            options,
            new FakeExternalProcessRunner(_ =>
                new ExternalProcessResult
                {
                    StartError = "forced start failure"
                }),
            QuickBmsImportFailureKind.ProcessStartFailed,
            "process start failure preserves the current project");

        await CheckImportFailureAsync(
            options,
            new FakeExternalProcessRunner(_ =>
                new ExternalProcessResult
                {
                    Started = true,
                    TimedOut = true
                }),
            QuickBmsImportFailureKind.ProcessTimedOut,
            "process timeout is rejected without promotion");

        string? retainedTerminationSession = null;
        await CheckImportFailureAsync(
            options,
            new FakeExternalProcessRunner(request =>
            {
                retainedTerminationSession =
                    request.Arguments[2];
                return new ExternalProcessResult
                {
                    Started = true,
                    ProcessId = 12345,
                    TerminationFailed = true
                };
            }),
            QuickBmsImportFailureKind.ProcessTerminationFailed,
            "unproven process termination blocks hashing, promotion, and cleanup");
        Check(!string.IsNullOrWhiteSpace(retainedTerminationSession) &&
              Directory.Exists(retainedTerminationSession),
            "termination failure retains staging because the process may still own it");
        Directory.Delete(
            retainedTerminationSession!,
            recursive: true);

        await CheckImportFailureAsync(
            options,
            new FakeExternalProcessRunner(_ =>
                new ExternalProcessResult
                {
                    Started = true,
                    ExitCode = 7,
                    StandardError = "forced extraction failure"
                }),
            QuickBmsImportFailureKind.ProcessFailed,
            "non-success process exit is rejected");

        await CheckImportFailureAsync(
            options,
            new FakeExternalProcessRunner(_ =>
                new ExternalProcessResult
                {
                    Started = true,
                    ExitCode = 0
                }),
            QuickBmsImportFailureKind.ExtractedCdbMissing,
            "successful exit without data.cdb is rejected");

        await CheckImportFailureAsync(
            options,
            new FakeExternalProcessRunner(request =>
            {
                string output = request.Arguments[2];
                Directory.CreateDirectory(Path.Combine(output, "a"));
                Directory.CreateDirectory(Path.Combine(output, "b"));
                File.WriteAllText(Path.Combine(output, "a", "data.cdb"), validCdb);
                File.WriteAllText(Path.Combine(output, "b", "data.cdb"), validCdb);
                return new ExternalProcessResult { Started = true, ExitCode = 0 };
            }),
            QuickBmsImportFailureKind.ExtractedCdbAmbiguous,
            "ambiguous data.cdb output is rejected");

        await CheckImportFailureAsync(
            options,
            new FakeExternalProcessRunner(request =>
            {
                File.WriteAllText(
                    Path.Combine(request.Arguments[2], "data.cdb"),
                    "not json");
                return new ExternalProcessResult { Started = true, ExitCode = 0 };
            }),
            QuickBmsImportFailureKind.ExtractedCdbInvalid,
            "invalid data.cdb is rejected by the production loader");

        await CheckImportFailureAsync(
            options,
            new FakeExternalProcessRunner(request =>
            {
                File.WriteAllBytes(
                    Path.Combine(request.Arguments[2], "data.cdb"),
                    Array.Empty<byte>());
                return new ExternalProcessResult { Started = true, ExitCode = 0 };
            }),
            QuickBmsImportFailureKind.ExtractedCdbInvalid,
            "empty data.cdb is rejected explicitly");

        string outsideDiscovery =
            Path.Combine(root, "outside-discovery");
        Directory.CreateDirectory(outsideDiscovery);
        File.WriteAllText(
            Path.Combine(outsideDiscovery, "data.cdb"),
            validCdb);
        string? junctionSession = null;
        await CheckImportFailureAsync(
            options,
            new FakeExternalProcessRunner(request =>
            {
                junctionSession = request.Arguments[2];
                CreateDirectoryJunction(
                    Path.Combine(junctionSession, "external-link"),
                    outsideDiscovery);
                return new ExternalProcessResult { Started = true, ExitCode = 0 };
            }),
            QuickBmsImportFailureKind.ExtractedCdbMissing,
            "recursive discovery ignores data.cdb behind a directory junction");
        Check(File.Exists(Path.Combine(outsideDiscovery, "data.cdb")),
            "junction discovery and cleanup do not touch the external target");
        CleanRetainedReparseSession(junctionSession, "external-link");

        string outsideFile =
            Path.Combine(root, "outside-file.cdb");
        File.WriteAllText(outsideFile, validCdb);
        string? symlinkSession = null;
        bool fileSymlinkCreated = false;

        try
        {
            await CheckImportFailureAsync(
                options,
                new FakeExternalProcessRunner(request =>
                {
                    symlinkSession = request.Arguments[2];
                    File.CreateSymbolicLink(
                        Path.Combine(symlinkSession, "data.cdb"),
                        outsideFile);
                    fileSymlinkCreated = true;
                    return new ExternalProcessResult { Started = true, ExitCode = 0 };
                }),
                QuickBmsImportFailureKind.ExtractedCdbInvalid,
                "data.cdb file symlink is rejected");
        }
        catch (Exception exception)
            when (!fileSymlinkCreated
                  &&
                  exception is UnauthorizedAccessException or IOException)
        {
            Console.WriteLine(
                "SKIP file-symlink regression: Windows symlink privilege is unavailable");
        }

        if (fileSymlinkCreated)
        {
            Check(File.Exists(outsideFile),
                "file-symlink rejection does not touch the external target");
            CleanRetainedReparseSession(symlinkSession, "data.cdb");
        }

        await CheckImportFailureAsync(
            options,
            new FakeExternalProcessRunner(_ =>
            {
                File.AppendAllText(package, "unexpected package change");
                return new ExternalProcessResult { Started = true, ExitCode = 0 };
            }),
            QuickBmsImportFailureKind.SourcePackageChanged,
            "a source-package identity change blocks promotion");
        File.WriteAllBytes(
            package,
            new byte[]
            {
                (byte)'P', (byte)'A', (byte)'K', 0,
                1, 2, 3, 4
            });

        string currentSuccessfulCdb = validCdb;
        bool stagingCdbWasCreated = false;
        Action? beforeSuccessfulExtraction = null;
        FakeExternalProcessRunner successfulRunner = new(request =>
        {
            beforeSuccessfulExtraction?.Invoke();
            beforeSuccessfulExtraction = null;
            string nested = Path.Combine(request.Arguments[2], "content", "db");
            Directory.CreateDirectory(nested);
            string stagedCdb =
                Path.Combine(nested, "data.cdb");
            File.WriteAllText(stagedCdb, currentSuccessfulCdb);
            stagingCdbWasCreated =
                File.Exists(stagedCdb);
            return new ExternalProcessResult { Started = true, ExitCode = 0 };
        });
        QuickBmsImportService service = CreateImportService(successfulRunner);
        QuickBmsImportResult first = await service.ImportAsync(options);
        string promotedCdb =
            Path.Combine(
                installation,
                "Extracted",
                "data.cdb");
        FileFingerprint promotedFingerprint =
            new FileFingerprintService().Calculate(
                promotedCdb);
        int requestsBeforeExistingFileRefusal =
            successfulRunner.Requests.Count;

        await CheckImportFailureAsync(
            options,
            successfulRunner,
            QuickBmsImportFailureKind.ExtractedCdbAlreadyExists,
            "an existing durable extraction is not replaced without explicit approval");
        Check(successfulRunner.Requests.Count == requestsBeforeExistingFileRefusal &&
              new FileFingerprintService().Calculate(promotedCdb) == promotedFingerprint,
            "cancelling replacement preserves the existing extracted file without running QuickBMS");

        currentSuccessfulCdb =
            "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"FishingDurationControl\",\"value\":10}]}]}";
        QuickBmsImportResult second =
            await service.ImportAsync(
                options,
                replaceExistingExtractedCdb: true);

        Check(first.Project.SourceProvenanceStatus == SourceProvenanceStatus.Verified &&
              first.Project.SourceCdbGenerationIdentity ==
                  first.Project.CurrentCdbContentIdentity,
            "initial QuickBMS import establishes equal authoritative source and current identities");
        Check(second.Project.UpdateCompatibilityReport?.Transition ==
                  SourceGenerationTransition.ChangedSourceGeneration,
            "different QuickBMS candidate is classified as a changed source generation");

        string secondSource = second.Project.SourceCdbGenerationIdentity!;
        ProjectMutationService secondMutation = new();
        GameplayPresetService secondPreset = new(
            secondMutation,
            new GameplayOperationStateService(secondMutation));
        ProjectOperationResult secondApplied = new ProjectOperationService().Execute(
            new GameplayPresetOperation(
                secondPreset,
                ProgressionType.FishingSpeed,
                "Fast"),
            second.Project);
        Check(secondApplied.Succeeded && second.Project.GameplayOperationStates.Count == 1,
            "same-source compatibility fixture records real gameplay state before local save");
        EntryModel secondEntry = second.Project.Sheets[0].Entries[0];
        new ProjectMutationService().EnsurePropertyByPath(
            secondEntry,
            "localMarker",
            new JValue(99));
        new JsonDataService().SaveProject(second.Project, promotedCdb);
        Check(second.Project.SourceCdbGenerationIdentity == secondSource &&
              second.Project.CurrentCdbContentIdentity != secondSource,
            "local save advances current identity while preserving QuickBMS source identity");

        QuickBmsImportResult third =
            await service.ImportAsync(
                options,
                replaceExistingExtractedCdb: true);
        Check(third.Project.SourceCdbGenerationIdentity == secondSource &&
              third.Project.CurrentCdbContentIdentity == secondSource,
            "same-source re-import after edits compares source to source");
        Check(third.Project.UpdateCompatibilityReport == null,
            "same-source re-import creates no false generation-change report");
        Check(third.Project.GameplayOperationStates.Count == 0 &&
              third.Project.HistoricalGameplayOperationStates.Count == 1 &&
              third.Project.HistoricalGameplayOperationStates[0]
                  .ProjectCompatibilityIdentity == secondSource,
            "same-source re-import revalidates incompatible target content and retains state as verified history");

        third.Project.EstablishPersistedIdentity(
            third.Project.CurrentCdbContentIdentity,
            null,
            SourceProvenanceStatus.Unknown);
        new JsonDataService().SaveGameplayOperationState(third.Project);
        QuickBmsImportResult unknownPrior = await service.ImportAsync(
            options,
            replaceExistingExtractedCdb: true);
        Check(unknownPrior.Project.UpdateCompatibilityReport?.Transition ==
                  SourceGenerationTransition.PreviousSourceGenerationUnknown,
            "bound Version 2 manifest with null source is not classified as confirmed changed generation");
        Check(unknownPrior.Project.HistoricalGameplayOperationStates.All(state =>
                  string.IsNullOrWhiteSpace(state.ProjectCompatibilityIdentity)),
            "QuickBMS retains null-source prior state only as unknown history");

        string statePath = promotedCdb +
            GameplayOperationStatePersistenceService.SidecarExtension;
        File.WriteAllText(statePath, "{bad-json");
        QuickBmsImportResult malformedPrior = await service.ImportAsync(
            options,
            replaceExistingExtractedCdb: true);
        Check(malformedPrior.Project.UpdateCompatibilityReport?.Transition ==
                  SourceGenerationTransition.PreviousSourceGenerationUnknown,
            "malformed prior sidecar produces unverified-prior compatibility result");

        File.WriteAllText(statePath, "{}");
        FileStream unreadableLock = new(
            statePath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None);
        beforeSuccessfulExtraction = unreadableLock.Dispose;
        QuickBmsImportResult unreadablePrior = await service.ImportAsync(
            options,
            replaceExistingExtractedCdb: true);
        Check(unreadablePrior.Project.UpdateCompatibilityReport?.Transition ==
                  SourceGenerationTransition.PreviousSourceGenerationUnknown,
            "unreadable prior sidecar produces unverified-prior compatibility result and safe replacement");

        ProjectMutationService vanillaMutation = new();
        GameplayPresetService vanillaPreset = new(
            vanillaMutation,
            new GameplayOperationStateService(vanillaMutation));
        Check(new ProjectOperationService().Execute(
                new GameplayPresetOperation(
                    vanillaPreset,
                    ProgressionType.FishingSpeed,
                    "Vanilla"),
                unreadablePrior.Project).Succeeded,
            "verified-history return fixture records a content-compatible active state");
        new JsonDataService().SaveGameplayOperationState(unreadablePrior.Project);

        currentSuccessfulCdb =
            "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"FishingDurationControl\",\"value\":20}]}]}";
        QuickBmsImportResult changedSource = await service.ImportAsync(
            options,
            replaceExistingExtractedCdb: true);
        Check(changedSource.Project.UpdateCompatibilityReport?.Transition ==
                  SourceGenerationTransition.ChangedSourceGeneration &&
              changedSource.Project.GameplayOperationStates.Count == 0 &&
              changedSource.Project.HistoricalGameplayOperationStates.Any(state =>
                  state.ProjectCompatibilityIdentity == secondSource),
            "changed source moves prior active state to verified non-restorable history");

        currentSuccessfulCdb =
            "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"FishingDurationControl\",\"value\":10}]}]}";
        QuickBmsImportResult returnedSource = await service.ImportAsync(
            options,
            replaceExistingExtractedCdb: true);
        Check(returnedSource.Project.SourceCdbGenerationIdentity == secondSource &&
              returnedSource.Project.GameplayOperationStates.Count == 1,
            "exact verified source return reactivates history only after full content validation");

        Check(first.Project.Sheets.Count == 1 &&
              first.Project.Sheets[0].Name == "constant",
            "valid extracted data uses the production project loader");
        Check(first.ExtractedCdbPath == Path.GetFullPath(promotedCdb) &&
              File.Exists(promotedCdb) &&
              first.Project.FileName == Path.GetFullPath(promotedCdb),
            "validated staging is promoted to the durable Extracted data.cdb and opened from that path");
        Check(first.ExtractedCdbFingerprint == promotedFingerprint,
            "the durable extracted CDB retains the validated staging fingerprint");
        Check(stagingCdbWasCreated &&
              second.ExtractedCdbFingerprint != promotedFingerprint &&
              returnedSource.Project.Sheets[0].Entries[0].SourceEntry?["value"]?.Value<int>() == 10,
            "approved replacement promotes the newly validated staging CDB over the existing durable file");
        Check(first.StagingDirectory != second.StagingDirectory &&
              first.SessionId != second.SessionId,
            "every import uses a unique staging workspace");
        Check(first.StagingCleaned && second.StagingCleaned &&
              !Directory.Exists(first.StagingDirectory) &&
              !Directory.Exists(second.StagingDirectory),
            "successful staging workspaces are cleaned");
        Check(successfulRunner.Requests.All(request =>
              request.Arguments.Count == 3 &&
              request.Arguments[0] == Path.GetFullPath(script) &&
              request.Arguments[1] == Path.GetFullPath(package) &&
              Path.IsPathFullyQualified(request.Arguments[2]) &&
              request.Arguments.All(argument =>
                  argument != "-w" && argument != "-r")),
            "process requests use exact absolute inputs without write or reimport flags");
        Check(first.SourcePackageFingerprint ==
              new FileFingerprintService().Calculate(package),
            "safe import leaves the source package unchanged");
        Check(Directory.Exists(stagingRoot) &&
              !Directory.EnumerateFileSystemEntries(stagingRoot).Any(),
            "failed and successful imports leave no stale staging output");
        Check(!File.Exists(promotedCdb + ".importing"),
            "successful promotion leaves no importing artifact");

        JsonDataService statePersistence = new();
        statePersistence.SaveGameplayOperationState(returnedSource.Project);
        Check(File.Exists(statePath),
            "a project imported from Wartales can persist gameplay-operation state beside its durable CDB");
        File.Delete(statePath);

        string blockedInstallation =
            Path.Combine(root, "promotion-blocked");
        Directory.CreateDirectory(blockedInstallation);
        File.WriteAllText(
            Path.Combine(blockedInstallation, "Wartales.exe"),
            "fixture executable");
        File.Copy(
            package,
            Path.Combine(blockedInstallation, "res.pak"));
        File.WriteAllText(
            Path.Combine(blockedInstallation, "Extracted"),
            "blocks directory creation");
        QuickBmsImportOptions blockedOptions = new()
        {
            WartalesInstallationDirectory = blockedInstallation,
            QuickBmsExecutablePath = executable,
            ShiroScriptPath = script,
            StagingRootDirectory = stagingRoot,
            ProcessTimeout = TimeSpan.FromSeconds(10)
        };
        ProjectModel activeProject = CreateProject(
            Sheet("constant", new JObject { ["id"] = "Active", ["value"] = 7 }));
        ProjectModel publishedProject = activeProject;

        try
        {
            QuickBmsImportResult blockedResult =
                await service.ImportAsync(blockedOptions);
            publishedProject = blockedResult.Project;
            throw new InvalidOperationException(
                "FAIL: a durable-promotion failure is surfaced");
        }
        catch (QuickBmsImportException exception)
        {
            Check(exception.FailureKind == QuickBmsImportFailureKind.PromotionFailed &&
                  ReferenceEquals(publishedProject, activeProject),
                "failed durable promotion leaves the active project unpublished and unchanged");
        }

        VerifyProjectPromotionAtomicity(root);

        Console.WriteLine(
            "PASS QuickBMS validation, process safety, durable promotion, replacement protection, state persistence, and production publication");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    QuickBmsImportService CreateImportService(
        IExternalProcessRunner runner) =>
        new(
            new JsonDataService(),
            new WartalesInstallationService(),
            new QuickBmsToolchainService(),
            runner,
            new ExtractionWorkspaceService(),
            new FileFingerprintService());

    async Task CheckImportFailureAsync(
        QuickBmsImportOptions testOptions,
        IExternalProcessRunner runner,
        QuickBmsImportFailureKind expectedKind,
        string message)
    {
        ProjectModel currentProject = CreateProject(
            Sheet("constant", new JObject { ["id"] = "Current", ["value"] = 9 }));
        ProjectModel promotedProject = currentProject;

        try
        {
            QuickBmsImportResult result =
                await CreateImportService(runner).ImportAsync(testOptions);
            promotedProject = result.Project;
        }
        catch (QuickBmsImportException exception)
        {
            Check(exception.FailureKind == expectedKind &&
                  ReferenceEquals(promotedProject, currentProject),
                message);
            return;
        }

        throw new InvalidOperationException($"FAIL: {message}");
    }
}

static async Task VerifyRealQuickBmsImport()
{
    QuickBmsImportOptions options =
        QuickBmsImportOptions.CreateDefault();
    string packagePath =
        Path.Combine(
            options.WartalesInstallationDirectory,
            "res.pak");
    string expectedPromotedCdb =
        Path.Combine(
            options.WartalesInstallationDirectory,
            "Extracted",
            "data.cdb");
    FileFingerprintService fingerprints = new();
    FileFingerprint before = fingerprints.Calculate(packagePath);
    QuickBmsImportResult result =
        await new QuickBmsImportService(
            new JsonDataService()).ImportAsync(
                options,
                replaceExistingExtractedCdb:
                    File.Exists(expectedPromotedCdb));
    FileFingerprint after = fingerprints.Calculate(packagePath);

    Check(before == after,
        "real extraction leaves res.pak byte-identical");
    Check(result.Project.Sheets.Count > 0,
        "real extracted CDB loads through the production loader");
    Check(result.ExtractedCdbPath == Path.GetFullPath(expectedPromotedCdb) &&
          result.Project.FileName == Path.GetFullPath(expectedPromotedCdb) &&
          File.Exists(expectedPromotedCdb),
        "real extraction is promoted and opened with a durable project identity");
    Check(!File.Exists(expectedPromotedCdb + ".importing"),
        "real promotion leaves no importing artifact");

    ProjectMutationService mutationService = new();
    GameplayOperationStateService stateService =
        new(mutationService);
    StartingResourcesService startingResources =
        new(mutationService, stateService);
    startingResources.Initialize(result.Project);
    ProjectMutationResult operationResult =
        startingResources.Apply(
            result.Project,
            new StartingResourcesSettings
            {
                Krowns = 1
            });
    Check(operationResult.WasModified &&
          result.Project.GameplayOperationStates.Any(state =>
              state.OperationType == ProgressionType.StartingResources),
        "a stateful gameplay operation executes on the real imported project");

    JsonDataService statePersistence = new();
    statePersistence.SaveGameplayOperationState(result.Project);
    string statePath =
        expectedPromotedCdb +
        GameplayOperationStatePersistenceService.SidecarExtension;
    Check(File.Exists(statePath),
        "real imported project can persist gameplay-operation state");
    File.Delete(statePath);

    Console.WriteLine($"SESSION={result.SessionId}");
    Console.WriteLine($"QUICKBMS={result.QuickBmsExecutablePath}");
    Console.WriteLine($"QUICKBMS_SIZE={result.QuickBmsExecutableFingerprint.Size}");
    Console.WriteLine($"QUICKBMS_SHA256={result.QuickBmsExecutableFingerprint.Sha256}");
    Console.WriteLine($"SCRIPT={result.ShiroScriptPath}");
    Console.WriteLine($"SCRIPT_SIZE={result.ShiroScriptFingerprint.Size}");
    Console.WriteLine($"SCRIPT_SHA256={result.ShiroScriptFingerprint.Sha256}");
    Console.WriteLine($"SOURCE={result.SourcePackagePath}");
    Console.WriteLine($"SOURCE_SIZE_BEFORE={before.Size}");
    Console.WriteLine($"SOURCE_SHA256_BEFORE={before.Sha256}");
    Console.WriteLine($"EXIT_CODE={result.ProcessExitCode}");
    Console.WriteLine($"CONTAINED_PROCESS_COUNT={result.ContainedProcessCount}");
    Console.WriteLine($"EXTRACTED_CDB={result.ExtractedCdbPath}");
    Console.WriteLine($"EXTRACTED_CDB_SIZE={result.ExtractedCdbFingerprint.Size}");
    Console.WriteLine($"EXTRACTED_CDB_SHA256={result.ExtractedCdbFingerprint.Sha256}");
    Console.WriteLine($"PROJECT_SHEETS={result.Project.Sheets.Count}");
    Console.WriteLine(
        $"PROJECT_SHEET_SAMPLE={string.Join(",", result.Project.Sheets.Take(10).Select(sheet => sheet.Name))}");
    Console.WriteLine($"SOURCE_SIZE_AFTER={after.Size}");
    Console.WriteLine($"SOURCE_SHA256_AFTER={after.Sha256}");
    Console.WriteLine($"STAGING={result.StagingDirectory}");
    Console.WriteLine($"STAGING_CLEANED={result.StagingCleaned}");
    Console.WriteLine($"PROJECT_FILE={result.Project.FileName}");
    Console.WriteLine($"GAMEPLAY_STATE_PERSISTENCE=PASSED");
    Console.WriteLine("REAL QUICKBMS IMPORT CHECK PASSED");
}

static async Task VerifyExternalProcessTermination()
{
    string selfExecutable =
        Path.ChangeExtension(
            System.Reflection.Assembly
                .GetExecutingAssembly()
                .Location,
            ".exe");
    ExternalProcessRunner runner = new();
    string markerRoot =
        Path.Combine(
            Path.GetTempPath(),
            $"WartalesEditor-process-tree-{Guid.NewGuid():N}");
    string timeoutChildIdFile = markerRoot + "-timeout-child.txt";
    string timeoutGrandchildIdFile = markerRoot + "-timeout-grandchild.txt";
    string cancellationChildIdFile = markerRoot + "-cancel-child.txt";
    string cancellationGrandchildIdFile = markerRoot + "-cancel-grandchild.txt";
    string rootExitChildIdFile = markerRoot + "-root-exit-child.txt";
    string rootExitGrandchildIdFile = markerRoot + "-root-exit-grandchild.txt";
    string[] markerFiles =
    {
        timeoutChildIdFile,
        timeoutGrandchildIdFile,
        cancellationChildIdFile,
        cancellationGrandchildIdFile,
        rootExitChildIdFile,
        rootExitGrandchildIdFile
    };

    try
    {
        Stopwatch timer = Stopwatch.StartNew();
        ExternalProcessResult timedOut =
            await runner.RunAsync(
                new ExternalProcessRequest
                {
                    ExecutablePath = selfExecutable,
                    Arguments = new[]
                    {
                        "--quickbms-process-tree-parent",
                        timeoutChildIdFile,
                        timeoutGrandchildIdFile
                    },
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Timeout = TimeSpan.FromSeconds(2)
                });
        timer.Stop();

        Check(
            timedOut.Started,
            $"production contained process starts successfully: {timedOut.StartError}");
        int timeoutChildId = ReadRecordedProcessId(timeoutChildIdFile);
        int timeoutGrandchildId = ReadRecordedProcessId(timeoutGrandchildIdFile);

        Check(timedOut.TimedOut &&
              !timedOut.TerminationFailed &&
              timedOut.ContainedProcessCount == 0 &&
              timedOut.ProcessId > 0 &&
              !IsProcessAlive(timedOut.ProcessId) &&
              !IsProcessAlive(timeoutChildId) &&
              !IsProcessAlive(timeoutGrandchildId) &&
              timer.Elapsed < TimeSpan.FromSeconds(15),
            "production timeout proves the contained parent, child, and grandchild exited within the bound");

        using CancellationTokenSource cancellation = new();
        cancellation.CancelAfter(TimeSpan.FromSeconds(2));
        ExternalProcessResult cancelled =
            await runner.RunAsync(
                new ExternalProcessRequest
                {
                    ExecutablePath = selfExecutable,
                    Arguments = new[]
                    {
                        "--quickbms-process-tree-parent",
                        cancellationChildIdFile,
                        cancellationGrandchildIdFile
                    },
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Timeout = TimeSpan.FromSeconds(30)
                },
                cancellation.Token);
        int cancellationChildId = ReadRecordedProcessId(cancellationChildIdFile);
        int cancellationGrandchildId =
            ReadRecordedProcessId(cancellationGrandchildIdFile);

        Check(cancelled.Started &&
              cancelled.Cancelled &&
              !cancelled.TerminationFailed &&
              cancelled.ContainedProcessCount == 0 &&
              cancelled.ProcessId > 0 &&
              !IsProcessAlive(cancelled.ProcessId) &&
              !IsProcessAlive(cancellationChildId) &&
              !IsProcessAlive(cancellationGrandchildId),
            "production cancellation proves the contained parent, child, and grandchild exited");

        Stopwatch rootExitTimer = Stopwatch.StartNew();
        ExternalProcessResult rootExitedFirst =
            await runner.RunAsync(
                new ExternalProcessRequest
                {
                    ExecutablePath = selfExecutable,
                    Arguments = new[]
                    {
                        "--quickbms-root-exits-first",
                        rootExitChildIdFile,
                        rootExitGrandchildIdFile
                    },
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Timeout = TimeSpan.FromSeconds(2)
                });
        rootExitTimer.Stop();
        int rootExitChildId = ReadRecordedProcessId(rootExitChildIdFile);
        int rootExitGrandchildId =
            ReadRecordedProcessId(rootExitGrandchildIdFile);

        Check(rootExitedFirst.Started &&
              rootExitedFirst.TimedOut &&
              !rootExitedFirst.TerminationFailed &&
              rootExitedFirst.ContainedProcessCount == 0 &&
              !IsProcessAlive(rootExitedFirst.ProcessId) &&
              !IsProcessAlive(rootExitChildId) &&
              !IsProcessAlive(rootExitGrandchildId) &&
              rootExitTimer.Elapsed >= TimeSpan.FromSeconds(1.5) &&
              rootExitTimer.Elapsed < TimeSpan.FromSeconds(15),
            "root exit is not mistaken for complete contained-tree exit");

        await CheckThrowsAsync<ArgumentOutOfRangeException>(
            () => runner.RunAsync(
                new ExternalProcessRequest
                {
                    ExecutablePath = selfExecutable,
                    Arguments = Array.Empty<string>(),
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Timeout = TimeSpan.Zero
                }),
            "invalid process timeout is rejected before process start");

        Console.WriteLine(
            "PASS production Job Object containment proves zero active parent/child/grandchild processes after timeout, cancellation, and root-first exit");
    }
    finally
    {
        foreach (string markerFile in markerFiles)
        {
            File.Delete(markerFile);
        }
    }
}

static int ReadRecordedProcessId(
    string filePath)
{
    Check(File.Exists(filePath),
        "process-tree helper recorded its child process ID");

    return int.Parse(
        File.ReadAllText(filePath),
        System.Globalization.CultureInfo.InvariantCulture);
}

static void VerifyStagingReparseSafety(
    string testRoot)
{
    ExtractionWorkspaceService service = new();
    string junctionTarget =
        Path.Combine(testRoot, "staging-junction-target");
    string stagingJunction =
        Path.Combine(testRoot, "staging-junction");
    Directory.CreateDirectory(junctionTarget);
    CreateDirectoryJunction(
        stagingJunction,
        junctionTarget);

    CheckImportFailure(
        () => service.Create(stagingJunction),
        QuickBmsImportFailureKind.StagingFailed,
        "staging root junction is rejected");
    Check(!Directory.EnumerateFileSystemEntries(junctionTarget).Any(),
        "staging root junction rejection creates no target session");
    Directory.Delete(stagingJunction);

    string safeRoot =
        Path.Combine(testRoot, "cleanup-reparse-root");
    ExtractionWorkspace workspace =
        service.Create(safeRoot);
    Directory.Delete(workspace.DirectoryPath);

    string cleanupTarget =
        Path.Combine(testRoot, "cleanup-reparse-target");
    Directory.CreateDirectory(cleanupTarget);
    string sentinel =
        Path.Combine(cleanupTarget, "sentinel.txt");
    File.WriteAllText(sentinel, "preserve");
    CreateDirectoryJunction(
        workspace.DirectoryPath,
        cleanupTarget);

    Check(!service.TryClean(workspace) &&
          File.Exists(sentinel),
        "cleanup refuses a replaced session junction and preserves its target");

    Directory.Delete(workspace.DirectoryPath);

    Console.WriteLine(
        "PASS staging root and cleanup reject junction/reparse redirection");
}

static void VerifyProjectPromotionAtomicity(
    string testRoot)
{
    LocalizationService localization = new();
    ReferenceDataService references =
        ReferenceDataService.Instance;
    MainViewModel viewModel =
        CreateMainViewModel(
            localization,
            references);
    TestMessageDialogService cancelReplacementDialog =
        new(showConfirmationResult: false);
    MainViewModel cancelReplacementViewModel =
        CreateMainViewModel(
            new LocalizationService(),
            references,
            cancelReplacementDialog);
    Check(!cancelReplacementViewModel.ConfirmReplaceExistingExtractedCdb() &&
          cancelReplacementDialog.ConfirmationCount == 1,
        "the existing Extracted file warning supports cancellation before import");
    ProjectModel projectA = CreateProject(
        Sheet(
            "constant",
            new JObject
            {
                ["id"] = "ProjectA",
                ["marker"] = "A"
            }));
    ProjectModel projectB = CreateProject(
        Sheet(
            "constant",
            new JObject
            {
                ["id"] = "ProjectB",
                ["marker"] = "B"
            }));
    string baselineLocalization =
        Path.Combine(testRoot, "baseline-localization.xml");
    File.WriteAllText(
        baselineLocalization,
        "<root><sheet><ProjectA><name>Project A</name></ProjectA></sheet></root>");
    localization.Load(baselineLocalization);
    references.Initialize(projectA);
    viewModel.Project = projectA;
    viewModel.CurrentFile = "ProjectA.cdb";

    string productionLocalization =
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "export_en.xml");
    byte[]? previousLocalizationFile =
        File.Exists(productionLocalization)
            ? File.ReadAllBytes(productionLocalization)
            : null;

    try
    {
        File.WriteAllText(
            productionLocalization,
            "<invalid");

        viewModel.PromoteLoadedProject(
            projectB,
            "ProjectB.cdb");
        Check(ReferenceEquals(viewModel.Project, projectB) &&
              viewModel.CurrentFile == "ProjectB.cdb" &&
              references.GetValues("constant", "marker")
                  .Any(value => value.Value == "B") &&
              references.GetValues("constant", "marker")
                  .All(value => value.Value != "A") &&
              localization.Contains("ProjectA") &&
              !localization.Contains("ProjectB"),
            "malformed executable-adjacent localization cannot block project promotion or replace application language state");

        File.WriteAllText(
            productionLocalization,
            "<root><sheet><ProjectB><name>Project B</name></ProjectB></sheet></root>");
        viewModel.PromoteLoadedProject(
            projectA,
            "ProjectA.cdb");
        Check(ReferenceEquals(viewModel.Project, projectA) &&
              viewModel.CurrentFile == "ProjectA.cdb" &&
              references.GetValues("constant", "marker")
                  .Any(value => value.Value == "A") &&
              localization.Contains("ProjectA") &&
              !localization.Contains("ProjectB"),
            "valid executable-adjacent localization is not a competing authority during project promotion");
    }
    finally
    {
        if (previousLocalizationFile == null)
        {
            File.Delete(productionLocalization);
        }
        else
        {
            File.WriteAllBytes(
                productionLocalization,
                previousLocalizationFile);
        }
    }

    Console.WriteLine(
        "PASS production promotion remains coherent and independent of executable-adjacent language files");
}

static MainViewModel CreateMainViewModel(
    LocalizationService localization,
    ReferenceDataService references,
    IMessageDialogService? messageDialogService = null,
    IFileDialogService? fileDialogService = null,
    LanguageDataService? languageDataService = null,
    WartalesInstallationService?
        wartalesInstallationService = null,
    QuickBmsImportOptions?
        quickBmsImportOptions = null)
{
    JsonDataService jsonDataService = new();
    ModificationSnapshotWorkflowService snapshotWorkflowService = new();
    ValidationService validationService =
        new(jsonDataService);
    ValidationWorkflowService validationWorkflowService =
        new(validationService);
    ProjectMutationService projectMutationService = new();
    ContentCreationService contentCreationService =
        new(projectMutationService);
    AddCampFacilitiesOperation addCampFacilitiesOperation =
        new(contentCreationService);
    UpgradeAllEquipmentOperation upgradeAllEquipmentOperation =
        new(contentCreationService);
    ProjectOperationTransactionService transactionService = new();
    ProjectOperationService operationService =
        new(
            new OperationValidatorProvider(),
            transactionService);
    ProfileOperationCaptureService profileCaptureService =
        new(
            new OperationValidatorProvider(),
            addCampFacilitiesOperation,
            upgradeAllEquipmentOperation);
    ModProfileService profileService =
        new(
            new ModificationSnapshotService(),
            profileCaptureService);

    LanguageDataService resolvedLanguageDataService =
        languageDataService
        ?? new LanguageDataService(
            localization,
            Path.Combine(
                Path.GetTempPath(),
                "WartalesEditorCompatibilityLanguageData",
                Guid.NewGuid().ToString("N")));

    return new MainViewModel(
        jsonDataService,
        new SearchService(),
        localization,
        new EditHistoryService(),
        new ModificationSnapshotService(),
        snapshotWorkflowService,
        new ChangeSummaryService(),
        new ModProfileLibraryService(),
        new ModProfileWorkflowService(
            profileService,
            new ModProfileSerializationService(),
            snapshotWorkflowService,
            new ProfileOperationResolver(
                addCampFacilitiesOperation,
                upgradeAllEquipmentOperation),
            operationService,
            transactionService),
        references,
        validationWorkflowService,
        new ValidationPresentationService(),
        operationService,
        transactionService,
        addCampFacilitiesOperation,
        upgradeAllEquipmentOperation,
        fileDialogService
        ?? new TestFileDialogService(),
        messageDialogService
        ?? new TestMessageDialogService(),
        resolvedLanguageDataService,
        wartalesInstallationService
        ?? new WartalesInstallationService(),
        quickBmsImportOptions
        ?? QuickBmsImportOptions.CreateDefault());
}

static bool IsProcessAlive(
    int processId)
{
    try
    {
        using Process process =
            Process.GetProcessById(processId);
        return !process.HasExited;
    }
    catch (ArgumentException)
    {
        return false;
    }
}

static async Task CheckThrowsAsync<TException>(
    Func<Task> action,
    string message)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"FAIL: {message}");
}

static void CreateDirectoryJunction(
    string linkPath,
    string targetPath)
{
    ProcessStartInfo startInfo =
        new()
        {
            FileName = Path.Combine(
                Environment.SystemDirectory,
                "cmd.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    startInfo.ArgumentList.Add("/d");
    startInfo.ArgumentList.Add("/c");
    startInfo.ArgumentList.Add("mklink");
    startInfo.ArgumentList.Add("/J");
    startInfo.ArgumentList.Add(linkPath);
    startInfo.ArgumentList.Add(targetPath);

    using Process process =
        Process.Start(startInfo)
        ?? throw new InvalidOperationException(
            "The junction helper did not start.");
    string standardError =
        process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"The junction helper failed: {standardError}");
    }
}

static void CleanRetainedReparseSession(
    string? sessionDirectory,
    string reparseEntryName)
{
    Check(!string.IsNullOrWhiteSpace(sessionDirectory),
        "retained reparse session path was recorded");
    string entry =
        Path.Combine(
            sessionDirectory!,
            reparseEntryName);
    FileAttributes attributes =
        File.GetAttributes(entry);

    if ((attributes & FileAttributes.Directory) != 0)
    {
        Directory.Delete(entry);
    }
    else
    {
        File.Delete(entry);
    }

    Directory.Delete(
        sessionDirectory!,
        recursive: true);
}

static void CheckImportFailure(
    Action action,
    QuickBmsImportFailureKind expectedKind,
    string message)
{
    try
    {
        action();
    }
    catch (QuickBmsImportException exception)
    {
        Check(exception.FailureKind == expectedKind, message);
        return;
    }

    throw new InvalidOperationException($"FAIL: {message}");
}

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException($"FAIL: {message}");
}

static void CheckThrows<TException>(
    Action action,
    string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"FAIL: {message}");
}

sealed class ThrowAfterPromotionHooks :
    ILanguageDataOperationHooks
{
    public void AfterCanonicalPromotion(
        string canonicalPath,
        string rollbackPath)
    {
        throw new InvalidOperationException(
            "Injected post-promotion publication failure.");
    }

    public void BeforeSuccessfulCleanup(
        string candidatePath,
        string rollbackPath)
    {
    }
}

sealed class MissingRollbackHooks :
    ILanguageDataOperationHooks
{
    public void AfterCanonicalPromotion(
        string canonicalPath,
        string rollbackPath)
    {
        File.Delete(rollbackPath);

        throw new InvalidOperationException(
            "Injected publication failure after rollback removal.");
    }

    public void BeforeSuccessfulCleanup(
        string candidatePath,
        string rollbackPath)
    {
    }
}

sealed class UnusableRollbackHooks :
    ILanguageDataOperationHooks,
    IDisposable
{
    private FileStream? rollbackLock;

    public void AfterCanonicalPromotion(
        string canonicalPath,
        string rollbackPath)
    {
        rollbackLock =
            new FileStream(
                rollbackPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None);

        throw new InvalidOperationException(
            "Injected publication failure with an unusable rollback.");
    }

    public void BeforeSuccessfulCleanup(
        string candidatePath,
        string rollbackPath)
    {
    }

    public void Dispose()
    {
        rollbackLock?.Dispose();
        rollbackLock = null;
    }
}

sealed class CleanupFailureHooks :
    ILanguageDataOperationHooks
{
    public void AfterCanonicalPromotion(
        string canonicalPath,
        string rollbackPath)
    {
    }

    public void BeforeSuccessfulCleanup(
        string candidatePath,
        string rollbackPath)
    {
        if (File.Exists(rollbackPath))
        {
            throw new IOException(
                "Injected rollback cleanup failure.");
        }
    }
}

sealed class ValidatorMismatchCollection : IReadOnlyCollection<string>
{
    private readonly string[] appliedValues;
    private readonly string[] validatedValues;
    private int enumerationCount;

    public ValidatorMismatchCollection(
        IEnumerable<string> appliedValues,
        IEnumerable<string> validatedValues)
    {
        this.appliedValues = appliedValues.ToArray();
        this.validatedValues = validatedValues.ToArray();
    }

    public int Count => appliedValues.Length;

    public IEnumerator<string> GetEnumerator()
    {
        enumerationCount++;
        return (enumerationCount <= 3
                ? appliedValues
                : validatedValues)
            .AsEnumerable()
            .GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
        GetEnumerator();
}

sealed class FakeExternalProcessRunner : IExternalProcessRunner
{
    private readonly Func<ExternalProcessRequest, ExternalProcessResult> run;

    public FakeExternalProcessRunner(
        Func<ExternalProcessRequest, ExternalProcessResult> run)
    {
        this.run = run;
    }

    public List<ExternalProcessRequest> Requests { get; } = new();

    public Task<ExternalProcessResult> RunAsync(
        ExternalProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(run(request));
    }
}

sealed class TestFileDialogService : IFileDialogService
{
    private readonly string? openFileName;
    private readonly string? folderName;

    public TestFileDialogService(
        string? openFileName = null,
        string? folderName = null)
    {
        this.openFileName = openFileName;
        this.folderName = folderName;
    }

    public int OpenCallCount { get; private set; }

    public int FolderCallCount { get; private set; }

    public string? LastOpenInitialFileName { get; private set; }

    public string? LastOpenInitialDirectory { get; private set; }

    public string? ShowOpenFileDialog(
        string filter,
        string? initialFileName = null)
    {
        OpenCallCount++;
        LastOpenInitialFileName =
            initialFileName;
        LastOpenInitialDirectory = null;
        return openFileName;
    }

    public string? ShowOpenFileDialog(
        string filter,
        string? initialFileName,
        string? initialDirectory)
    {
        OpenCallCount++;
        LastOpenInitialFileName =
            initialFileName;
        LastOpenInitialDirectory =
            initialDirectory;
        return openFileName;
    }

    public string? ShowSaveFileDialog(
        string filter,
        string? initialFileName = null) =>
        null;

    public string? ShowOpenFolderDialog(
        string title,
        string? initialDirectory = null)
    {
        FolderCallCount++;
        return folderName;
    }
}

sealed class TestMessageDialogService : IMessageDialogService
{
    private readonly bool showConfirmationResult;

    public TestMessageDialogService(
        bool showConfirmationResult = true)
    {
        this.showConfirmationResult =
            showConfirmationResult;
    }

    public int ConfirmationCount { get; private set; }

    public int ErrorCount { get; private set; }

    public string? LastInformation { get; private set; }

    public string? LastWarning { get; private set; }

    public string? LastError { get; private set; }

    public void ShowInformation(
        string message,
        string title)
    {
        LastInformation = message;
    }

    public void ShowWarning(
        string message,
        string title)
    {
        LastWarning = message;
    }

    public void ShowError(
        string message,
        string title)
    {
        ErrorCount++;
        LastError = message;
    }

    public bool ShowConfirmation(
        string message,
        string title)
    {
        ConfirmationCount++;
        return showConfirmationResult;
    }

    public UnsavedChangesResult ShowUnsavedChanges(
        string message,
        string title) =>
        UnsavedChangesResult.Discard;
}

sealed class MixedPropertyRemovalTestOperation : IProjectOperation
{
    private readonly ProjectMutationService mutationService;
    private readonly string sheetName;
    private readonly string entryId;

    public MixedPropertyRemovalTestOperation(
        ProjectMutationService mutationService,
        string sheetName,
        string entryId)
    {
        this.mutationService = mutationService;
        this.sheetName = sheetName;
        this.entryId = entryId;
    }

    public string Name => "Property removal test";

    public string Description => "Exercises removal rollback.";

    public bool CanExecute(ProjectModel project) => true;

    public ProjectOperationResult Execute(ProjectModel project)
    {
        SheetModel sheet = mutationService.FindSheet(project, sheetName);
        EntryModel entry = mutationService.FindEntry(sheet, entryId);
        ProjectMutationResult result = mutationService.EnsurePropertyByPath(
            entry,
            "a",
            new JValue(10));
        result.Merge(mutationService.EnsurePropertyByPath(
            entry,
            "d",
            new JValue(4)));
        result.Merge(mutationService.RemovePropertyByPath(
            entry,
            "b"));
        return ProjectOperationResult.Success(result);
    }
}

sealed class TestMutationOperation : IProjectOperation
{
    private readonly Func<ProjectModel, ProjectMutationResult> execute;

    public TestMutationOperation(
        string name,
        Func<ProjectModel, ProjectMutationResult> execute)
    {
        Name = name;
        this.execute = execute;
    }

    public string Name { get; }

    public string Description => Name;

    public bool CanExecute(ProjectModel project) => true;

    public ProjectOperationResult Execute(ProjectModel project) =>
        ProjectOperationResult.Success(execute(project));
}

sealed class RejectPropertyRemovalValidatorProvider :
    IOperationValidatorProvider
{
    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult) =>
        OperationValidationResult.Failure(
            "Forced property-removal validation failure.");
}
