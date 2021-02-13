﻿using AspNetCore.ApiGateway.Application.ExceptionFilters;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace ApiGateway.API.Application.ActionFilters
{
    public class ExceptionFilterService : IGatewayExceptionFilter
    {
        public async Task OnExceptionAsync(ExceptionContext context, string api, string key, string verb)
        {
            //handle exception here

            await Task.CompletedTask;
        }
    }

    public class PostExceptionFilterService : IPostGatewayExceptionFilter
    {
        public async Task OnExceptionAsync(ExceptionContext context, string api, string key)
        {
            //handle exception here

            await Task.CompletedTask;
        }
    }
}
