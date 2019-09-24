using System;
using System.Web.Mvc;

namespace TilosAzureMvc {
    public class AllowCrossSiteAttribute : ActionFilterAttribute {

        public string origin { get; set; }
        public override void OnActionExecuting(ActionExecutingContext filterContext) {
            if (origin == null) origin = "https://tilos.hu";
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", origin);
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Headers", "*");
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Credentials", "true");

            base.OnActionExecuting(filterContext);
        }
    }
}