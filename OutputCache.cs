using System;
using System.IO;
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
                if (context.Result is ObjectResult objectResult)
                {
                    string value = context.HttpContext.Response.ContentType.StartsWith("application/json") ? JsonConvert.SerializeObject(objectResult.Value, jsonSettings) : objectResult.Value.ToString();
                    var outputCache = new OutputCacheModel() { Content = value, ContentType = context.HttpContext.Response.ContentType, StatusCode = context.HttpContext.Response.StatusCode };
                    cache.Set(cacheKey, outputCache, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(Duration)));
                }
                else if (context.Result is ViewResult viewResult)
                {

                    string viewName = viewResult.ViewName != null ? viewResult.ViewName : context.RouteData.Values["action"].ToString();
                    using (var writer = new StringWriter())
                    {
                        IViewEngine viewEngine = context.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                        ViewEngineResult viewEngineResult = viewEngine.FindView(context, viewName, true);

                        if (viewEngineResult.Success)
                        {
                            ViewContext viewContext = new ViewContext(context, viewEngineResult.View, viewResult.ViewData, viewResult.TempData, writer, new HtmlHelperOptions());
                            await viewEngineResult.View.RenderAsync(viewContext);

                            string content = writer.GetStringBuilder().ToString();
                            var outputCache = new OutputCacheModel() { Content = content, ContentType = context.HttpContext.Response.ContentType, StatusCode = context.HttpContext.Response.StatusCode };
                            cache.Set(cacheKey, outputCache, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(Duration)));

                        }
                        else
                        {
                            Console.WriteLine($"A view with the name {viewName} could not be found");
                        }
                    }
                }
            }
        }
    }
}