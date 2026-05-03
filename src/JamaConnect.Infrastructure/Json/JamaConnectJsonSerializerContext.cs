using System.Text.Json.Serialization;
using JamaConnect.Infrastructure.Authentication;
using JamaConnect.Infrastructure.JamaConnect.Dto;

namespace JamaConnect.Infrastructure.Json;

[JsonSerializable(typeof(TokenResponse), TypeInfoPropertyName = "TokenResponse")]
[JsonSerializable(typeof(PagedResponse<ProjectDto>), TypeInfoPropertyName = "PagedProjectResponse")]
[JsonSerializable(typeof(SingleResponse<ProjectDto>), TypeInfoPropertyName = "SingleProjectResponse")]
[JsonSerializable(typeof(PagedResponse<ItemDto>), TypeInfoPropertyName = "PagedItemResponse")]
[JsonSerializable(typeof(SingleResponse<ItemDto>), TypeInfoPropertyName = "SingleItemResponse")]
internal sealed partial class JamaConnectJsonSerializerContext : JsonSerializerContext;
