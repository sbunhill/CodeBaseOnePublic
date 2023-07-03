using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.Controllers
{
    /// <summary>
    /// ToDo
    /// </summary>
    [ApiController]
    [ExcludeFromCodeCoverage]
    public class HomeController : ControllerBase
    {
        /// <summary>
        /// launchurl for production build
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet]
        [Route("")]
        public ActionResult GetDefault()
        {
            return Ok("Ok");
        }

        /// <summary>
        /// no authentication required - api healthcheck
        /// </summary>
        /// <response code="200">Ok</response>
        [HttpGet("healthcheck"), AllowAnonymous]
        public ActionResult HealthCheck()
        {
            return Ok("Ok");
        }

    }
}
