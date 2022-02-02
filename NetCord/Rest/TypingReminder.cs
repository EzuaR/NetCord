﻿namespace NetCord;

internal class TypingReminder : IDisposable
{
    private readonly RestClient _client;

    public DiscordId ChannelId { get; }

    private readonly CancellationTokenSource _tokenSource;
    private readonly RequestOptions? _options;

    public TypingReminder(DiscordId channelId, RestClient client, RequestOptions? options)
    {
        ChannelId = channelId;
        _client = client;
        _options = options;
        _tokenSource = new();
    }

    public async Task StartAsync()
    {
        await _client.Channel.TriggerTypingStateAsync(ChannelId).ConfigureAwait(false);
        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        var token = _tokenSource.Token;
        PeriodicTimer timer = new(TimeSpan.FromSeconds(9.5));
        while (true)
        {
            await timer.WaitForNextTickAsync(token).ConfigureAwait(false);
            await _client.Channel.TriggerTypingStateAsync(ChannelId, _options).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _tokenSource.Cancel();
    }
}