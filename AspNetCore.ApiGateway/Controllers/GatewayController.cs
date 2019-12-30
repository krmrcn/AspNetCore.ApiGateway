﻿using AspNetCore.ApiGateway.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AspNetCore.ApiGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(GatewayAuthorizeAttribute))]
    public class GatewayController : ControllerBase
    {
        readonly IApiOrchestrator _apiOrchestrator;
        readonly ILogger<GatewayController> _logger;
            
        public GatewayController(IApiOrchestrator apiOrchestrator, ILogger<GatewayController> logger)
        {
            _apiOrchestrator = apiOrchestrator;
            _logger = logger;
        }

        [HttpGet]
        [Route("{api}/{key}")]
        [ServiceFilter(typeof(GatewayGetAuthorizeAttribute))]
        public async Task<IActionResult> Get(string api, string key, string parameters = null)
        {
            if (parameters != null)
                parameters = HttpUtility.UrlDecode(parameters);
            else
                parameters = string.Empty;

            _logger.LogInformation($"ApiGateway: Incoming GET request. api: {api}, key: {key}, parameters: {parameters}");

            var apiInfo = _apiOrchestrator.GetApi(api);

            var gwRouteInfo = apiInfo.Mediator.GetRoute(key);

            var routeInfo = gwRouteInfo.Route;

            if (routeInfo.Exec != null)
            {
                return Ok(await routeInfo.Exec(apiInfo, this.Request));
            }
            else
            {
                using (var client = routeInfo.HttpClientConfig?.HttpClient() ?? new HttpClient())
                {
                    this.Request.Headers?.AddRequestHeaders(client.DefaultRequestHeaders);

                    var response = await client.GetAsync($"{apiInfo.BaseUrl}{routeInfo.Path}{parameters}");

                    response.EnsureSuccessStatusCode();

                    return Ok(JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), routeInfo.ResponseType));
                }
            }
        }

        [HttpPost]
        [Route("{api}/{key}")]
        [ServiceFilter(typeof(GatewayPostAuthorizeAttribute))]
        public async Task<IActionResult> Post(string api, string key, object request, string parameters = null)
        {            
            if (parameters != null)
                parameters = HttpUtility.UrlDecode(parameters);
            else
                parameters = string.Empty;

            _logger.LogInformation($"ApiGateway: Incoming POST request. api: {api}, key: {key}, object: {request.ToString()}, parameters: {parameters}");

            var apiInfo = _apiOrchestrator.GetApi(api);

            var gwRouteInfo = apiInfo.Mediator.GetRoute(key);

            var routeInfo = gwRouteInfo.Route;

            if (routeInfo.Exec != null)
            {
                return Ok(await routeInfo.Exec(apiInfo, this.Request));
            }
            else
            {
                using (var client = routeInfo.HttpClientConfig?.HttpClient() ?? new HttpClient())
                {
                    HttpContent content = null;

                    if (routeInfo.HttpClientConfig?.HttpContent != null)
                    {
                        content = routeInfo.HttpClientConfig.HttpContent();
                    }
                    else
                    {
                        content = new StringContent(request.ToString(), Encoding.UTF8, "application/json");

                        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    }

                    this.Request.Headers?.AddRequestHeaders(client.DefaultRequestHeaders);
                    
                    var response = await client.PostAsync($"{apiInfo.BaseUrl}{routeInfo.Path}{parameters}", content);

                    response.EnsureSuccessStatusCode();

                    return Ok(JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), routeInfo.ResponseType));
                }
            }
        }

        [HttpPut]
        [Route("{api}/{key}")]
        [ServiceFilter(typeof(GatewayPutAuthorizeAttribute))]
        public async Task<IActionResult> Put(string api, string key, object request, string parameters = null)
        {            
            if (parameters != null)
                parameters = HttpUtility.UrlDecode(parameters);
            else
                parameters = string.Empty;

            _logger.LogInformation($"ApiGateway: Incoming PUT request. api: {api}, key: {key}, object: {request.ToString()}, parameters: {parameters}");

            var apiInfo = _apiOrchestrator.GetApi(api);

            var gwRouteInfo = apiInfo.Mediator.GetRoute(key);

            var routeInfo = gwRouteInfo.Route;

            if (routeInfo.Exec != null)
            {
                return Ok(await routeInfo.Exec(apiInfo, this.Request));
            }
            else
            {
                using (var client = routeInfo.HttpClientConfig?.HttpClient() ?? new HttpClient())
                {
                    HttpContent content = null;

                    if (routeInfo.HttpClientConfig?.HttpContent != null)
                    {
                        content = routeInfo.HttpClientConfig.HttpContent();
                    }
                    else
                    {
                        content = new StringContent(request.ToString(), Encoding.UTF8, "application/json");

                        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    }

                    this.Request.Headers?.AddRequestHeaders(client.DefaultRequestHeaders);

                    var response = await client.PutAsync($"{apiInfo.BaseUrl}{routeInfo.Path}{parameters}", content);

                    response.EnsureSuccessStatusCode();

                    return Ok(JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), routeInfo.ResponseType));
                }
            }
        }

        [HttpDelete]
        [Route("{api}/{key}")]
        [ServiceFilter(typeof(GatewayDeleteAuthorizeAttribute))]
        public async Task<IActionResult> Delete(string api, string key, string parameters = null)
        {
            if (parameters != null)
            {
                parameters = HttpUtility.UrlDecode(parameters);
            }
            else
                parameters = string.Empty;

            _logger.LogInformation($"ApiGateway: Incoming DELETE request. api: {api}, key: {key}, parameters: {parameters}");

            var apiInfo = _apiOrchestrator.GetApi(api);

            var gwRouteInfo = apiInfo.Mediator.GetRoute(key);

            var routeInfo = gwRouteInfo.Route;

            if (routeInfo.Exec != null)
            {
                return Ok(await routeInfo.Exec(apiInfo, this.Request));
            }
            else
            {
                using (var client = routeInfo.HttpClientConfig?.HttpClient() ?? new HttpClient())
                {
                    this.Request.Headers?.AddRequestHeaders(client.DefaultRequestHeaders);

                    var response = await client.DeleteAsync($"{apiInfo.BaseUrl}{routeInfo.Path}{parameters}");

                    response.EnsureSuccessStatusCode();

                    return Ok(JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), routeInfo.ResponseType));
                }
            }
        }

        [HttpGet]
        [Route("orchestration")]
        [ServiceFilter(typeof(GatewayGetOrchestrationAuthorizeAttribute))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Orchestration))]
        public async Task<IActionResult> GetOrchestration()
        {
            return Ok(await Task.FromResult(_apiOrchestrator.Orchestration));
        }
    }
}