﻿using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nyaalink;
using Nyaalink.Configuration;
using Nyaalink.Endpoints;
using Nyaalink.Services;
using QBittorrent.Client;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddOpenApi();
builder.Services.AddDbContext<DownloadContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("Downloads")));
builder.Services.AddSingleton<Channel<DownloadRecord>>(_ => Channel.CreateUnbounded<DownloadRecord>());
builder.Services.AddSingleton<Channel<RuleCreatedEvent>>(_ => Channel.CreateUnbounded<RuleCreatedEvent>());
builder.Services.AddHttpClient();
builder.Services.AddScoped<FeedConsumer>();
builder.Services.AddHostedService<FeedScheduler>();
builder.Services.Configure<QbitConfiguration>(builder.Configuration.GetSection("Qbit"));
builder.Services.AddScoped<QbitService>();
builder.Services.AddHostedService<DownloadInitiator>();
builder.Services.AddHostedService<RetryInitiator>();
builder.Services.AddHostedService<NewRuleBackfiller>();
builder.Services.AddScoped(static services =>
{
    var options = services.GetRequiredService<IOptions<QbitConfiguration>>();
    return new QBittorrentClient(options.Value.Uri);
});

var app = builder.Build();
app.Services.GetRequiredService<DownloadContext>().Database.EnsureCreated();

app.MapEndpoints();
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.Run();