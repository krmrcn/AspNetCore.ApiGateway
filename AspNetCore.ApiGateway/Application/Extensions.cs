﻿using AspNetCore.ApiGateway.Application;
using AspNetCore.ApiGateway.Application.ActionFilters;
using AspNetCore.ApiGateway.Application.ExceptionFilters;
using AspNetCore.ApiGateway.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http.Headers;

namespace AspNetCore.ApiGateway
{
    public static class Extensions
    {
        static ApiGatewayOptions Options { get; set; }

        public static void AddApiGateway(this IServiceCollection services, Action<ApiGatewayOptions> options = null)
        {
            Options = new ApiGatewayOptions();

            options?.Invoke(Options);

            var apis = new ApiOrchestrator();
            
            services.AddTransient<IApiOrchestrator>(x => apis);

            services.AddAuthorizationFilters()
                    .AddActionFilters()
                    .AddExceptionFilters();

            services.AddHttpClient<IHttpService, HttpService>();

            services.AddApiGatewayResponseCaching();

            services.AddControllers().AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);            
        }

        public static void UseApiGateway(this IApplicationBuilder app, Action<IApiOrchestrator> setApis)
        {
            var serviceProvider = app.ApplicationServices;
            var apiOrchestrator = serviceProvider.GetService<IApiOrchestrator>();
            setApis(apiOrchestrator);
            if (Options != null)
            {
                if (Options.UseResponseCaching)
                {
                    app.UseResponseCaching();
                }
            }
            app.UseMiddleware<GatewayMiddleware>();

            app.UseHubs(apiOrchestrator);            
        }

        internal static IServiceCollection AddAuthorizationFilters(this IServiceCollection services)
        {
            services.AddScoped<GatewayAuthorizeAttribute>();
            services.AddScoped<GatewayGetOrHeadAuthorizeAttribute>();
            services.AddScoped<GatewayGetOrchestrationAuthorizeAttribute>();
            services.AddScoped<GatewayPostAuthorizeAttribute>();
            services.AddScoped<GatewayHubPostAuthorizeAttribute>();
            services.AddScoped<GatewayPutAuthorizeAttribute>();
            services.AddScoped<GatewayPatchAuthorizeAttribute>();
            services.AddScoped<GatewayDeleteAuthorizeAttribute>();

            return services;
        }

        internal static IServiceCollection AddActionFilters(this IServiceCollection services)
        {
            services.AddScoped<GatewayAsyncActionFilterAttribute>();
            services.AddScoped<GatewayGetOrHeadAsyncActionFilterAttribute>();
            services.AddScoped<GatewayGetOrchestrationAsyncActionFilterAttribute>();
            services.AddScoped<GatewayPostAsyncActionFilterAttribute>();
            services.AddScoped<GatewayHubPostAsyncActionFilterAttribute>();
            services.AddScoped<GatewayPutAsyncActionFilterAttribute>();
            services.AddScoped<GatewayPatchAsyncActionFilterAttribute>();
            services.AddScoped<GatewayDeleteAsyncActionFilterAttribute>();

            return services;
        }

        internal static IServiceCollection AddExceptionFilters(this IServiceCollection services)
        {
            services.AddScoped<GatewayAsyncExceptionFilterAttribute>();
            services.AddScoped<GatewayGetOrHeadAsyncExceptionFilterAttribute>();
            services.AddScoped<GatewayGetOrchestrationAsyncExceptionFilterAttribute>();
            services.AddScoped<GatewayPostAsyncExceptionFilterAttribute>();
            services.AddScoped<GatewayHubPostAsyncExceptionFilterAttribute>();
            services.AddScoped<GatewayPutAsyncExceptionFilterAttribute>();
            services.AddScoped<GatewayPatchAsyncExceptionFilterAttribute>();
            services.AddScoped<GatewayDeleteAsyncExceptionFilterAttribute>();

            return services;
        }

        internal static void AddApiGatewayResponseCaching(this IServiceCollection services)
        {
            if (Options != null)
            {
                if (Options.UseResponseCaching && Options.ResponseCacheSettings != null)
                {
                    services.AddResponseCaching();

                    services.AddMvc(o => o.Filters.Add(new ResponseCacheAttribute
                    {
                        NoStore = Options.ResponseCacheSettings.NoStore,
                        Location = Options.ResponseCacheSettings.Location,
                        Duration = Options.ResponseCacheSettings.Duration,
                        VaryByHeader = Options.ResponseCacheSettings.VaryByHeader,
                        VaryByQueryKeys = Options.ResponseCacheSettings.VaryByQueryKeys,
                        CacheProfileName = Options.ResponseCacheSettings.CacheProfileName
                    }));
                }
            }
        }

        internal static void UseHubs(this IApplicationBuilder app, IApiOrchestrator apiOrchestrator)
        {
            if (apiOrchestrator.StartGatewayHub)
            {
                var gatewayConn = new HubConnectionBuilder()
                    .WithUrl(apiOrchestrator.GatewayHubUrl)
                    .WithAutomaticReconnect()
                    .AddNewtonsoftJsonProtocol()
                    .Build();

                gatewayConn.StartAsync().ConfigureAwait(false);

                apiOrchestrator.Hubs.ToList().ForEach(async hub =>
                {
                    var connection = hub.Value.Connection;

                    await connection.StartAsync();

                    hub.Value.Mediator.Paths.ToList().ForEach(path =>
                    {
                        var route = hub.Value.Mediator.GetRoute(path.Key).HubRoute;

                        connection.On(route.ReceiveMethod, route.ReceiveParameterTypes, async (arg1, arg2) =>
                        {
                            await gatewayConn.InvokeAsync("Receive", new HubReceiveAuth { Hub = hub.Key, ReceiveKey = hub.Value.ReceiveKey }, route, arg1, arg2);
                        }, new object());
                    });
                });
            }
        }

        internal static void AddRequestHeaders (this IHeaderDictionary requestHeaders, HttpRequestHeaders headers)
        { 
            foreach (var item in requestHeaders)
            {
                try
                {
                    if (!headers.Contains(item.Key))
                        headers.Add(item.Key, item.Value.ToString());
                }
                catch(Exception)
                { }
            }
        }

        internal static Orchestration FilterRoutes(this Orchestration orchestration, string key)
        {
            orchestration.Routes = orchestration.Routes.Where(y => y.Key.Contains(key.Trim()));            
            return orchestration;
        }

        internal static string ToUtcLongDateTime(this DateTime dateTime)
        {
            return dateTime.ToString("MM/dd/yyyy hh:mm:ss.fff tt");
        }

        internal static void LogApiInfo(this ILogger<ApiGatewayLog> logger, string api, string key, string parameters, object request = null)
        {
            if (request != null)
                logger.LogInformation($"ApiGateway: Incoming POST request. api: {api}, key: {key}, object: {JsonConvert.SerializeObject(request)}, parameters: {parameters}, UtcTime: { DateTime.UtcNow.ToUtcLongDateTime() }");
            else
                logger.LogInformation($"ApiGateway: Incoming POST request. api: {api}, key: {key}, UtcTime: { DateTime.UtcNow.ToUtcLongDateTime() }");
        }

        internal static void LogApiInfo(this ILogger<ApiGatewayLog> logger, string url, bool beforeBackendCall = true)
        {
            if (beforeBackendCall)
                logger.LogInformation($"ApiGateway: Calling back end. Url: {url}, UtcTime: { DateTime.UtcNow.ToUtcLongDateTime() }");
            else
                logger.LogInformation($"ApiGateway: Finished calling back end. Url: {url}, UtcTime: { DateTime.UtcNow.ToUtcLongDateTime() }");
        }

    }
}
