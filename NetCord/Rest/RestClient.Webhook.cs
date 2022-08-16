﻿namespace NetCord.Rest;

public partial class RestClient
{
    public async Task<Webhook> CreateWebhookAsync(Snowflake channelId, WebhookProperties webhookProperties, RequestProperties? properties = null)
        => Webhook.CreateFromJson((await SendRequestAsync(HttpMethod.Post, $"/channels/{channelId}/webhooks", new JsonContent(webhookProperties), properties).ConfigureAwait(false)).ToObject<JsonModels.JsonWebhook>(), this);

    public async Task<IReadOnlyDictionary<Snowflake, Webhook>> GetChannelWebhooksAsync(Snowflake channelId, RequestProperties? properties = null)
        => (await SendRequestAsync(HttpMethod.Get, $"/channels/{channelId}/webhooks", properties).ConfigureAwait(false)).ToObject<JsonModels.JsonWebhook[]>().ToDictionary(w => w.Id, w => Webhook.CreateFromJson(w, this));

    public async Task<IReadOnlyDictionary<Snowflake, Webhook>> GetGuildWebhooksAsync(Snowflake guildId, RequestProperties? properties = null)
        => (await SendRequestAsync(HttpMethod.Get, $"/guilds/{guildId}/webhooks", properties).ConfigureAwait(false)).ToObject<JsonModels.JsonWebhook[]>().ToDictionary(w => w.Id, w => Webhook.CreateFromJson(w, this));

    public async Task<Webhook> GetWebhookAsync(Snowflake webhookId, RequestProperties? properties = null)
        => Webhook.CreateFromJson((await SendRequestAsync(HttpMethod.Get, $"/webhooks/{webhookId}", properties).ConfigureAwait(false)).ToObject<JsonModels.JsonWebhook>(), this);

    public async Task<Webhook> GetWebhookWithTokenAsync(Snowflake webhookId, string webhookToken, RequestProperties? properties = null)
        => Webhook.CreateFromJson((await SendRequestAsync(HttpMethod.Get, $"/webhooks/{webhookId}/{webhookToken}", properties).ConfigureAwait(false)).ToObject<JsonModels.JsonWebhook>(), this);

    public async Task<Webhook> ModifyWebhookAsync(Snowflake webhookId, Action<WebhookOptions> action, RequestProperties? properties = null)
    {
        WebhookOptions webhookOptions = new();
        action(webhookOptions);
        return Webhook.CreateFromJson((await SendRequestAsync(HttpMethod.Patch, $"/webhooks/{webhookId}", new JsonContent(webhookOptions), properties).ConfigureAwait(false)).ToObject<JsonModels.JsonWebhook>(), this);
    }

    public async Task<Webhook> ModifyWebhookWithTokenAsync(Snowflake webhookId, string webhookToken, Action<WebhookOptions> action, RequestProperties? properties = null)
    {
        WebhookOptions webhookOptions = new();
        action(webhookOptions);
        return Webhook.CreateFromJson((await SendRequestAsync(HttpMethod.Patch, $"/webhooks/{webhookId}/{webhookToken}", new JsonContent(webhookOptions), properties).ConfigureAwait(false)).ToObject<JsonModels.JsonWebhook>(), this);
    }

    public Task DeleteWebhookAsync(Snowflake webhookId, RequestProperties? properties = null)
        => SendRequestAsync(HttpMethod.Delete, $"/webhooks/{webhookId}", properties);

    public Task DeleteWebhookWithTokenAsync(Snowflake webhookId, string webhookToken, RequestProperties? properties = null)
        => SendRequestAsync(HttpMethod.Delete, $"/webhooks/{webhookId}/{webhookToken}", properties);

    public async Task<RestMessage?> ExecuteWebhookAsync(Snowflake webhookId, string webhookToken, WebhookMessageProperties messageProperties, bool wait = false, Snowflake? threadId = null, RequestProperties? properties = null)
    {
        if (wait)
            return new((await SendRequestAsync(HttpMethod.Post, threadId.HasValue ? $"/webhooks/{webhookId}/{webhookToken}?wait=True&thread_id={threadId.GetValueOrDefault()}" : $"/webhooks/{webhookId}/{webhookToken}?wait=True", new(RateLimits.RouteParameter.ExecuteWebhookModifyDeleteWebhookMessage), messageProperties.Build(), properties).ConfigureAwait(false)).ToObject<JsonModels.JsonMessage>(), this);
        else
        {
            await SendRequestAsync(HttpMethod.Post, threadId.HasValue ? $"/webhooks/{webhookId}/{webhookToken}?wait=False&thread_id={threadId.GetValueOrDefault()}" : $"/webhooks/{webhookId}/{webhookToken}?wait=False", new(RateLimits.RouteParameter.ExecuteWebhookModifyDeleteWebhookMessage), messageProperties.Build(), properties).ConfigureAwait(false);
            return null;
        }
    }

    public async Task<WebhookMessage> GetWebhookMessageAsync(Snowflake webhookId, string webhookToken, Snowflake messageId, RequestProperties? properties = null)
        => new((await SendRequestAsync(HttpMethod.Get, $"/webhooks/{webhookId}/{webhookToken}/messages/{messageId}", new RateLimits.Route(RateLimits.RouteParameter.ExecuteWebhookModifyDeleteWebhookMessage), properties).ConfigureAwait(false)).ToObject<JsonModels.JsonMessage>(), this);

    public async Task<WebhookMessage> ModifyWebhookMessageAsync(Snowflake webhookId, string webhookToken, Snowflake messageId, Action<MessageOptions> action, Snowflake? threadId = null, RequestProperties? properties = null)
    {
        MessageOptions messageOptions = new();
        action(messageOptions);
        return new((await SendRequestAsync(HttpMethod.Patch, threadId.HasValue ? $"/webhooks/{webhookId}/{webhookToken}/messages/{messageId}?thread_id={threadId.GetValueOrDefault()}" : $"/webhooks/{webhookId}/{webhookToken}/messages/{messageId}", new(RateLimits.RouteParameter.ExecuteWebhookModifyDeleteWebhookMessage), messageOptions.Build(), properties).ConfigureAwait(false)).ToObject<JsonModels.JsonMessage>(), this);
    }

    public Task DeleteWebhookMessageAsync(Snowflake webhookId, string webhookToken, Snowflake messageId, Snowflake? threadId = null, RequestProperties? properties = null)
        => SendRequestAsync(HttpMethod.Delete, threadId.HasValue ? $"/webhooks/{webhookId}/{webhookToken}/messages/{messageId}?thread_id={threadId.GetValueOrDefault()}" : $"/webhooks/{webhookId}/{webhookToken}/messages/{messageId}", new RateLimits.Route(RateLimits.RouteParameter.ExecuteWebhookModifyDeleteWebhookMessage), properties);
}