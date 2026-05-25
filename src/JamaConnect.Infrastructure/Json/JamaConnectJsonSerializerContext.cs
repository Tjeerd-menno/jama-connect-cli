using System.Text.Json.Serialization;
using JamaConnect.Infrastructure.Authentication;
using JamaConnect.Infrastructure.JamaConnect.Dto;
using System.Text.Json;

namespace JamaConnect.Infrastructure.Json;

[JsonSerializable(typeof(TokenResponse), TypeInfoPropertyName = "TokenResponse")]
[JsonSerializable(typeof(PagedResponse<ProjectDto>), TypeInfoPropertyName = "PagedProjectResponse")]
[JsonSerializable(typeof(SingleResponse<ProjectDto>), TypeInfoPropertyName = "SingleProjectResponse")]
[JsonSerializable(typeof(PagedResponse<ItemDto>), TypeInfoPropertyName = "PagedItemResponse")]
[JsonSerializable(typeof(SingleResponse<ItemDto>), TypeInfoPropertyName = "SingleItemResponse")]
[JsonSerializable(typeof(JsonPagedResponse), TypeInfoPropertyName = "JsonPagedResponse")]
[JsonSerializable(typeof(JsonSingleResponse), TypeInfoPropertyName = "JsonSingleResponse")]
[JsonSerializable(typeof(JsonErrorResponse), TypeInfoPropertyName = "JsonErrorResponse")]
[JsonSerializable(typeof(RequestItemDto), TypeInfoPropertyName = "RequestItemDto")]
[JsonSerializable(typeof(RequestRelationshipDto), TypeInfoPropertyName = "RequestRelationshipDto")]
[JsonSerializable(typeof(RequestTestCaseDto), TypeInfoPropertyName = "RequestTestCaseDto")]
[JsonSerializable(typeof(PatchDto[]), TypeInfoPropertyName = "PatchDtoArray")]
[JsonSerializable(typeof(Dictionary<string, JsonElement>), TypeInfoPropertyName = "DictionaryStringJsonElement")]
internal sealed partial class JamaConnectJsonSerializerContext : JsonSerializerContext
{
}
