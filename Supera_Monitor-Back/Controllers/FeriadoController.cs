using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Feriado;
using Supera_Monitor_Back.Models.Sala;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers
{
	//[Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
	[ApiController]
	[Route("back/[controller]")]
	public class FeriadoController : _BaseController
	{
		private readonly IFeriadoService _feriadoService;
		private readonly ILogService _logger;

		public FeriadoController(IFeriadoService feriadoService, ILogService logger)
		{
			_feriadoService = feriadoService;
			_logger = logger;
		}

		[HttpGet("all")]
		public ActionResult<List<FeriadoList>> GetAll()
		{
			try
			{
				var response = _feriadoService.GetList();

				return Ok(response);
			}
			catch (Exception e)
			{
				_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[HttpGet("{id}")]
		public ActionResult<FeriadoList> Get(int id)
		{
			try
			{
				var response = _feriadoService.Get(id);

				return Ok(response);
			}
			catch (Exception e)
			{
				_logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[HttpPost]
		public ActionResult<ResponseModel> Insert(InsertFeriadoRequest model)
		{
			try
			{
				var response = _feriadoService.Insert(model);
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

		[HttpPut]
		public ActionResult<ResponseModel> Update(UpdateFeriadoRequest model)
		{
			try
			{
				var response = _feriadoService.Update(model);

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

		[HttpPatch("{id}")]
		public ActionResult<ResponseModel> ToggleDeactivate(int id)
		{
			try
			{
				var response = _feriadoService.ToggleDeactivate(id);

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
