using System.Collections.Generic;
using PowerDown.Abstractions;

namespace PowerDown.Abstractions.Interfaces;

public interface IDetectorFactory
{
    List<IDownloadDetector> CreateDetectors(Configuration config, ILogger logger);
}
