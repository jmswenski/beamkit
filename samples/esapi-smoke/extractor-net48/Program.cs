using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using Application = VMS.TPS.Common.Model.API.Application;

namespace BeamKit.EsapiSmoke.Extractor
{
    internal static class Program
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                var options = SmokeOptions.Parse(args);
                using (var app = Application.CreateApplication())
                {
                    var patient = app.OpenPatientById(options.PatientId)
                        ?? throw new InvalidOperationException("Patient was not found.");
                    try
                    {
                        var course = patient.Courses.FirstOrDefault(item => string.Equals(item.Id, options.CourseId, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException("Course was not found.");
                        var plan = course.PlanSetups.FirstOrDefault(item => string.Equals(item.Id, options.PlanId, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException("Plan was not found.");
                        var snapshot = ExtractSnapshot(patient, course, plan, options.DiseaseSite);
                        var directory = Path.GetDirectoryName(options.OutputPath);
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        File.WriteAllText(options.OutputPath, JsonConvert.SerializeObject(snapshot, JsonSettings));
                        Console.WriteLine("Wrote ESAPI snapshot: " + options.OutputPath);
                        return 0;
                    }
                    finally
                    {
                        app.ClosePatient();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("esapi-smoke: " + ex.Message);
                Console.Error.WriteLine();
                SmokeOptions.WriteUsage();
                return 1;
            }
        }

        private static object ExtractSnapshot(Patient patient, Course course, PlanSetup plan, string diseaseSite)
        {
            return new
            {
                patientId = patient.Id,
                patientDisplayName = PatientDisplayName(patient),
                courseId = course.Id,
                planId = plan.Id,
                diseaseSite,
                prescription = ExtractPrescription(plan),
                structures = ExtractStructures(plan.StructureSet).ToArray(),
                doseGrid = ExtractDoseGrid(plan),
                doseStatistics = ExtractDoseStatistics(plan).ToArray(),
                beams = plan.Beams.Select(ExtractBeam).ToArray()
            };
        }

        private static object ExtractPrescription(PlanSetup plan)
        {
            var fractionation = plan.UniqueFractionation
                ?? throw new InvalidOperationException("Plan has no unique fractionation.");
            var fractionCount = Convert.ToInt32(fractionation.NumberOfFractions, CultureInfo.InvariantCulture);
            var totalDoseGy = DoseToGy(plan.TotalDose);

            return new
            {
                totalDoseGy,
                fractionCount,
                targetStructureId = TargetStructureId(plan),
                isSigned = IsApproved(plan),
                intent = TryReadString(plan, "TreatmentOrientation"),
                requestedEnergy = CommonValue(plan.Beams.Where(beam => !beam.IsSetupField).Select(beam => beam.EnergyModeDisplayName)),
                requestedTechniqueId = CommonValue(plan.Beams.Where(beam => !beam.IsSetupField).Select(beam => beam.Technique == null ? null : beam.Technique.Id))
            };
        }

        private static IEnumerable<object> ExtractStructures(StructureSet structureSet)
        {
            foreach (var structure in structureSet.Structures.OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase))
            {
                yield return new
                {
                    id = structure.Id,
                    name = structure.Id,
                    type = MapStructureType(structure.DicomType),
                    volumeCc = Decimal(structure.Volume),
                    hasContours = !structure.IsEmpty && structure.Volume > 0
                };
            }
        }

        private static object ExtractDoseGrid(PlanSetup plan)
        {
            if (plan.Dose == null)
            {
                return null;
            }

            return new
            {
                spacingXMm = Decimal(plan.Dose.XRes),
                spacingYMm = Decimal(plan.Dose.YRes),
                spacingZMm = Decimal(plan.Dose.ZRes),
                calculationModel = TryGetCalculationModel(plan),
                calculationModelVersion = TryGetCalculationOption(plan, "CalculationVersion")
            };
        }

        private static IEnumerable<object> ExtractDoseStatistics(PlanSetup plan)
        {
            if (plan.Dose == null || plan.StructureSet == null)
            {
                yield break;
            }

            foreach (var structure in plan.StructureSet.Structures.Where(item => !item.IsEmpty && item.Volume > 0).OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase))
            {
                Dictionary<string, decimal> metrics;
                try
                {
                    metrics = ExtractStructureDoseMetrics(plan, structure);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Skipping dose statistics for " + structure.Id + ": " + ex.Message);
                    continue;
                }

                if (metrics.Count > 0)
                {
                    yield return new
                    {
                        structureId = structure.Id,
                        metrics
                    };
                }
            }
        }

        private static Dictionary<string, decimal> ExtractStructureDoseMetrics(PlanSetup plan, Structure structure)
        {
            var metrics = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var dvh = plan.GetDVHCumulativeData(
                structure,
                DoseValuePresentation.Absolute,
                VolumePresentation.Relative,
                0.1);

            if (dvh != null)
            {
                metrics["MaxDoseGy"] = DoseToGy(dvh.MaxDose);
                metrics["MeanDoseGy"] = DoseToGy(dvh.MeanDose);
                metrics["MinDoseGy"] = DoseToGy(dvh.MinDose);
            }

            metrics["D95PercentDoseGy"] = DoseToGy(plan.GetDoseAtVolume(
                structure,
                95,
                VolumePresentation.Relative,
                DoseValuePresentation.Absolute));
            metrics["D98PercentDoseGy"] = DoseToGy(plan.GetDoseAtVolume(
                structure,
                98,
                VolumePresentation.Relative,
                DoseValuePresentation.Absolute));
            metrics["D2PercentDoseGy"] = DoseToGy(plan.GetDoseAtVolume(
                structure,
                2,
                VolumePresentation.Relative,
                DoseValuePresentation.Absolute));
            metrics["V20GyPercent"] = Decimal(plan.GetVolumeAtDose(
                structure,
                new DoseValue(20, DoseValue.DoseUnit.Gy),
                VolumePresentation.Relative));
            metrics["V95GyPercent"] = Decimal(plan.GetVolumeAtDose(
                structure,
                new DoseValue(95, DoseValue.DoseUnit.Percent),
                VolumePresentation.Relative));

            return metrics;
        }

        private static object ExtractBeam(Beam beam)
        {
            return new
            {
                id = beam.Id,
                name = beam.Name,
                modality = "Photon",
                energy = beam.EnergyModeDisplayName,
                gantryAngleDegrees = Decimal(beam.GantryAngle),
                monitorUnits = beam.Meterset == null ? (decimal?)null : Decimal(beam.Meterset.Value),
                treatmentUnitId = beam.TreatmentUnit == null ? null : beam.TreatmentUnit.Id,
                techniqueId = beam.Technique == null ? null : beam.Technique.Id,
                isSetupField = beam.IsSetupField,
                controlPoints = beam.ControlPoints.Select((controlPoint, index) => ExtractControlPoint(controlPoint, index)).ToArray(),
                beamModelId = BeamModelId(beam),
                jawTrackingEnabled = TryReadBool(beam, "JawTracking")
            };
        }

        private static object ExtractControlPoint(ControlPoint controlPoint, int index)
        {
            var jawPositions = TryReadProperty(controlPoint, "JawPositions");
            return new
            {
                index,
                gantryAngleDegrees = Decimal(controlPoint.GantryAngle),
                cumulativeMetersetWeight = Decimal(controlPoint.MetersetWeight),
                jawPositions = ExtractJawPositions(jawPositions)
            };
        }

        private static object ExtractJawPositions(object jawPositions)
        {
            if (jawPositions == null)
            {
                return null;
            }

            return new
            {
                x1Cm = MmToCm(TryReadDecimal(jawPositions, "X1")),
                x2Cm = MmToCm(TryReadDecimal(jawPositions, "X2")),
                y1Cm = MmToCm(TryReadDecimal(jawPositions, "Y1")),
                y2Cm = MmToCm(TryReadDecimal(jawPositions, "Y2"))
            };
        }

        private static string TargetStructureId(PlanSetup plan)
        {
            var targetVolumeId = TryReadString(plan, "TargetVolumeID");
            if (!string.IsNullOrWhiteSpace(targetVolumeId))
            {
                return targetVolumeId;
            }

            var target = plan.StructureSet.Structures.FirstOrDefault(structure =>
                string.Equals(structure.DicomType, "PTV", StringComparison.OrdinalIgnoreCase)
                || string.Equals(structure.DicomType, "CTV", StringComparison.OrdinalIgnoreCase)
                || string.Equals(structure.DicomType, "GTV", StringComparison.OrdinalIgnoreCase));

            return target == null
                ? throw new InvalidOperationException("Could not determine target structure id.")
                : target.Id;
        }

        private static string MapStructureType(string dicomType)
        {
            if (string.Equals(dicomType, "EXTERNAL", StringComparison.OrdinalIgnoreCase))
            {
                return "External";
            }

            if (string.Equals(dicomType, "PTV", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dicomType, "CTV", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dicomType, "GTV", StringComparison.OrdinalIgnoreCase))
            {
                return "Target";
            }

            if (string.Equals(dicomType, "ORGAN", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dicomType, "AVOIDANCE", StringComparison.OrdinalIgnoreCase))
            {
                return "OrganAtRisk";
            }

            if (string.Equals(dicomType, "SUPPORT", StringComparison.OrdinalIgnoreCase))
            {
                return "Support";
            }

            return "Unknown";
        }

        private static bool IsApproved(PlanSetup plan)
        {
            var approvalStatus = TryReadString(plan, "ApprovalStatus");
            return approvalStatus != null
                && approvalStatus.IndexOf("Approved", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BeamModelId(Beam beam)
        {
            return FirstNonEmpty(
                TryReadString(beam, "BeamModelId"),
                TryReadString(beam, "ExternalBeamMachineParameters"),
                beam.EnergyModeDisplayName);
        }

        private static string TryGetCalculationModel(PlanSetup plan)
        {
            try
            {
                return plan.GetCalculationModel(CalculationType.PhotonVolumeDose);
            }
            catch
            {
                return null;
            }
        }

        private static string TryGetCalculationOption(PlanSetup plan, string option)
        {
            try
            {
                return plan.GetCalculationOption(CalculationType.PhotonVolumeDose, option);
            }
            catch
            {
                return null;
            }
        }

        private static string PatientDisplayName(Patient patient)
        {
            return FirstNonEmpty(
                string.Join(", ", new[] { TryReadString(patient, "LastName"), TryReadString(patient, "FirstName") }.Where(value => !string.IsNullOrWhiteSpace(value))),
                TryReadString(patient, "Name"),
                patient.Id);
        }

        private static string CommonValue(IEnumerable<string> values)
        {
            var distinct = values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return distinct.Length == 1 ? distinct[0] : null;
        }

        private static decimal DoseToGy(DoseValue value)
        {
            if (double.IsNaN(value.Dose))
            {
                return 0m;
            }

            switch (value.Unit)
            {
                case DoseValue.DoseUnit.Gy:
                    return Decimal(value.Dose);
                case DoseValue.DoseUnit.cGy:
                    return Decimal(value.Dose) / 100m;
                case DoseValue.DoseUnit.Percent:
                    return Decimal(value.Dose);
                default:
                    throw new InvalidOperationException("Unsupported dose unit: " + value.Unit);
            }
        }

        private static decimal MmToCm(decimal value)
        {
            return value / 10m;
        }

        private static decimal Decimal(double value)
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        private static decimal TryReadDecimal(object source, string propertyName)
        {
            var value = TryReadProperty(source, propertyName);
            return value == null ? 0m : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        private static bool? TryReadBool(object source, string propertyName)
        {
            var value = TryReadProperty(source, propertyName);
            return value == null ? (bool?)null : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        private static string TryReadString(object source, string propertyName)
        {
            return TryReadProperty(source, propertyName)?.ToString();
        }

        private static object TryReadProperty(object source, string propertyName)
        {
            if (source == null)
            {
                return null;
            }

            var property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            return property == null ? null : property.GetValue(source, null);
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }
    }

    internal sealed class SmokeOptions
    {
        public string PatientId { get; private set; }

        public string CourseId { get; private set; }

        public string PlanId { get; private set; }

        public string OutputPath { get; private set; }

        public string DiseaseSite { get; private set; }

        public static SmokeOptions Parse(string[] args)
        {
            var options = new SmokeOptions();
            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                switch (arg)
                {
                    case "--patient":
                        options.PatientId = ReadRequired(args, ++index, arg);
                        break;
                    case "--course":
                        options.CourseId = ReadRequired(args, ++index, arg);
                        break;
                    case "--plan":
                        options.PlanId = ReadRequired(args, ++index, arg);
                        break;
                    case "--out":
                    case "--output":
                        options.OutputPath = ReadRequired(args, ++index, arg);
                        break;
                    case "--disease-site":
                        options.DiseaseSite = ReadRequired(args, ++index, arg);
                        break;
                    default:
                        throw new ArgumentException("Unknown option '" + arg + "'.");
                }
            }

            if (string.IsNullOrWhiteSpace(options.PatientId)
                || string.IsNullOrWhiteSpace(options.CourseId)
                || string.IsNullOrWhiteSpace(options.PlanId)
                || string.IsNullOrWhiteSpace(options.OutputPath))
            {
                throw new ArgumentException("Options --patient, --course, --plan, and --out are required.");
            }

            return options;
        }

        public static void WriteUsage()
        {
            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  BeamKit.EsapiSmoke.Extractor.exe --patient id --course id --plan id --out path [--disease-site name]");
        }

        private static string ReadRequired(string[] args, int index, string optionName)
        {
            if (index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Option '" + optionName + "' requires a value.");
            }

            return args[index];
        }
    }
}
