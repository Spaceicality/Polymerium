﻿namespace Trident.Abstractions.Resources
{

    // 用于表示市场中的一个项目，仅用于展示
    // 从 IRepository.Query(string projectId) 获得

    public record Project(string Id, string Name, Uri? Thumbnail, string Author, string Summary, Uri Reference, ResourceKind Kind, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, uint DownloadCount, string DescriptionHtml, IEnumerable<Project.Screenshot> Gallery,
        IEnumerable<Project.Version> Versions)
    {
        public record Screenshot(string Title, Uri Url);

        public record Version(string Name, string ChangelogHtml, ReleaseType ReleaseType, DateTimeOffset PublishedAt, string FileName, string? Hash, Uri Download, Requirement Requirements,IEnumerable<Dependency> Dependencies);
    }
}
