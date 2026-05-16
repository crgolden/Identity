namespace Identity.Pages.Account.Manage;

using Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model shown after a device authorization flow completes successfully.</summary>
[AllowAnonymous]
[SecurityHeaders]
public class DeviceSuccessModel : PageModel
{
}
