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
				return StatusCode(500, e);
			}
		}

		[Authorize]
		[HttpGet("dropdown/aula-zero")]
		public ActionResult<List<AlunoList>> GetAlunosAulaZeroDropdown()
		{
			try
			{
				var response = _alunoService.GetAlunosAulaZeroDropdown();
				return Ok(response);
			}
			catch (Exception e)
			{
				return StatusCode(500, e);
			}
		}


		[Authorize]
		[HttpGet("dropdown/primeira-aula")]
		public ActionResult<List<AlunoList>> GetAlunosPrimeiraAulaDropdown()
		{
			try
			{
				var response = _alunoService.GetAlunosPrimeiraAulaDropdown();
				return Ok(response);
			}
			catch (Exception e)
			{
				return StatusCode(500, e);
			}
		}
		[Authorize]
		[HttpGet("dropdown/reposicao-de/{evento_Id}")]
		public ActionResult<List<AlunoList>> GetAlunosReposicaoDeDropdown(int evento_Id)
		{
			try
			{
				var response = _alunoService.GetAlunosReposicaoDeDropdown(evento_Id);
				return Ok(response);
			}
			catch (Exception e)
			{
				return StatusCode(500, e);
			}
		}
		[Authorize]
		[HttpGet("dropdown/reposicao-para/{evento_Id}")]
		public ActionResult<List<AlunoList>> GetAlunosReposicaoParaDropdown(int evento_Id)
		{
			try
			{
				var response = _alunoService.GetAlunosReposicaoParaDropdown(evento_Id);
				return Ok(response);
			}
			catch (Exception e)
			{
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
					return Created(@$"/alunos/{alunoId}", response);
				}

				return BadRequest(response);
			}
			catch (Exception e)
			{
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
					return Ok(response);
				}

				return BadRequest(response);
			}
			catch (Exception e)
			{
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
					return Ok(response);
				}

				return BadRequest(response);
			}
			catch (Exception e)
			{
				return StatusCode(500, e);
			}
		}

	}
}
