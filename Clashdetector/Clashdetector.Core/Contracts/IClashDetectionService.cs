using Clashdetector.Core.Models;

namespace Clashdetector.Core.Contracts;

public interface IClashDetectionService
{
    DetectionRunSummary Run(ClashDetectionRequest request);
}
