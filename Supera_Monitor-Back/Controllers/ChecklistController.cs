using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Checklist;
using Supera_Monitor_Back.Services;

namespace Supera_Monitor_Back.Controllers
{
	[Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
	[ApiController]
	[Route("back/[controller]")]
	public class ChecklistController : _BaseController
	{
		private readonly IChecklistService _checklistService;
		private readonly ILogService _logger;

		public ChecklistController(
			IChecklistService checklistService,
			ILogService logger
		)
		{
			_checklistService = checklistService;
			_logger = logger;
		}

		[HttpGet("all")]
		public ActionResult<List<ChecklistModel>> GetAll()
		{
			try
			{
				var response = _checklistService.GetAll();

				return Ok(response);
			}
			catch (Exception e)
			{
				_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[HttpPost("populate/{alunoId}")]
		public ActionResult<ResponseModel> PopulateAlunoChecklist(int alunoId)
		{
			try
			{
				var response = _checklistService.PopulateAlunoChecklist(alunoId);

				if (response.Success)
				{
					return Ok(response);
				}

				return BadRequest(response);
			}
			catch (Exception e)
			{
				_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[HttpPatch("toggle-item")]
		public ActionResult<ResponseModel> FinalizaChecklist(FinalizaChecklistRequest model)
		{
			try
			{
				var response = _checklistService.FinalizaChecklist(model);

				if (response.Success)
					return Ok(response);

				return BadRequest(response);
			}
			catch (Exception e)
			{
				_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}
	}
}
