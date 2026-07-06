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
}
