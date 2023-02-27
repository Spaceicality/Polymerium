﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.Abstractions.Resources;

namespace Polymerium.Core.Resources;

public interface IResourceRepository
{
    string Label { get; }
    ResourceType SupportedResources { get; }

    Task<IEnumerable<Modpack>> SearchModpacksAsync(string query, string? version, uint offset = 0, uint limit = 10,
        CancellationToken token = default);
}