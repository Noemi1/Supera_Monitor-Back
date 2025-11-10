using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Services;

namespace Supera_Monitor_Back.Controllers
{
	[ApiController]
	[Route("back/[controller]")]
	public class AlunosController : _BaseController
	{
		private readonly IAlunoService _alunoService;
		private readonly ILogService _logger;

		public AlunosController(
			IAlunoService alunoService, 
			ILogService logService
		)
		{
			_alunoService = alunoService;
			_logger = logService;
		}

		[Authorize]
		[HttpGet("all")]
		public ActionResult<List<AlunoList>> GetAll()
		{
			try
			{
				var response = _alunoService.GetAll();

				return Ok(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}
		
		[Authorize]
		[HttpGet("{alunoId}")]
		public ActionResult<AlunoResponse> Get(int alunoId)
		{
			try
			{
				var response = _alunoService.Get(alunoId);

				return Ok(response);
			}
			catch (Exception e)
			{
				return StatusCode(500, e);
			}
		}

		[Authorize]
		[HttpGet("historico/{alunoId}")]
		public ActionResult<List<AlunoHistoricoList>> GetHistoricoById(int alunoId)
		{
			try
			{
				List<AlunoHistoricoList> response = _alunoService.GetHistoricoById(alunoId);

				return Ok(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[Authorize]
		[HttpGet("vigencia/{alunoId}")]
		public ActionResult<List<AlunoVigenciaList>> GetVigenciaById(int alunoId)
		{
			try
			{
				List<AlunoVigenciaList> response = _alunoService.GetVigenciaById(alunoId);

				return Ok(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[HttpPost()]
		public ActionResult<ResponseModel> Insert(CreateAlunoRequest model)
		{
			try
			{
				ResponseModel response = _alunoService.Insert(model);

				if (response.Success)
				{
					int alunoId = response.Object!.Id;
					// _logger.Log("Insert", "Alunos", response, Account?.Id);
					return Created(@$"/alunos/{alunoId}", response);
				}

				return BadRequest(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[Authorize]
		[HttpPut()]
		public ActionResult<ResponseModel> Update(UpdateAlunoRequest model)
		{
			try
			{
				var response = _alunoService.Update(model);

				if (response.Success)
				{
					// _logger.Log("Update", "Alunos", response, Account?.Id);
					return Ok(response);
				}

				return BadRequest(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[Authorize]
		[HttpPatch("toggle-active/{alunoId}")]
		public ActionResult<ResponseModel> ToggleDeactivate(int alunoId)
		{
			try
			{
				ResponseModel response = _alunoService.ToggleDeactivate(alunoId);

				if (response.Success)
				{
					string action = response.Object!.Deactivated is null ? "Enable" : "Disable";

					// _logger.Log(action, "Alunos", response, Account?.Id);
					return Ok(response);
				}

				return BadRequest(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[Authorize]
		[HttpGet("image/{alunoId}")]
		public ActionResult<ResponseModel> GetProfileImage(int alunoId)
		{
			try
			{
				ResponseModel response = _alunoService.GetProfileImage(alunoId);

				return Ok(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[Authorize]
		[HttpPost("reposicao")]
		public ActionResult<ResponseModel> Reposicao(ReposicaoRequest model)
		{
			try
			{
				ResponseModel response = _alunoService.Reposicao(model);

				if (response.Success)
				{
					// _logger.Log("Reposicao", "Aluno", response, Account?.Id);
					return Ok(response);
				}

				return BadRequest(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[Authorize]
		[HttpPost("primeira-aula")]
		public ActionResult<ResponseModel> PrimeiraAula(PrimeiraAulaRequest model)
		{
			try
			{
				ResponseModel response = _alunoService.PrimeiraAula(model);

				if (response.Success)
				{
					// _logger.Log("Primeira Aula", "Aluno", response, Account?.Id);
					return Ok(response);
				}

				return BadRequest(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		//[Authorize]
		//[HttpGet("apostilas/{alunoId}")]
		//public ActionResult<List<ApostilaList>> GetApostilasByAluno(int alunoId)
		//{
		//	try
		//	{
		//		var response = _alunoService.GetApostilasByAluno(alunoId);

		//		return Ok(response);
		//	}
		//	catch (Exception e)
		//	{
		//		// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
		//		return StatusCode(500, e);
		//	}
		//}
		
		//[Authorize]
		//[HttpPost("checklists/all")]
		//public ActionResult<List<AlunoResponse>> GetAllAlunoChecklists(AlunoRequest request)
		//{
		//	try
		//	{
		//		var response = _alunoService.GetAlunoChecklistItemList(request);

		//		return Ok(response);
		//	}
		//	catch (Exception e)
		//	{
		//		// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
		//		return StatusCode(500, e);
		//	}
		//}
	}
}
