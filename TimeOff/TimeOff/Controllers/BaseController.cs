using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace TimeOff.Controllers
{
	public class BaseController<T> : ControllerBase
	{

		#region Methods
		// protected string GetClaim(string type)
		// {
		// 	if (Request == null)
		// 		return string.Empty;

		// 	return Request.HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == type)?.Value;
		// }

		protected async Task<IActionResult> TryExecuteAction(Func<Task<IActionResult>> myMetho)
		{
			try
			{
				return await myMetho();
			}
			catch (BadRequestException ex)
			{
				LogBadRequest(ex);
				Console.WriteLine(String.Format("Exception TryExecuteAction:{0}", ex.Message));
				return BadRequest();
			}
			catch (Exception ex)
			{
				LogExeption(ex);
				return BadRequest();
			}
		}
		protected bool IsValidEmail(string email)
		{
			try
			{
				var addr = new System.Net.Mail.MailAddress(email);
				return addr.Address == email;
			}
			catch
			{
				return false;
			}
		}

		private IActionResult LogBadRequest(BadRequestException ex)
		{
			return StatusCode((int)HttpStatusCode.PreconditionFailed, $"Bad Request! : {ex.Message}");
		}

		private IActionResult LogExeption(Exception ex)
		{
			return StatusCode((int)HttpStatusCode.InternalServerError, $"Erro interno! : {ex.Message}");
		}

		protected DateTime HrBrasiliam()
		{
			try
			{
				DateTime dateTime = DateTime.UtcNow;
				TimeZoneInfo hrBrasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
				Console.WriteLine(String.Format("HrBrasiliam:{0}", hrBrasilia));
				return TimeZoneInfo.ConvertTimeFromUtc(dateTime, hrBrasilia);
			}
			catch (Exception ex)
			{
				foreach (var item in TimeZoneInfo.GetSystemTimeZones())
				{
					Console.WriteLine(String.Format("TimeZoneInfo.Name:{0}; TimeZoneInfo.Id:{1}", item.DisplayName, item.Id));
				}
				Console.WriteLine(String.Format("Converter.Exception:{0}", ex.Message));
				return DateTime.UtcNow;
			}
		}
		#endregion

	}

	public class BadRequestException : Exception
	{
		public BadRequestException() { }

		public BadRequestException(string message) : base(message)
		{ }
	}
}
