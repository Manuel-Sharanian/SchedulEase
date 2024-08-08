using Microsoft.AspNetCore.Http;

using System.Threading.Tasks;

namespace BeautySalon.Middleware
{
    public class RedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public RedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated &&
                !context.User.IsInRole("Admin") &&
                context.Request.Path == "/")
            {
                context.Response.Redirect("/Appointments/Index");
                return;
            }

            await _next(context);
        }
    }
}