using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class RegisterSuccessModel : PolpAbpAccountPageModel
    {

        public virtual Task<IActionResult> OnGetAsync()
        {
            // Render page 
            return Task.FromResult(Page() as IActionResult);
        }
    }
}
