using Microsoft.AspNetCore.Mvc;

namespace OTPTest.Api.Controllers
{
  [ApiController]
  [Route("/")]
  public class HomeController : Controller
  {

    [HttpGet]
    public ContentResult Get()
    {
      return Content("OTP API started", "text/plain");
    }
  }
}

