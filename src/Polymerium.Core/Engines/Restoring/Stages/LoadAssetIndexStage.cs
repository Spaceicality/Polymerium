﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Core.Managers;
using Polymerium.Core.Models.Mojang;
using Polymerium.Core.StageModels;
using Wupoo;

namespace Polymerium.Core.Engines.Restoring.Stages;

public class LoadAssetIndexStage : StageBase
{
    private readonly AssetManager _assetManager;
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;
    private readonly GameInstance _instance;
    private readonly PolylockData _polylock;
    private readonly SHA1 _sha1;

    public LoadAssetIndexStage(
        GameInstance instance,
        SHA1 sha1,
        PolylockData polylock,
        IFileBaseService fileBase,
        DownloadEngine downloader,
        AssetManager assetManager
    )
    {
        _instance = instance;
        _sha1 = sha1;
        _polylock = polylock;
        _fileBase = fileBase;
        _downloader = downloader;
        _assetManager = assetManager;
    }

    public override string StageNameKey => "获取游戏资产资源清单";

    public override async Task<Option<StageBase>> StartAsync()
    {
        if (Token.IsCancellationRequested)
            return Cancel();
        var assetIndexFile = new Uri(
            ConstPath.CACHE_ASSETS_INDEX_FILE.Replace("{0}", _polylock.AssetIndex.Id)
        );
        string? content = null;
        if (
            !await _fileBase.VerifyHashAsync(assetIndexFile, _polylock.AssetIndex.Sha1, _sha1)
            || !_fileBase.TryReadAllText(assetIndexFile, out content)
        )
        {
            if (Token.IsCancellationRequested)
                return Cancel();
            Exception? exception = null;
            await Wapoo
                .Wohoo(_polylock.AssetIndex.Url)
                .WhenException<Exception>(e => exception = e)
                .ForAnyResult(
                    async (_, stream) =>
                    {
                        using var reader = new StreamReader(stream);
                        content = await reader.ReadToEndAsync();
                    }
                )
                .FetchAsync();
            if (content == null)
                return Error(exception?.Message ?? "获取资源索引失败", exception);
            _fileBase.WriteAllText(assetIndexFile, content);
        }

        try
        {
            var assetIndex = JsonConvert.DeserializeObject<AssetsIndex>(content);
            return Next(
                new CompleteAssetsStage(
                    _instance,
                    _polylock,
                    assetIndex,
                    _sha1,
                    _fileBase,
                    _downloader,
                    _assetManager
                )
            );
        }
        catch (Exception e)
        {
            return Error(e.Message, e);
        }
    }
}
