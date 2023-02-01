using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyweb.Site.Core;
using Easyweb.Site.Infrastructure.Controllers;
using Easyweb.Site.Infrastructure.Filters;
using Easyweb.Site.Infrastructure.Services;
using Easyweb.Site.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Easyweb.Controllers
{
    /// <summary>
    /// Main base controller that handles all article and defailt requests.
    /// </summary>
    public class EasywebController : EasywebBaseController
    {
        /// <summary>
        /// Base action for all article requests.
        /// </summary>
        [HttpGet]
        [EnsureLinkable]
        public virtual IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Default post action for forms.
        /// </summary>
        [HttpPost]
        [EnsureLinkable]
        [ValidateFormCaptcha]
        [IgnoreAntiforgeryToken]
        public virtual IActionResult Index(IFormCollection collection, [FromServices]IFormService formService)
        {
            // Handle form validation, form mail storage and sending 
            //
            var formPostResult = formService.HandleForm(collection);

            // Create a link result based on whether it was a success
            //
            var resultLink = formPostResult.Successful ? ResultLink.GoodPostPage : ResultLink.BadPostPage;

            // Redirect or return view. The FormResult result will handle both ajax requests and normal requests, and return a view or a redirect result
            // either from default linkables that exists, or the default view
            //
            return FormPostResult(resultLink);
        }
    }
}
