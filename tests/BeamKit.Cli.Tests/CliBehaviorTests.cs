using BeamKit.Reporting;
using Xunit;

namespace BeamKit.Cli.Tests;

public sealed class CliBehaviorTests
{
    [Fact]
    public void ParserReadsQaInputPaths()
    {
        var options = CliOptions.Parse(new[]
        {
            "qa",
            "--plan",
            "plan.json",
            "--template",
            "template.json",
            "--dictionary",
            "dictionary.json",
            "--format",
            "json"
        });

        Assert.Equal("qa", options.Command);
        Assert.Equal("plan.json", options.PlanPath);
        Assert.Equal("template.json", options.TemplatePath);
        Assert.Equal("dictionary.json", options.NamingDictionaryPath);
        Assert.Equal(ReportFormat.Json, options.Format);
    }

    [Fact]
    public void ParserReadsDoseCalculationOptions()
    {
        var options = CliOptions.Parse(new[]
        {
            "dose-calc",
            "--total-dose-gy",
            "70",
            "--fractions",
            "35",
            "--alpha-beta",
            "10",
            "--equivalent-fractions",
            "30"
        });

        Assert.Equal("dose-calc", options.Command);
        Assert.Equal(70m, options.TotalDoseGy);
        Assert.Equal(35, options.Fractions);
        Assert.Equal(10m, options.AlphaBetaGy);
        Assert.Equal(30, options.EquivalentFractions);
    }

    [Fact]
    public void ParserReadsCheckRulePackAndCaseOptions()
    {
        var options = CliOptions.Parse(new[]
        {
            "check",
            "--case",
            "head-neck-pass",
            "--rule-pack",
            "pack.json",
            "--capture-writeup"
        });

        Assert.Equal("check", options.Command);
        Assert.Equal("head-neck-pass", options.SyntheticCaseId);
        Assert.Equal("pack.json", options.RulePackPath);
        Assert.True(options.CaptureWriteUp);
    }

    [Fact]
    public void ParserReadsRulePackSubcommands()
    {
        var validate = CliOptions.Parse(new[] { "rule-pack", "validate", "--rule-pack", "pack.json" });
        var test = CliOptions.Parse(new[] { "rule-pack", "test", "--case", "head-neck-pass" });
        var diff = CliOptions.Parse(new[]
        {
            "rule-pack",
            "diff",
            "--old-rule-pack",
            "old.json",
            "--new-rule-pack",
            "new.json"
        });
        var addCheck = CliOptions.Parse(new[]
        {
            "rule-pack",
            "add-check",
            "--rule-pack",
            "pack.json",
            "--id",
            "dose.grid",
            "--title",
            "Dose grid",
            "--type",
            "dose-grid-max-spacing",
            "--parameter",
            "maxSpacingMm=2.5"
        });

        Assert.Equal("rule-pack-validate", validate.Command);
        Assert.Equal("pack.json", validate.RulePackPath);
        Assert.Equal("rule-pack-test", test.Command);
        Assert.Equal("head-neck-pass", test.SyntheticCaseId);
        Assert.Equal("rule-pack-diff", diff.Command);
        Assert.Equal("old.json", diff.RulePackPath);
        Assert.Equal("new.json", diff.ComparisonRulePackPath);
        Assert.Equal("rule-pack-add-check", addCheck.Command);
        Assert.Equal("dose.grid", addCheck.CheckId);
        Assert.Equal("maxSpacingMm=2.5", Assert.Single(addCheck.CheckParameters));
    }

    [Fact]
    public void ParserReadsProtocolSubcommands()
    {
        var validate = CliOptions.Parse(new[] { "rtpx", "validate", "--rtpx", "rtpx.json" });
        var compile = CliOptions.Parse(new[]
        {
            "rtpx",
            "compile",
            "--rtpx",
            "rtpx.json",
            "--output",
            "rule-pack"
        });
        var lintWord = CliOptions.Parse(new[] { "rtpx", "lint-word", "--docx", "protocol.docx" });
        var extractWord = CliOptions.Parse(new[]
        {
            "protocol",
            "extract-word",
            "--word",
            "protocol.docx",
            "--output",
            "rtpx.json"
        });

        Assert.Equal("rtpx-validate", validate.Command);
        Assert.Equal("rtpx.json", validate.ProtocolPath);
        Assert.Equal("rtpx-compile", compile.Command);
        Assert.Equal("rtpx.json", compile.ProtocolPath);
        Assert.Equal("rule-pack", compile.OutputPath);
        Assert.Equal("rtpx-lint-word", lintWord.Command);
        Assert.Equal("protocol.docx", lintWord.DocxPath);
        Assert.Equal("protocol-extract-word", extractWord.Command);
        Assert.Equal("protocol.docx", extractWord.DocxPath);
        Assert.Equal("rtpx.json", extractWord.OutputPath);
    }

    [Fact]
    public void ParserReadsCiRunOptions()
    {
        var options = CliOptions.Parse(new[]
        {
            "ci",
            "run",
            "--case",
            "head-neck-pass",
            "--branch",
            "main",
            "--commit",
            "abc123",
            "--build-id",
            "build-1"
        });

        Assert.Equal("ci-run", options.Command);
        Assert.Equal("head-neck-pass", options.SyntheticCaseId);
        Assert.Equal("main", options.Branch);
        Assert.Equal("abc123", options.Commit);
        Assert.Equal("build-1", options.BuildId);
    }

    [Fact]
    public void ParserReadsAssignmentRecommendationOptions()
    {
        var options = CliOptions.Parse(new[]
        {
            "assignment",
            "recommend",
            "--disease-site",
            "Head and Neck",
            "--required-skill",
            "VMAT",
            "--required-skill",
            "SBRT",
            "--roster",
            "staff.json",
            "--role",
            "Physicist",
            "--complexity",
            "5",
            "--priority",
            "4",
            "--due-date",
            "2026-07-10"
        });

        Assert.Equal("assignment-recommend", options.Command);
        Assert.Equal("Head and Neck", options.DiseaseSite);
        Assert.Equal(new[] { "VMAT", "SBRT" }, options.RequiredSkills);
        Assert.Equal("staff.json", options.StaffRosterPath);
        Assert.Equal("Physicist", Assert.Single(options.AssignmentRoles));
        Assert.Equal(5, options.ComplexityScore);
        Assert.Equal(4, options.Priority);
        Assert.Equal(new DateOnly(2026, 7, 10), options.DueDate);
    }

    [Fact]
    public void ParserReadsAssignmentTeamOptions()
    {
        var options = CliOptions.Parse(new[]
        {
            "assignment",
            "recommend-team",
            "--disease-site",
            "Lung",
            "--physician",
            "Dr Gray",
            "--role",
            "Dosimetrist",
            "--role",
            "Physicist"
        });

        Assert.Equal("assignment-recommend-team", options.Command);
        Assert.Equal("Lung", options.DiseaseSite);
        Assert.Equal("Dr Gray", options.Physician);
        Assert.Equal(new[] { "Dosimetrist", "Physicist" }, options.AssignmentRoles);
    }

    [Fact]
    public void ParserReadsCaseIntelligenceOptions()
    {
        var options = CliOptions.Parse(new[]
        {
            "intelligence",
            "case",
            "--case",
            "lung-sbrt-pass",
            "--priority",
            "5",
            "--due-date",
            "2026-07-12",
            "--format",
            "json"
        });

        Assert.Equal("intelligence-case", options.Command);
        Assert.Equal("lung-sbrt-pass", options.SyntheticCaseId);
        Assert.Equal(5, options.Priority);
        Assert.Equal(new DateOnly(2026, 7, 12), options.DueDate);
        Assert.Equal(ReportFormat.Json, options.Format);
    }

    [Fact]
    public void ParserReadsEsapiSnapshotValidateSubcommand()
    {
        var options = CliOptions.Parse(new[]
        {
            "esapi-snapshot",
            "validate",
            "--esapi-snapshot",
            "snapshot.json"
        });

        Assert.Equal("esapi-snapshot-validate", options.Command);
        Assert.Equal("snapshot.json", options.EsapiSnapshotPath);
    }

    [Fact]
    public void ParserReadsStructureRingOptions()
    {
        var options = CliOptions.Parse(new[]
        {
            "structure-rings",
            "--ptv",
            "PTV_7000",
            "--ring",
            "1:0.2:1.0"
        });

        Assert.Equal("structure-rings", options.Command);
        Assert.Equal("PTV_7000", options.PtvName);
        Assert.Equal("1:0.2:1.0", Assert.Single(options.RingDefinitions));
    }

    [Fact]
    public void ParserReadsRuleCatalogFilters()
    {
        var options = CliOptions.Parse(new[]
        {
            "rule-catalog",
            "--catalog",
            "rules.json",
            "--disease-site",
            "Head and Neck",
            "--physician",
            "Dr Example",
            "--tag",
            "baseline"
        });

        Assert.Equal("rule-catalog", options.Command);
        Assert.Equal("rules.json", options.RuleCatalogPath);
        Assert.Equal("Head and Neck", options.DiseaseSite);
        Assert.Equal("Dr Example", options.Physician);
        Assert.Equal("baseline", Assert.Single(options.Tags));
    }

    [Fact]
    public void ParserReadsPlanCheckAndMachineProfileOptions()
    {
        var options = CliOptions.Parse(new[]
        {
            "plan-check",
            "--plan",
            "plan.json",
            "--check-catalog",
            "checks.json",
            "--machine-profile",
            "machine.json",
            "--format",
            "json"
        });

        Assert.Equal("plan-check", options.Command);
        Assert.Equal("plan.json", options.PlanPath);
        Assert.Equal("checks.json", options.PlanCheckCatalogPath);
        Assert.Equal("machine.json", options.MachineProfilePath);
        Assert.Equal(ReportFormat.Json, options.Format);
    }

    [Fact]
    public void ParserReadsEsapiSnapshotPlanInput()
    {
        var options = CliOptions.Parse(new[]
        {
            "plan-check",
            "--esapi-snapshot",
            "snapshot.json"
        });

        Assert.Equal("plan-check", options.Command);
        Assert.Equal("snapshot.json", options.EsapiSnapshotPath);
    }

    [Fact]
    public void ParserReadsMetricOptions()
    {
        var options = CliOptions.Parse(new[]
        {
            "metrics",
            "--metric",
            "D95%",
            "--metric-structure",
            "PTV_7000"
        });

        Assert.Equal("metrics", options.Command);
        Assert.Equal("D95%", options.MetricExpression);
        Assert.Equal("PTV_7000", options.MetricStructureName);
    }

    [Fact]
    public void ParserReadsPlanIntegrityQaPlanPath()
    {
        var options = CliOptions.Parse(new[]
        {
            "plan-integrity",
            "--plan",
            "treatment.json",
            "--qa-plan",
            "qa.json"
        });

        Assert.Equal("plan-integrity", options.Command);
        Assert.Equal("treatment.json", options.PlanPath);
        Assert.Equal("qa.json", options.QaPlanPath);
    }

    [Fact]
    public void ParserReadsWriteUpCaptureOptions()
    {
        var options = CliOptions.Parse(new[]
        {
            "writeup",
            "capture",
            "--export",
            "record-and-verify:ARIA:PLAN-1:V1:dosimetry",
            "--document",
            "Plan packet:html",
            "--attest",
            "documents-printed=true",
            "--ct-imported",
            "--optimization-finished",
            "--physics-qa-complete",
            "--physician-approved",
            "--treatment-ready"
        });

        Assert.Equal("writeup-capture", options.Command);
        Assert.Equal("record-and-verify:ARIA:PLAN-1:V1:dosimetry", Assert.Single(options.ExportRecords));
        Assert.Equal("Plan packet:html", Assert.Single(options.DocumentRecords));
        Assert.Equal("documents-printed=true", Assert.Single(options.Attestations));
        Assert.True(options.CtImported);
        Assert.True(options.OptimizationFinished);
        Assert.True(options.PhysicsQaComplete);
        Assert.True(options.PhysicianApprovalComplete);
        Assert.True(options.TreatmentReady);
    }

    [Fact]
    public void ParserReadsWriteUpVerifyManifestPath()
    {
        var options = CliOptions.Parse(new[] { "writeup", "verify", "--manifest", "writeup.json" });

        Assert.Equal("writeup-verify", options.Command);
        Assert.Equal("writeup.json", options.ManifestPath);
    }

    [Fact]
    public void ProgramRunsDoseCalculationCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "dose-calc", "--total-dose-gy", "70", "--fractions", "35", "--alpha-beta", "10" });

            Assert.Equal(0, exitCode);
            Assert.Contains("BED: 84 Gy", output.ToString());
            Assert.Contains("EQD2: 70 Gy", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsStructureRingCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "structure-rings", "--ptv", "PTV_7000" });

            Assert.Equal(0, exitCode);
            Assert.Contains("Z_PTV_7000Ring1", output.ToString());
            Assert.Contains("1.2 cm", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsRuleCatalogCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "rule-catalog", "--disease-site", "Head and Neck" });

            Assert.Equal(0, exitCode);
            Assert.Contains("Synthetic clinical rule catalog", output.ToString());
            Assert.Contains("goal.ptv.d95", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsPlanCheckCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "plan-check" });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit Plan Check", output.ToString());
            Assert.Contains("target.d95", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsCheckCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "check", "--case", "head-neck-pass" });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit Check Report", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("Status: `Pass`", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsRulePackValidateCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "rule-pack", "validate" });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit Rule-Pack Validation", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("sha256:", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsRulePackTestCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "rule-pack", "test" });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit Rule-Pack Tests", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("head-neck-cord-fail", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsRulePackScaffoldAndDoctorCommands()
    {
        var directory = Path.Combine(Path.GetTempPath(), "beamkit-cli-rulepack-tests", Guid.NewGuid().ToString("N"));
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var scaffoldOutput = new StringWriter();
            using var scaffoldError = new StringWriter();
            Console.SetOut(scaffoldOutput);
            Console.SetError(scaffoldError);

            var scaffoldExitCode = Program.Main(new[]
            {
                "rule-pack",
                "new",
                "--disease-site",
                "breast",
                "--institution",
                "Synthetic",
                "--output",
                directory
            });

            Assert.Equal(0, scaffoldExitCode);
            Assert.Contains("BeamKit Rule-Pack Scaffold", scaffoldOutput.ToString(), StringComparison.Ordinal);
            var manifestPath = Path.Combine(directory, "beamkit-rule-pack.json");
            Assert.True(File.Exists(manifestPath));

            using var doctorOutput = new StringWriter();
            using var doctorError = new StringWriter();
            Console.SetOut(doctorOutput);
            Console.SetError(doctorError);

            var doctorExitCode = Program.Main(new[]
            {
                "rule-pack",
                "doctor",
                "--rule-pack",
                manifestPath
            });

            Assert.Equal(0, doctorExitCode);
            Assert.Contains("BeamKit Rule-Pack Doctor", doctorOutput.ToString(), StringComparison.Ordinal);
            Assert.Contains("Healthy: Yes", doctorOutput.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void ProgramRunsProtocolValidateAndCompileCommands()
    {
        var directory = Path.Combine(Path.GetTempPath(), "beamkit-cli-protocol-tests", Guid.NewGuid().ToString("N"));
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var validateOutput = new StringWriter();
            using var validateError = new StringWriter();
            Console.SetOut(validateOutput);
            Console.SetError(validateError);

            var validateExitCode = Program.Main(new[]
            {
                "rtpx",
                "validate",
                "--rtpx",
                SampleProtocolPath()
            });

            Assert.Equal(0, validateExitCode);
            Assert.Contains("BeamKit RT-PX Validation", validateOutput.ToString(), StringComparison.Ordinal);
            Assert.Contains("Valid: Yes", validateOutput.ToString(), StringComparison.Ordinal);

            using var compileOutput = new StringWriter();
            using var compileError = new StringWriter();
            Console.SetOut(compileOutput);
            Console.SetError(compileError);

            var compileExitCode = Program.Main(new[]
            {
                "rtpx",
                "compile",
                "--rtpx",
                SampleProtocolPath(),
                "--output",
                directory
            });

            Assert.Equal(0, compileExitCode);
            Assert.Contains("BeamKit RT-PX Compile", compileOutput.ToString(), StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(directory, "beamkit-rule-pack.json")));
            Assert.True(File.Exists(Path.Combine(directory, "clinical-rules.json")));
            Assert.True(File.Exists(Path.Combine(directory, "plan-checks.json")));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void ProgramRunsRulePackDiffCommandWithDiffHeading()
    {
        var root = Path.Combine(Path.GetTempPath(), "beamkit-cli-rulepack-diff-tests", Guid.NewGuid().ToString("N"));
        var oldDirectory = Path.Combine(root, "old");
        var newDirectory = Path.Combine(root, "new");
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);

            Assert.Equal(0, Program.Main(new[] { "rule-pack", "new", "--disease-site", "lung-sbrt", "--institution", "Synthetic", "--output", oldDirectory }));
            Assert.Equal(0, Program.Main(new[] { "rule-pack", "new", "--disease-site", "prostate", "--institution", "Synthetic", "--output", newDirectory }));

            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[]
            {
                "rule-pack",
                "diff",
                "--old-rule-pack",
                Path.Combine(oldDirectory, "beamkit-rule-pack.json"),
                "--new-rule-pack",
                Path.Combine(newDirectory, "beamkit-rule-pack.json")
            });

            Assert.Equal(2, exitCode);
            Assert.Contains("BeamKit Rule-Pack Diff", output.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("BeamKit Rule-Pack Changelog", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void ProgramRunsCiRunCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[]
            {
                "ci",
                "run",
                "--case",
                "head-neck-pass",
                "--branch",
                "main",
                "--commit",
                "abc123",
                "--build-id",
                "build-1"
            });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit CI Run", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("sha256:", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("Build ID: build-1", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsAssignmentRecommendCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[]
            {
                "assignment",
                "recommend",
                "--disease-site",
                "Head and Neck",
                "--required-skill",
                "VMAT",
                "--complexity",
                "4"
            });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit Assignment Recommendation", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("Jane Doe", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsAssignmentRecommendCommandWithRosterFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "beamkit-cli-roster-tests", Guid.NewGuid().ToString("N"));
        var rosterPath = Path.Combine(directory, "staff.json");
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(rosterPath, """
                {
                  "name": "CLI roster",
                  "staff": [
                    {
                      "id": "custom-dosimetrist",
                      "displayName": "Custom Dosimetrist",
                      "role": "Dosimetrist",
                      "skills": [ "VMAT", "Head and Neck" ],
                      "preferredDiseaseSites": [ "Head and Neck" ],
                      "activeCaseCount": 1,
                      "maxActiveCaseCount": 8
                    }
                  ]
                }
                """);

            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[]
            {
                "assignment",
                "recommend",
                "--roster",
                rosterPath,
                "--disease-site",
                "Head and Neck",
                "--required-skill",
                "VMAT",
                "--due-date",
                dueDate
            });

            Assert.Equal(0, exitCode);
            Assert.Contains("Custom Dosimetrist", output.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("Jane Doe", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void ProgramRunsAssignmentTeamCommandWithInferredCaseIntelligence()
    {
        var directory = Path.Combine(Path.GetTempPath(), "beamkit-cli-intelligent-assignment-tests", Guid.NewGuid().ToString("N"));
        var rosterPath = Path.Combine(directory, "staff.json");
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(rosterPath, """
                {
                  "name": "CLI intelligence roster",
                  "staff": [
                    {
                      "id": "lung-dosimetrist",
                      "displayName": "Lung SBRT Dosimetrist",
                      "role": "Dosimetrist",
                      "skills": [ "VMAT", "SBRT", "Lung" ],
                      "preferredDiseaseSites": [ "Lung" ],
                      "activeCaseCount": 1,
                      "maxActiveCaseCount": 8,
                      "maxComplexityScore": 5
                    },
                    {
                      "id": "sbrt-physicist",
                      "displayName": "SBRT Physicist",
                      "role": "Physicist",
                      "skills": [ "VMAT", "SBRT", "Lung", "Machine QA" ],
                      "preferredDiseaseSites": [ "Lung" ],
                      "activeCaseCount": 2,
                      "maxActiveCaseCount": 10,
                      "maxComplexityScore": 5
                    }
                  ]
                }
                """);

            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[]
            {
                "assignment",
                "recommend-team",
                "--case",
                "lung-sbrt-pass",
                "--roster",
                rosterPath,
                "--due-date",
                dueDate
            });

            var report = output.ToString();
            Assert.Equal(0, exitCode);
            Assert.Contains("Predicted complexity", report, StringComparison.Ordinal);
            Assert.Contains("Inferred skills", report, StringComparison.Ordinal);
            Assert.Contains("SBRT", report, StringComparison.Ordinal);
            Assert.Contains("Lung SBRT Dosimetrist", report, StringComparison.Ordinal);
            Assert.Contains("SBRT Physicist", report, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void ProgramReturnsTwoForFailingCheckCase()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "check", "--case", "head-neck-cord-fail" });

            Assert.Equal(2, exitCode);
            Assert.Contains("BeamKit Check Report", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("cord.max", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramListsSyntheticCases()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "cases" });

            Assert.Equal(0, exitCode);
            Assert.Contains("head-neck-pass", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("lung-sbrt-pass", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsMetricsCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "metrics" });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit Metrics", output.ToString());
            Assert.Contains("| CI |", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsDeliverabilityCommand()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "deliverability" });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit Deliverability", output.ToString());
            Assert.Contains("deliverability.beam.min-mu", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void ProgramRunsPlanIntegrityCommand()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, TestPlanJson);
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "plan-integrity", "--plan", path, "--qa-plan", path });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit Plan Integrity", output.ToString());
            Assert.Contains("Changes: 0", output.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            File.Delete(path);
        }
    }

    [Fact]
    public void ProgramRunsWriteUpCaptureAndVerifyCommands()
    {
        var manifestPath = Path.GetTempFileName();
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var captureOutput = new StringWriter();
            using var captureError = new StringWriter();
            Console.SetOut(captureOutput);
            Console.SetError(captureError);

            var captureExitCode = Program.Main(new[]
            {
                "writeup",
                "capture",
                "--format",
                "json",
                "--output",
                manifestPath,
                "--export",
                "record-and-verify:ARIA:HN-SYN-001:V1:dosimetry",
                "--document",
                "Plan write-up:html",
                "--attest",
                "documents-printed=true",
                "--ct-imported",
                "--optimization-finished",
                "--physics-qa-complete",
                "--physician-approved",
                "--treatment-ready"
            });

            Assert.Equal(0, captureExitCode);
            Assert.Contains("planFingerprint", File.ReadAllText(manifestPath), StringComparison.Ordinal);

            using var verifyOutput = new StringWriter();
            using var verifyError = new StringWriter();
            Console.SetOut(verifyOutput);
            Console.SetError(verifyError);

            var verifyExitCode = Program.Main(new[]
            {
                "writeup",
                "verify",
                "--manifest",
                manifestPath
            });

            Assert.Equal(0, verifyExitCode);
            Assert.Contains("BeamKit Write-Up Verification", verifyOutput.ToString(), StringComparison.Ordinal);
            Assert.Contains("Status: `Current`", verifyOutput.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            File.Delete(manifestPath);
        }
    }

    [Fact]
    public void ProgramRunsMetricsFromEsapiSnapshot()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, TestEsapiSnapshotJson);
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "metrics", "--esapi-snapshot", path });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit Metrics", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("HN-SYN-001", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            File.Delete(path);
        }
    }

    [Fact]
    public void ProgramValidatesEsapiSnapshot()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, TestEsapiSnapshotJson);
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "esapi-snapshot", "validate", "--esapi-snapshot", path });

            Assert.Equal(0, exitCode);
            Assert.Contains("BeamKit ESAPI Snapshot Validation", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("Errors: 0", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            File.Delete(path);
        }
    }

    [Fact]
    public void ProgramRejectsBothPlanAndEsapiSnapshotInputs()
    {
        var planPath = Path.GetTempFileName();
        var snapshotPath = Path.GetTempFileName();
        File.WriteAllText(planPath, TestPlanJson);
        File.WriteAllText(snapshotPath, TestEsapiSnapshotJson);
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "plan-check", "--plan", planPath, "--esapi-snapshot", snapshotPath });

            Assert.Equal(1, exitCode);
            Assert.Contains("Use only one of --plan, --esapi-snapshot, or --case", error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            File.Delete(planPath);
            File.Delete(snapshotPath);
        }
    }

    [Fact]
    public void ParserRejectsUnknownOption()
    {
        Assert.Throws<ArgumentException>(() => CliOptions.Parse(new[] { "qa", "--unknown" }));
    }

    [Fact]
    public void PlanJsonLoaderReadsPlanWithBeamsAndGoals()
    {
        var plan = PlanJsonLoader.FromJson(TestPlanJson);

        Assert.Equal("Plan", plan.Id);
        Assert.Single(plan.Beams);
        Assert.Single(plan.ClinicalGoals);
        Assert.Equal(52m, plan.FindDoseStatistics("PTV")?.GetMetric("maxdosegy"));
        Assert.Equal("6X", plan.Prescription.RequestedEnergy);
        Assert.Equal("SyntheticAAA", plan.Dose?.CalculationModel);
        Assert.Equal("SYN-AAA-6X", plan.Beams[0].BeamModelId);
    }

    [Fact]
    public void PlanJsonLoaderRejectsMissingPrescription()
    {
        var json = """
            {
              "patient": { "id": "SYN-001", "displayName": "Synthetic" },
              "plan": { "id": "Plan", "courseId": "C1" }
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => PlanJsonLoader.FromJson(json));

        Assert.Contains("prescription", exception.Message);
    }

    [Fact]
    public void ProgramReturnsOneForMalformedPlanJson()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "{");
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Program.Main(new[] { "qa", "--plan", path });

            Assert.Equal(1, exitCode);
            Assert.Contains("beamkit:", error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            File.Delete(path);
        }
    }

    private static string SampleProtocolPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "samples",
            "rtpx",
            "lung-sbrt-v1"));
    }

    private const string TestPlanJson = """
        {
          "patient": { "id": "SYN-001", "displayName": "Synthetic" },
          "plan": {
            "id": "Plan",
            "courseId": "C1",
            "prescription": {
              "totalDoseGy": 50,
              "fractionCount": 25,
              "targetStructureId": "PTV",
              "requestedEnergy": "6X",
              "requestedTechniqueId": "VMAT"
            },
            "structures": [ { "id": "PTV", "name": "PTV", "type": "Target", "volumeCc": 100 } ],
            "dose": {
              "id": "Dose",
              "grid": { "spacingXMm": 2, "spacingYMm": 2, "spacingZMm": 2 },
              "calculationModel": "SyntheticAAA",
              "calculationModelVersion": "16.1",
              "statistics": [ { "structureId": "PTV", "metrics": { "MaxDoseGy": 52 } } ]
            },
            "beams": [
              {
                "id": "B1",
                "name": "Beam",
                "modality": "Photon VMAT",
                "energy": "6X",
                "techniqueId": "VMAT",
                "beamModelId": "SYN-AAA-6X",
                "jawTrackingEnabled": true
              }
            ],
            "clinicalGoals": [ { "id": "goal", "structureName": "PTV", "metricKey": "MaxDoseGy", "comparison": "LessThanOrEqual", "threshold": 55, "unit": "Gy" } ]
          }
        }
        """;

    private const string TestEsapiSnapshotJson = """
        {
          "patientId": "SYN-0001",
          "patientDisplayName": "Synthetic Patient",
          "courseId": "C1",
          "planId": "HN-SYN-001",
          "diseaseSite": "Head and Neck",
          "prescription": {
            "totalDoseGy": 70,
            "fractionCount": 35,
            "targetStructureId": "PTV_7000",
            "isSigned": true,
            "requestedEnergy": "6X",
            "requestedTechniqueId": "VMAT"
          },
          "structures": [
            { "id": "BODY", "name": "Body", "type": "External", "volumeCc": 31500, "hasContours": true },
            { "id": "PTV_7000", "name": "PTV_7000", "type": "Target", "volumeCc": 164.2, "hasContours": true },
            { "id": "CORD", "name": "SpinalCord", "type": "OrganAtRisk", "volumeCc": 42.1, "hasContours": true },
            { "id": "HEART", "name": "Heart", "type": "OrganAtRisk", "volumeCc": 611.4, "hasContours": true },
            { "id": "LUNG_R", "name": "Lung_R", "type": "OrganAtRisk", "volumeCc": 1820.5, "hasContours": true }
          ],
          "doseGrid": {
            "spacingXMm": 2.5,
            "spacingYMm": 2.5,
            "spacingZMm": 2.5,
            "calculationModel": "AAA",
            "calculationModelVersion": "16.1"
          },
          "doseStatistics": [
            { "structureId": "PTV_7000", "metrics": { "D95PercentDoseGy": 67.2, "MaxDoseGy": 74.1, "MeanDoseGy": 70.8, "V95GyPercent": 99 } },
            { "structureId": "CORD", "metrics": { "MaxDoseGy": 42.5 } },
            { "structureId": "HEART", "metrics": { "MeanDoseGy": 8.1 } },
            { "structureId": "LUNG_R", "metrics": { "V20GyPercent": 18.2 } }
          ],
          "beams": [
            {
              "id": "B1",
              "name": "Arc 1",
              "modality": "Photon",
              "energy": "6X",
              "monitorUnits": 410,
              "treatmentUnitId": "TB1",
              "techniqueId": "VMAT",
              "isSetupField": false,
              "beamModelId": "SYN-AAA-6X",
              "jawTrackingEnabled": true,
              "controlPoints": [
                { "index": 0, "gantryAngleDegrees": 179, "cumulativeMetersetWeight": 0, "jawPositions": { "x1Cm": -5, "x2Cm": 5, "y1Cm": -6, "y2Cm": 6 } },
                { "index": 1, "gantryAngleDegrees": 181, "cumulativeMetersetWeight": 1, "jawPositions": { "x1Cm": -5, "x2Cm": 5, "y1Cm": -6, "y2Cm": 6 } }
              ]
            }
          ]
        }
        """;
}
