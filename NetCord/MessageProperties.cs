﻿using System.Text.Json.Serialization;

namespace NetCord;

public class MessageProperties
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("tts")]
    public bool Tts { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(JsonConverters.MessageAttachmentListConverter))]
    [JsonPropertyName("attachments")]
    public List<AttachmentProperties>? Attachments { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("embeds")]
    public List<EmbedProperties>? Embeds { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("allowed_mentions")]
    public AllowedMentionsProperties? AllowedMentions { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("message_reference")]
    public MessageReferenceProperties? MessageReference { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("components")]
    public List<ComponentProperties>? Components { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("sticker_ids")]
    public List<DiscordId>? StickerIds { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("flags")]
    public MessageFlags? Flags { get; set; }

    internal MultipartFormDataContent Build()
    {
        MultipartFormDataContent content = new();
        content.Add(new JsonContent(this), "payload_json");
        if (Attachments != null)
        {
            var count = Attachments.Count;
            for (var i = 0; i < count; i++)
            {
                AttachmentProperties attachment = Attachments[i];
                content.Add(new StreamContent(attachment.Stream), $"files[{i}]", attachment.FileName);
            }
        }
        return content;
    }

    public static implicit operator MessageProperties(string content) => new()
    {
        Content = content
    };
}