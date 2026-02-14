using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookwormsOnline.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public int? StatusCode { get; set; }
        public string ErrorMessage { get; set; }

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(int? statusCode = null)
        {
            StatusCode = statusCode;

            if (statusCode == 404)
            {
                ErrorMessage = "Oops! The page you are looking for could not be found.";
            }
            else if (statusCode == 403)
            {
                ErrorMessage = "Sorry, you don't have permission to access this resource.";
            }
            else
            {
                ErrorMessage = "An error occurred while processing your request.";
            }
        }
    }

}
