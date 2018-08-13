using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace chen2584.CustomAttributes
{
    public class OutputCacheAttribute : ActionFilterAttribute
    {
        private static MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        private static JsonSerializerSettings jsonSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        public int Duration { get; set; } = 30;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string cacheKey = $"{context.RouteData.Values["controller"].ToString()}-{context.RouteData.Values["action"].ToString()}";
            if (cache.TryGetValue(cacheKey, out OutputCacheModel outputCache))
            {
                context.Result = new ContentResult
                {
                    ContentType = outputCache.ContentType,
                    StatusCode = outputCache.StatusCode,
                    Content = outputCache.Content
                };
            }
        }

        public override async void OnResultExecuted(ResultExecutedContext context)
        {
            string cacheKey = $"{context.RouteData.Values["controller"].ToString()}-{context.RouteData.Values["action"].ToString()}";
            if (cache.Get(cacheKey) == null)
            {
                OutputCacheModel outputCache = null;
                if (context.Result is ObjectResult objectResult)
                {
                    // Check if objectResult is JSON
                    string value = context.HttpContext.Response.ContentType.StartsWith("application/json") 
                        ? JsonConvert.SerializeObject(objectResult.Value, jsonSettings) : objectResult.Value.ToString();
                    outputCache = new OutputCacheModel()
                    { 
                        Content = value,
                        ContentType = context.HttpContext.Response.ContentType,
                        StatusCode = context.HttpContext.Response.StatusCode 
                    };
                }
                else if(context.Result is JsonResult jsonResult)
                {
                    outputCache = new OutputCacheModel()
                    { 
                        Content = JsonConvert.SerializeObject(jsonResult.Value, jsonSettings),
                        ContentType = context.HttpContext.Response.ContentType,
                        StatusCode = context.HttpContext.Response.StatusCode 
                    };
                }
                else if (context.Result is ViewResult viewResult)
                {
                    outputCache = await GenerateViewAsString(context, viewResult);
                }

                if(outputCache != null)
                {
                    cache.Set(cacheKey, outputCache, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(Duration)));
                }
            }
        }

        private async Task<OutputCacheModel> GenerateViewAsString(FilterContext context, ViewResult viewResult)
        {
            string viewName = viewResult.ViewName != null ? viewResult.ViewName : context.RouteData.Values["action"].ToString();
            IViewEngine viewEngine = context.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
            ViewEngineResult viewEngineResult = viewEngine.FindView(context, viewName, true);
            using (var writer = new StringWriter())
            {
                if (viewEngineResult.Success)
                {
                    ViewContext viewContext = new ViewContext(context, viewEngineResult.View, viewResult.ViewData, viewResult.TempData, writer, new HtmlHelperOptions());
                    await viewEngineResult.View.RenderAsync(viewContext);

                    string content = writer.GetStringBuilder().ToString();
                    var outputCache = new OutputCacheModel()
                    {
                        Content = content,
                        ContentType = context.HttpContext.Response.ContentType,
                        StatusCode = context.HttpContext.Response.StatusCode
                    };
                    return outputCache;
                }
                else
                {
                    throw new FileNotFoundException($"A view with the name {viewName} could not be found");
                }
            }
        }
    }
}