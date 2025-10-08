using AlgoLab.Core.Abstractions;
using AlgoLab.Core.Models.Enums;

namespace AlgoLab.Core.Extensions;

public static class BrokerApiExtensions
{
    public static bool Supports(this IBrokerApi api, BrokerCapabilities cap) => (api.Capabilities & cap) == cap;
}
